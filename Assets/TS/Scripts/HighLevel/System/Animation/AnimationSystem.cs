using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct AnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 이 시스템은 SpriteSheetAnimationComponent가 있는 엔티티가 하나라도 있을 때만 업데이트됩니다.
        state.RequireForUpdate<SpriteRendererComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        // 애니메이션 처리 및 렌더링 옵션 적용
        var animationJob = new AnimationJob()
        {
            DeltaTime = state.World.Time.DeltaTime
        };

        animationJob.ScheduleParallel(state.Dependency);

        // 렌더링 옵션 적용
        foreach (var (spriteRenderer, renderer) in
        SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<SpriteRenderer>,
        RefRO<SpriteRendererComponent>>())
        {
            SetRenderer(spriteRenderer.Value, in renderer.ValueRO);
        }

        // 애니메이션 콜백 이벤트 호출
        var animationCallbackJob = new AnimationCallbackJob()
        {
            ObjectTargetCLookup = SystemAPI.GetComponentLookup<ObjectTargetComponent>(true),
            InteractCLookup = SystemAPI.GetComponentLookup<InteractComponent>(true),
            InteractBLookup = SystemAPI.GetBufferLookup<InteractBuffer>(false),
            Ecb = ecb.AsParallelWriter(),
        };

        animationCallbackJob.ScheduleParallel(state.Dependency);
    }

    private void SetRenderer(SpriteRenderer spriteRenderer, in SpriteRendererComponent renderer)
    {
        // 컴포넌트 값에 맞춰서 렌더러 옵션 변경
        SetFlip(spriteRenderer, renderer.IsFlip);
        SetLayer(spriteRenderer, renderer.Layer);
    }

    public void SetFlip(SpriteRenderer spriteRenderer, bool isFlip)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = isFlip;
    }

    public void SetLayer(SpriteRenderer spriteRenderer, int layer)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = layer;
    }
}