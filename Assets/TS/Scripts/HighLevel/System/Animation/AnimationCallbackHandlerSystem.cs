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
            if (animComponent.ValueRO.AnimationStarted)
            {
                HandleAnimationStarted(entity, ref animComponent.ValueRW, ref state);

                animComponent.ValueRW.AnimationStarted = false;
                animComponent.ValueRW.StartedAnimationState = AnimationState.None;
            }

            if (animComponent.ValueRO.AnimationCompleted)
            {
                HandleAnimationCompleted(entity, ref animComponent.ValueRW, ref state);

                animComponent.ValueRW.AnimationCompleted = false;
                animComponent.ValueRW.CompletedAnimationState = AnimationState.None;
            }
        }
    }

    private void HandleAnimationStarted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        switch (animComponent.StartedAnimationState)
        {
            case AnimationState.None:
            case AnimationState.Idle:
                break;

            case AnimationState.Interact:
                HandleInteractAnimationStarted(entity, ref animComponent, ref state);
                break;

            case AnimationState.Ladder_ClimbUp:
            case AnimationState.Ladder_ClimbDown:
                HandleClimbAnimationStarted(entity, ref animComponent, ref state);
                break;

            case AnimationState.Fall:
                HandleFallAnimationStarted(entity, ref animComponent, ref state);
                break;

            case AnimationState.Walking:
                HandleWalkAnimationStarted(entity, ref animComponent, ref state);
                break;

            default:
                // 기본 처리 로직
                Debug.Log($"Animation Started: {animComponent.CompletedAnimationState} for Entity {state.EntityManager.GetName(entity)}");
                break;
        }
    }

    private void HandleAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        switch (animComponent.CompletedAnimationState)
        {
            case AnimationState.None:
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

    private void HandleInteractAnimationStarted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Interact Animation Started for Entity {entity.Index}");

        var interactComponent = SystemAPI.GetComponent<InteractComponent>(entity);
        var collectorComponent = SystemAPI.GetSingletonRW<CollectorComponent>();

        // ComponentLookup을 사용해서 Singleton에 접근
        if (collectorComponent.IsValid)
        {
            collectorComponent.ValueRW.InteractCollector.Add(interactComponent);
        }
    }

    private void HandleClimbAnimationStarted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Climb Animation Started for Entity {entity.Index}");

        // 사다리 애니메이션이 시작할 때의 로직
        // 예: 물리 상태 복원, 특정 효과 재생 등
    }

    private void HandleFallAnimationStarted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Fall Animation Started for Entity {entity.Index}");

        // 낙하 애니메이션이 시작할 때의 로직
        // 예: 착지 효과, 데미지 계산 등
    }

    private void HandleWalkAnimationStarted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Walk Animation Started for Entity {entity.Index}");

        // 낙하 애니메이션이 시작할 때의 로직
        // 예: 착지 효과, 데미지 계산 등
    }

    private void HandleInteractAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent animComponent, ref SystemState state)
    {
        Debug.Log($"Interact Animation Completed for Entity {entity.Index}");

        // animComponent.RequestTransition(AnimationState.Idle, AnimationTransitionType.SkipAllPhase);
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
