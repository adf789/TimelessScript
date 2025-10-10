
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct SpriteRendererJob : IJobEntity
{
    public void Execute(Entity entity,
    ref SpriteRendererComponent renderer,
    SpriteRendererAuthoring authoring)
    {
        if (!authoring.IsInitialized)
        {
            authoring.Initialize();
            return;
        }

        SetRenderer(authoring, in renderer);
    }

    private void SetRenderer(SpriteRendererAuthoring authoring, in SpriteRendererComponent renderer)
    {
        // 컴포넌트 값에 맞춰서 렌더러 옵션 변경
        authoring.SetFlip(renderer.IsFlip);
        authoring.SetLayer(renderer.Layer);
    }
}
