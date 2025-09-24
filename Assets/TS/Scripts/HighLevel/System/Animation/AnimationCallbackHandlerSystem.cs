using Unity.Entities;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// 애니메이션 완료 이벤트를 처리하는 시스템
/// 특정 애니메이션이 끝났을 때 수행할 로직을 여기에 구현
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(SpriteSheetAnimationSystem))]
public partial struct AnimationCallbackHandlerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    => state.RequireForUpdate<SpriteSheetAnimationComponent>();

    public void OnUpdate(ref SystemState state)
    {
        // 애니메이션 완료 이벤트 처리 (Main Thread에서 실행)
        foreach (var (animComponent, entity)
        in SystemAPI.Query<RefRW<SpriteSheetAnimationComponent>>().WithEntityAccess())
        {
            if (animComponent.ValueRO.AnimationCompleted)
            {
                HandleAnimationCompleted(entity, ref animComponent.ValueRW, ref state);

                animComponent.ValueRW.AnimationCompleted = false;
                animComponent.ValueRW.CompletedAnimationState = AnimationState.None;
            }
        }
    }

    private void HandleAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        switch (animComponent.CompletedAnimationState)
        {
            case AnimationState.Idle:
                break;

            case AnimationState.Interact:
                HandleInteractAnimationCompleted(entity, ref animComponent, ref state);
                break;

            case AnimationState.Ladder_ClimbUp:
            case AnimationState.Ladder_ClimbDown:
                HandleClimbAnimationCompleted(entity, ref animComponent, ref state);
                break;

            case AnimationState.Fall:
                HandleFallAnimationCompleted(entity, ref animComponent, ref state);
                break;

            case AnimationState.Walking:
                HandleWalkAnimationCompleted(entity, ref animComponent, ref state);
                break;

            default:
                // 기본 처리 로직
                Debug.Log($"Animation Completed: {animComponent.CompletedAnimationState} for Entity {state.EntityManager.GetName(entity)}");
                break;
        }
    }

    private void HandleInteractAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Interact Animation Completed for Entity {entity.Index}");

        // 상호작용 애니메이션이 끝났을 때의 로직
        // 예: 상태 변경, 아이템 획득, 문 열기 등

        // objectComponent를 처음 찾는 경우
        // ObjectTargetComponent targetComponent;
        // if (!state.EntityManager.HasComponent<ObjectTargetComponent>(entity))
        // {
        //     // objectComponent 탐색 후 캐싱
        //     var targetEntity = Utility.Entities.FindComponentInParents<TSObjectComponent>(entity, state.EntityManager);
        //     var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        //     var buffer = ecb.CreateCommandBuffer(state.WorldUnmanaged);
        //     targetComponent = new ObjectTargetComponent()
        //     {
        //         target = targetEntity,
        //     };

        //     buffer.AddComponent(entity, targetComponent);
        // }
        // else
        // {
        //     // ObjectComponent를 가져옴
        //     targetComponent = state.EntityManager.GetComponentData<ObjectTargetComponent>(entity);
        // }

        // if (targetComponent.target != Entity.Null)
        // {
        //     var objectEntity = targetComponent.target;
        //     var objectComponent = SystemAPI.GetComponentRW<TSObjectComponent>(objectEntity);
        //     // 상호작용 완료 후 처리
        //     objectComponent.ValueRW.Behavior.MoveState = MoveState.None;

        //     // 관련 상호작용
        //     if (state.EntityManager.HasComponent<InteractComponent>(objectEntity))
        //     {
        //         var interactComponent = state.EntityManager.GetComponentData<InteractComponent>(objectEntity);

        //         var collector = SystemAPI.GetSingletonRW<CollectorComponent>();
        //         collector.ValueRW.InteractCollector.Add(interactComponent);

        //         var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        //         var buffer = ecb.CreateCommandBuffer(state.WorldUnmanaged);

        //         buffer.RemoveComponent<InteractComponent>(objectEntity);
        //     }
        // }

        animComponent.RequestTransition(AnimationState.Idle, AnimationTransitionType.SkipAllPhase);
    }

    private void HandleClimbAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Climb Animation Completed for Entity {entity.Index}");

        // 사다리 애니메이션이 끝났을 때의 로직
        // 예: 물리 상태 복원, 특정 효과 재생 등
    }

    private void HandleFallAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Fall Animation Completed for Entity {entity.Index}");

        // 낙하 애니메이션이 끝났을 때의 로직
        // 예: 착지 효과, 데미지 계산 등
    }

    private void HandleWalkAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Walk Animation Completed for Entity {entity.Index}");

        // 낙하 애니메이션이 끝났을 때의 로직
        // 예: 착지 효과, 데미지 계산 등
    }
}
