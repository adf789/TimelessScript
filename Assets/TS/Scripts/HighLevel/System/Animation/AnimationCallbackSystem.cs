using Unity.Entities;

/// <summary>
/// 애니메이션 완료 이벤트를 처리하는 시스템
/// 특정 애니메이션이 끝났을 때 수행할 로직을 여기에 구현
/// </summary>
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(AnimationSystem))]
public partial struct AnimationCallbackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    => state.RequireForUpdate<SpriteSheetAnimationComponent>();

    public void OnUpdate(ref SystemState state)
    {
        // // 애니메이션 완료 이벤트 처리 (Main Thread에서 실행)
        // foreach (var (anim, entity)
        // in SystemAPI.Query<RefRW<SpriteSheetAnimationComponent>>().WithEntityAccess())
        // {
        //     // 애니메이션이 시작한 경우
        //     OnAnimationStarted(entity, ref anim.ValueRW, ref state);

        //     // 애니메이션의 한 루프가 종료한 경우
        //     OnAnimationCompleted(entity, ref anim.ValueRW, ref state);

        //     // 애니메이션이 종료한 경우
        //     OnAnimationEnded(entity, ref anim.ValueRW, ref state);
        // }
    }

    /// <summary>
    /// 애니메이션이 시작될 때 콜백
    /// </summary>
    private void OnAnimationStarted(Entity entity, ref SpriteSheetAnimationComponent anim, ref SystemState state)
    {
        var flag = anim.GetFlag(AnimationFlagType.Start);

        if (!flag.IsOn)
            return;

        HandleAnimationStarted(ref state, entity, flag.State);

        anim.SetFlagReset(AnimationFlagType.Start);
    }

    /// <summary>
    /// 애니메이션 하나의 루프가 종료될 때 콜백
    /// </summary>
    private void OnAnimationCompleted(Entity entity, ref SpriteSheetAnimationComponent anim, ref SystemState state)
    {
        var flag = anim.GetFlag(AnimationFlagType.Complete);

        if (!flag.IsOn)
            return;

        HandleAnimationCompleted(ref state, entity, flag.State);

        anim.SetFlagReset(AnimationFlagType.Complete);
    }

    /// <summary>
    /// 애니메이션이 모두 종료될 때 콜백
    /// </summary>
    private void OnAnimationEnded(Entity entity, ref SpriteSheetAnimationComponent anim, ref SystemState state)
    {
        var flag = anim.GetFlag(AnimationFlagType.End);

        if (!flag.IsOn)
            return;

        HandleAnimationEnded(ref state, entity, flag.State);

        anim.SetFlagReset(AnimationFlagType.End);
    }

    private void HandleAnimationStarted(ref SystemState state, Entity entity, AnimationState animationState)
    {
        switch (animationState)
        {
            default:
                // 기본 처리 로직
                break;
        }
    }

    private void HandleAnimationCompleted(ref SystemState state, Entity entity, AnimationState animationState)
    {
        switch (animationState)
        {
            case AnimationState.Interact:
                HandleInteractAnimationCompleted(ref state, entity);
                break;

            default:
                // 기본 처리 로직
                break;
        }
    }

    private void HandleAnimationEnded(ref SystemState state, Entity entity, AnimationState animationState)
    {
        switch (animationState)
        {
            case AnimationState.Interact:
                HandleInteractAnimationEnded(ref state, entity);
                break;

            default:
                // 기본 처리 로직
                break;
        }
    }

    private void HandleInteractAnimationCompleted(ref SystemState state, Entity entity)
    {
        // 엔티티 타겟을 가져옴
        if (!SystemAPI.HasComponent<ObjectTargetComponent>(entity))
            return;

        var objectTarget = SystemAPI.GetComponent<ObjectTargetComponent>(entity);

        if (!SystemAPI.HasComponent<InteractComponent>(objectTarget.Target))
            return;

        // 상호작용 정보를 가져옴
        var interactComponent = SystemAPI.GetComponent<InteractComponent>(objectTarget.Target);

        // 상호작용 버퍼 가져옴
        DynamicBuffer<InteractBuffer> interactBuffer;

        if (!SystemAPI.HasBuffer<InteractBuffer>(objectTarget.Target))
        {
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.World.Unmanaged);

            interactBuffer = ecb.AddBuffer<InteractBuffer>(objectTarget.Target);
        }
        else
        {
            interactBuffer = SystemAPI.GetBuffer<InteractBuffer>(objectTarget.Target);
        }

        // 상호작용 등록
        interactBuffer.Add(new InteractBuffer()
        {
            DataID = interactComponent.DataID,
            DataType = interactComponent.DataType,
        });
    }

    private void HandleInteractAnimationEnded(ref SystemState state, Entity entity)
    {
        if (!SystemAPI.HasComponent<ObjectTargetComponent>(entity))
            return;

        var objectTargetComponent = SystemAPI.GetComponent<ObjectTargetComponent>(entity);

        if (objectTargetComponent.Target == Entity.Null)
            return;

        var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.World.Unmanaged);

        ecb.RemoveComponent<InteractBuffer>(objectTargetComponent.Target);
    }
}
