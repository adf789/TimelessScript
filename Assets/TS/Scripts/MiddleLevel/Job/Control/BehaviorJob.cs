
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct BehaviorJob : IJobEntity
{
    [NativeDisableParallelForRestriction]
    public ComponentLookup<SpriteSheetAnimationComponent> AnimationComponentLookup;

    [NativeDisableParallelForRestriction]
    public ComponentLookup<ObjectTargetComponent> ObjectTargetComponentLookup;
    [NativeDisableParallelForRestriction]
    public ComponentLookup<LocalTransform> TransformLookup;
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float Speed;
    public float ClimbSpeed;
    public float DeltaTime;
    public bool IsPrevGrounded;

    public void Execute([EntityIndexInQuery] int entityInQueryIndex,
        Entity entity,
    ref TSObjectComponent objectComponent,
    ref TSActorComponent actorComponent,
    ref PhysicsComponent physicsComponent,
    in NavigationComponent navigationComponent,
    [ReadOnly] DynamicBuffer<Child> children)
    {
        RefRW<LocalTransform> transform = TransformLookup.GetRefRW(entity);
        RefRW<SpriteSheetAnimationComponent> animComponent = default;

        if (objectComponent.AnimationEntity == Entity.Null)
        {
            // 자식 목록을 순회하여 원하는 컴포넌트를 가진 자식을 찾습니다.
            foreach (var child in children)
            {
                var childEntity = child.Value;

                // 자식 엔티티가 SpriteSheetAnimationComponent와 Authoring을 모두 가지고 있는지 확인합니다.
                if (AnimationComponentLookup.HasComponent(childEntity))
                {
                    // 자식의 컴포넌트들을 가져옵니다.
                    objectComponent.AnimationEntity = childEntity;
                    animComponent = AnimationComponentLookup.GetRefRW(childEntity);

                    // 엔티티 타겟을 캐싱함
                    var objectTargetComponent = ObjectTargetComponentLookup.GetRefRW(childEntity);
                    objectTargetComponent.ValueRW.Target = entity;
                    break;
                }
            }
        }
        else
        {
            animComponent = AnimationComponentLookup.GetRefRW(objectComponent.AnimationEntity);
        }

        if (!animComponent.IsValid)
            return;

        if (!physicsComponent.IsGrounded)
        {
            animComponent.ValueRW.RequestTransition(AnimationState.Fall, AnimationTransitionType.SkipAllPhase);
            return;
        }

        var purpose = actorComponent.Move.MoveState;

        if (purpose == MoveState.Move)
        {
            OnStartMoving(ref animComponent.ValueRW);

            Debug.Log($"[BehaviorJob] 일반 이동 처리 중: {objectComponent.Name}");
            HandleMovement(ref transform.ValueRW, ref objectComponent, ref actorComponent, ref animComponent.ValueRW);

            if (navigationComponent.IsActive && navigationComponent.State == NavigationState.Completed)
                OnEndMoving(entityInQueryIndex, in entity, in transform.ValueRO, ref objectComponent, ref actorComponent, ref animComponent.ValueRW);
        }
        else if (purpose == MoveState.ClimbUp
        || purpose == MoveState.ClimbDown)
        {
            OnStartClimbing(in actorComponent, ref animComponent.ValueRW);

            Debug.Log($"[BehaviorJob] 사다리 이동 처리 중: {objectComponent.Name} - {actorComponent.Move.MoveState}");
            HandleClimbing(ref transform.ValueRW, ref objectComponent, ref actorComponent, ref animComponent.ValueRW);
        }
        else if (!physicsComponent.IsPrevGrounded && physicsComponent.IsGrounded)
        {
            // 땅에 착지했을 때 애니메이션 수정
            animComponent.ValueRW.RequestTransition(AnimationState.Idle, AnimationTransitionType.SkipCurrentPhase);
        }
    }

    private void HandleMovement(ref LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref TSActorComponent actorComponent,
    ref SpriteSheetAnimationComponent anim)
    {
        // 1. 현재 '루트(발)'의 위치를 계산합니다. (피벗 위치 + 오프셋)
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        // 2. 목표 '루트' 위치를 가져옵니다.
        float2 moveRootPosition = actorComponent.Move.MovePosition;

        anim.IsFlip = moveRootPosition.x < currentRootPosition.x;

        // 3. 한 프레임에 이동할 최대 거리를 계산합니다.
        float maxDistanceDelta = Speed * DeltaTime;

        // 4. MoveTowards를 사용해 다음 '루트' 위치를 계산합니다.
        float2 newRootPosition = Utility.Geometry.MoveTowards(currentRootPosition, moveRootPosition, maxDistanceDelta);

        // 5. 새로 계산된 '루트' 위치를 엔티티의 '피벗' 위치로 다시 변환합니다. (루트 위치 - 오프셋)
        float2 newTransformPosition = newRootPosition;
        newTransformPosition.y -= objectComponent.RootOffset;

        // 6. 최종 '피벗' 위치를 LocalTransform에 적용합니다. Z축 값은 유지합니다.
        transform.Position = new float3(newTransformPosition.x, newTransformPosition.y, transform.Position.z);

        // 도착지 위치 비교
        currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;
    }

    private void HandleClimbing(ref LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref TSActorComponent actorComponent,
    ref SpriteSheetAnimationComponent animComponent)
    {
        // 현재 위치와 목표 위치 계산
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        float2 targetRootPosition = actorComponent.Move.MovePosition;

        Debug.Log($"사다리 이동: 현재({currentRootPosition.x:G2}, {currentRootPosition.y:G2}) → 목표({targetRootPosition.x:G2}, {targetRootPosition.y:G2})");

        float maxDistanceDelta = ClimbSpeed * DeltaTime; // 일반 이동 속도 사용

        // 사다리 이동은 일반적인 MoveTowards 사용 (물리 무시)
        float2 newRootPosition = Utility.Geometry.MoveTowards(currentRootPosition, targetRootPosition, maxDistanceDelta);

        // Transform 위치 업데이트
        float2 newTransformPosition = newRootPosition;
        newTransformPosition.y -= objectComponent.RootOffset;

        transform.Position = new float3(newTransformPosition.x, newTransformPosition.y, transform.Position.z);

        // 도착 확인
        currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        float remainingDistance = math.distance(currentRootPosition, targetRootPosition);
        Debug.Log($"[BehaviorJob] 사다리 이동 중: 남은 거리 = {remainingDistance:G3}");

        if (remainingDistance < StringDefine.AUTO_MOVE_WAYPOINT_ARRIVAL_DISTANCE)
        {
            // 정확한 목표 위치로 스냅
            float2 exactTransformPos = targetRootPosition;
            exactTransformPos.y -= objectComponent.RootOffset;
            transform.Position = new float3(exactTransformPos.x, exactTransformPos.y, transform.Position.z);

            // 사다리 이동 완료 시 Idle 애니메이션으로 전환 (자연스러운 전환을 위해 Start 애니메이션 사용)
            animComponent.RequestTransition(AnimationState.Idle);
            actorComponent.Move.MoveState = MoveState.None;
            actorComponent.Move.MovePosition = targetRootPosition;

            Debug.Log($"[BehaviorJob] 사다리 이동 완료: {objectComponent.Name}, 거리: {remainingDistance:G3}");
        }
    }

    private void OnStartMoving(ref SpriteSheetAnimationComponent animComponent)
    {
        if (animComponent.CurrentState != AnimationState.Walking)
        {
            // 걷기 애니메이션으로 전환 (Start 애니메이션 사용)
            animComponent.RequestTransition(AnimationState.Walking, AnimationTransitionType.SkipAllPhase);
        }
    }

    private void OnStartClimbing(in TSActorComponent actorComponent,
    ref SpriteSheetAnimationComponent animComponent)
    {
        var purpose = actorComponent.Move.MoveState;
        var currentState = animComponent.CurrentState;

        if (purpose == MoveState.ClimbUp)
        {
            bool isSkip = currentState == AnimationState.Ladder_ClimbDown;

            animComponent.RequestTransition(AnimationState.Ladder_ClimbUp,
            isSkip ? AnimationTransitionType.SkipAllPhase : AnimationTransitionType.SkipCurrentPhase);
        }
        else
        {
            bool isSkip = currentState == AnimationState.Ladder_ClimbUp;

            animComponent.RequestTransition(AnimationState.Ladder_ClimbDown,
            isSkip ? AnimationTransitionType.SkipAllPhase : AnimationTransitionType.SkipCurrentPhase);
        }
    }

    private void OnEndMoving(int entityInQueryIndex,
        in Entity entity,
        in LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref TSActorComponent actorComponent,
    ref SpriteSheetAnimationComponent anim)
    {
        // 현재 오브젝트와 타겟의 위치를 가져옵니다.
        float2 targetPosition = TransformLookup[actorComponent.Move.Target].Position.xy;
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        // 이동 목적지 리셋
        if (actorComponent.Move.TargetType == TSObjectType.Gimmick)
        {
            anim.IsFlip = targetPosition.x < currentRootPosition.x;
            anim.RequestTransition(AnimationState.Interact, AnimationTransitionType.SkipAllPhase);

            var interactComponent = new InteractComponent()
            {
                DataID = actorComponent.Move.TargetDataID,
                DataType = TableDataType.Gimmick,
            };

            Ecb.AddComponent(entityInQueryIndex, entity, interactComponent);
        }
        else
        {
            anim.RequestTransition(AnimationState.Idle, AnimationTransitionType.SkipAllPhase);
        }

        actorComponent.Move.TargetType = TSObjectType.None;
        actorComponent.Move.MovePosition = currentRootPosition;
        actorComponent.Move.MoveState = MoveState.None;

        Debug.Log($"[BehaviorJob] 일반 이동 완료: {objectComponent.Name}");
    }
}
