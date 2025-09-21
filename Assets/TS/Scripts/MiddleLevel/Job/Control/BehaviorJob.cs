
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
    public float Speed;
    public float ClimbSpeed;
    public float DeltaTime;
    public bool IsPrevGrounded;

    public void Execute(Entity entity,
    ref LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref PhysicsComponent physicsComponent,
    in NavigationComponent navigationComponent,
    [ReadOnly] DynamicBuffer<Child> children)
    {
        RefRW<SpriteSheetAnimationComponent> animComponent = default;

        // 2. 자식 목록을 순회하여 원하는 컴포넌트를 가진 자식을 찾습니다.
        foreach (var child in children)
        {
            var childEntity = child.Value;

            // 3. 자식 엔티티가 SpriteSheetAnimationComponent와 Authoring을 모두 가지고 있는지 확인합니다.
            if (AnimationComponentLookup.HasComponent(child.Value))
            {
                // 4. 자식의 컴포넌트들을 가져옵니다.
                animComponent = AnimationComponentLookup.GetRefRW(child.Value);
                break;
            }
        }

        if (!animComponent.IsValid)
            return;

        var purpose = objectComponent.Behavior.MoveState;
        var currentState = animComponent.ValueRW.CurrentState;

        if (purpose == MoveState.Move)
        {
            if (currentState != AnimationState.Walking)
            {
                // 걷기 애니메이션으로 전환 (Start 애니메이션 사용)
                animComponent.ValueRW.RequestTransition(AnimationState.Walking, AnimationTransitionType.SkipAllPhase);
            }

            Debug.Log($"[BehaviorJob] 일반 이동 처리 중: {objectComponent.Name}");
            HandleMovement(ref transform, ref objectComponent, ref animComponent.ValueRW);
        }
        else if (purpose == MoveState.ClimbUp
        || purpose == MoveState.ClimbDown)
        {
            if (purpose == MoveState.ClimbUp)
            {
                bool isSkip = currentState == AnimationState.Ladder_ClimbDown;

                animComponent.ValueRW.RequestTransition(AnimationState.Ladder_ClimbUp,
                isSkip ? AnimationTransitionType.SkipAllPhase : AnimationTransitionType.SkipCurrentPhase);
            }
            else
            {
                bool isSkip = currentState == AnimationState.Ladder_ClimbUp;

                animComponent.ValueRW.RequestTransition(AnimationState.Ladder_ClimbDown,
                isSkip ? AnimationTransitionType.SkipAllPhase : AnimationTransitionType.SkipCurrentPhase);
            }

            Debug.Log($"[BehaviorJob] 사다리 이동 처리 중: {objectComponent.Name} - {objectComponent.Behavior.MoveState}");
            HandleClimbing(ref transform, ref objectComponent, ref animComponent.ValueRW);
        }
        else if (!physicsComponent.isGrounded)
        {
            animComponent.ValueRW.RequestTransition(AnimationState.Fall, AnimationTransitionType.SkipAllPhase);
        }
        // 땅에 착지했을 때 애니메이션 수정
        else
        {
            if (!physicsComponent.isPrevGrounded && physicsComponent.isGrounded)
            {
                animComponent.ValueRW.RequestTransition(AnimationState.Idle, AnimationTransitionType.SkipCurrentPhase);
            }
            else if (navigationComponent.IsActive && navigationComponent.State == NavigationState.Completed)
            {
                if (animComponent.ValueRW.CurrentState != AnimationState.Interact
                && animComponent.ValueRW.NextState != AnimationState.Interact)
                    animComponent.ValueRW.RequestTransition(AnimationState.Idle, AnimationTransitionType.SkipAllPhase);
            }
        }
    }

    private void HandleMovement(ref LocalTransform transform, ref TSObjectComponent objectComponent, ref SpriteSheetAnimationComponent animComponent)
    {
        // 1. 현재 '루트(발)'의 위치를 계산합니다. (피벗 위치 + 오프셋)
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        // 2. 목표 '루트' 위치를 가져옵니다.
        float2 moveRootPosition = objectComponent.Behavior.MovePosition;
        float2 targetRootPosition = objectComponent.Behavior.TargetPosition;

        animComponent.IsFlip = moveRootPosition.x < currentRootPosition.x;

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

        float distance = math.distance(currentRootPosition, moveRootPosition);
        if (distance < 0.2f) // NavigationSystem과 동일한 임계값 사용
        {
            if (objectComponent.Behavior.TargetType == TSObjectType.Gimmick)
            {
                animComponent.IsFlip = targetRootPosition.x < currentRootPosition.x;
                animComponent.RequestTransition(AnimationState.Interact, AnimationTransitionType.SkipAllPhase);
                animComponent.ShouldTransitionToEndOneTime = true;
            }

            objectComponent.Behavior.MoveState = MoveState.None;
            objectComponent.Behavior.TargetType = TSObjectType.None;
            // 현재 위치를 MovePosition에 업데이트
            objectComponent.Behavior.MovePosition = currentRootPosition;

            Debug.Log($"[BehaviorJob] 일반 이동 완료: {objectComponent.Name}, 거리: {distance:G3}");
        }
    }

    private void HandleClimbing(ref LocalTransform transform, ref TSObjectComponent objectComponent, ref SpriteSheetAnimationComponent animComponent)
    {
        // 현재 위치와 목표 위치 계산
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        float2 targetRootPosition = objectComponent.Behavior.MovePosition;

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

        if (remainingDistance < 0.2f) // NavigationSystem과 동일한 임계값 사용
        {
            // 정확한 목표 위치로 스냅
            float2 exactTransformPos = targetRootPosition;
            exactTransformPos.y -= objectComponent.RootOffset;
            transform.Position = new float3(exactTransformPos.x, exactTransformPos.y, transform.Position.z);

            // 사다리 이동 완료 시 Idle 애니메이션으로 전환 (자연스러운 전환을 위해 Start 애니메이션 사용)
            animComponent.RequestTransition(AnimationState.Idle);
            objectComponent.Behavior.MoveState = MoveState.None;
            objectComponent.Behavior.MovePosition = targetRootPosition;

            Debug.Log($"[BehaviorJob] 사다리 이동 완료: {objectComponent.Name}, 거리: {remainingDistance:G3}");
        }
    }

}