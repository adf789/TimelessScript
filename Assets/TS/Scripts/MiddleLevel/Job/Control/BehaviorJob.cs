
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BehaviorJob : IJobEntity
{
    [NativeDisableParallelForRestriction]
    public ComponentLookup<SpriteSheetAnimationComponent> AnimationComponentLookup;
    [NativeDisableParallelForRestriction]
    public ComponentLookup<SpriteRendererComponent> RendererComponentLookup;

    [NativeDisableParallelForRestriction]
    public ComponentLookup<ObjectTargetComponent> ObjectTargetComponentLookup;
    [NativeDisableParallelForRestriction]
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float Speed;
    public float ClimbSpeed;
    public float DeltaTime;
    public bool IsPrevGrounded;

    public void Execute([EntityIndexInQuery] int entityInQueryIndex,
        Entity entity,
    ref TSObjectComponent tsObject,
    ref TSActorComponent tsActor,
    ref PhysicsComponent physics,
    ref LocalTransform transform,
    in LocalToWorld localToWorld,
    in NavigationComponent navigation)
    {
        if (tsObject.RendererEntity == Entity.Null)
            return;

        RefRW<SpriteSheetAnimationComponent> animComponent = AnimationComponentLookup.GetRefRW(tsObject.RendererEntity);
        RefRW<SpriteRendererComponent> rendererComponent = RendererComponentLookup.GetRefRW(tsObject.RendererEntity);

        if (!animComponent.IsValid)
            return;

        // 공중에서는 떨어지는 애니메이션 고정
        if (!physics.IsGrounded)
        {
            animComponent.ValueRW.RequestTransition(AnimationState.Fall, AnimationTransitionType.SkipAllPhase);
            return;
        }
        // 착지했을 때
        else if (physics.IsRandingAnimation && physics.IsGrounded)
        {
            // 땅에 착지했을 때 애니메이션 수정
            animComponent.ValueRW.RequestTransition(AnimationState.Idle, AnimationTransitionType.SkipCurrentPhase);

            physics.IsRandingAnimation = false;

            return;
        }

        var moveState = tsActor.Move.MoveState;

        // 이동하는 경우
        if (moveState == MoveState.Move)
        {
            OnStartMoving(ref animComponent.ValueRW);

            HandleMovement(ref transform, ref tsObject, ref tsActor, ref rendererComponent.ValueRW, in localToWorld);

            if (navigation.IsActive && navigation.State == NavigationState.Completed)
                OnEndMoving(entityInQueryIndex,
                in entity,
                in transform,
                ref tsObject,
                ref tsActor,
                ref rendererComponent.ValueRW,
                ref animComponent.ValueRW);

            return;
        }

        // 사다리를 이용하는 경우
        if (moveState == MoveState.ClimbUp
        || moveState == MoveState.ClimbDown)
        {
            OnStartClimbing(in tsActor, ref animComponent.ValueRW);

            HandleClimbing(ref transform, ref tsObject, ref tsActor, in localToWorld);

            if (navigation.IsActive && navigation.State == NavigationState.Completed)
                OnEndMoving(entityInQueryIndex,
                in entity,
                in transform,
                ref tsObject,
                ref tsActor,
                ref rendererComponent.ValueRW,
                ref animComponent.ValueRW);

            return;
        }
    }

    private void HandleMovement(ref LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref TSActorComponent actorComponent,
    ref SpriteRendererComponent renderer,
    in LocalToWorld localToWorld
)
    {
        // 1. 현재 '루트(발)'의 위치를 계산합니다. (피벗 위치 + 오프셋)
        float2 currentRootPosition = localToWorld.Position.xy;
        currentRootPosition.y -= objectComponent.RootOffset;

        // 2. 목표 '루트' 위치를 가져옵니다.
        float2 moveRootPosition = actorComponent.Move.MovePosition;

        renderer.IsFlip = moveRootPosition.x < currentRootPosition.x;

        // 3. 한 프레임에 이동할 최대 거리를 계산합니다.
        float maxDistanceDelta = Speed * DeltaTime;

        // 4. MoveTowards를 사용해 다음 '루트' 위치를 계산합니다.
        float2 newRootPosition = Utility.Geometry.MoveTowards(currentRootPosition, moveRootPosition, maxDistanceDelta);

        // 5. 새로 계산된 '루트' 위치를 엔티티의 '피벗' 위치로 다시 변환합니다. (루트 위치 - 오프셋)
        float2 moveDeltaPosition = newRootPosition - currentRootPosition;

        // 6. 최종 '피벗' 위치를 LocalTransform에 적용합니다. Z축 값은 유지합니다.
        float3 movePosition = new float3(moveDeltaPosition.x, moveDeltaPosition.y, transform.Position.z);

        transform.Position += movePosition;
    }

    private void HandleClimbing(ref LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref TSActorComponent actorComponent,
    in LocalToWorld localToWorld)
    {
        // 현재 위치와 목표 위치 계산
        float2 currentRootPosition = localToWorld.Position.xy;
        currentRootPosition.y -= objectComponent.RootOffset;

        float2 targetRootPosition = actorComponent.Move.MovePosition;

        // 사다리 이동은 일반적인 MoveTowards 사용 (물리 무시)
        float2 newRootPosition = Utility.Geometry.MoveTowards(currentRootPosition, targetRootPosition, ClimbSpeed * DeltaTime);

        // Transform 위치 업데이트
        float2 newDeltaRootPosition = newRootPosition - currentRootPosition;

        // 도착 확인
        currentRootPosition = new float2()
        {
            x = localToWorld.Position.x + newDeltaRootPosition.x,
            y = localToWorld.Position.y + newDeltaRootPosition.y - objectComponent.RootOffset
        };

        float remainingDistance = math.distance(currentRootPosition, targetRootPosition);

        // 위치 이동 및 스냅
        if (remainingDistance > StringDefine.AUTO_MOVE_WAYPOINT_ARRIVAL_DISTANCE)
        {
            transform.Position += new float3()
            {
                x = newDeltaRootPosition.x,
                y = newDeltaRootPosition.y,
                z = 0,
            };
        }
        else
        {
            // 정확한 목표 위치로 스냅
            float2 exactTransformPos = targetRootPosition - currentRootPosition;

            transform.Position += new float3()
            {
                x = exactTransformPos.x,
                y = exactTransformPos.y,
                z = 0,
            };
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
    ref SpriteRendererComponent renderer,
    ref SpriteSheetAnimationComponent anim)
    {
        // 현재 오브젝트와 타겟의 위치를 가져옵니다.
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        // 이동 목적지 리셋
        if (actorComponent.Move.TargetType == TSObjectType.Gimmick)
        {
            renderer.IsFlip = actorComponent.Move.MovePosition.x < currentRootPosition.x;
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
    }
}
