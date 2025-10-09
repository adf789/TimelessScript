
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public partial struct LinkRendererJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public ComponentLookup<SpriteRendererComponent> rendererComponentLookup;
    public ComponentLookup<ObjectTargetComponent> targetComponentLookup;
    [ReadOnly] public BufferLookup<LinkedEntityGroup> linkedEntityGroupLookup;

    public void Execute(Entity entity,
    in AnimationLinkerFlagComponent linkerFlag,
    ref TSObjectComponent objectComponent)
    {
        objectComponent.Self = entity;

        // 자식 엔티티에서 SpriteSheetAnimationComponent 찾기
        if (linkedEntityGroupLookup.HasBuffer(entity))
        {
            var linkedEntities = linkedEntityGroupLookup[entity];

            for (int i = 0; i < linkedEntities.Length; i++)
            {
                var childEntity = linkedEntities[i].Value;

                if (!rendererComponentLookup.HasComponent(childEntity))
                    continue;

                // 렌더러 엔티티 연결
                objectComponent.RendererEntity = childEntity;

                // 메인 엔티티와 연결
                if (targetComponentLookup.HasComponent(childEntity))
                    targetComponentLookup.GetRefRW(childEntity).ValueRW.Target = entity;

                break;
            }
        }

        // SpriteRenderer SortingOrder 설정
        if (objectComponent.RendererEntity != Entity.Null &&
            rendererComponentLookup.HasComponent(objectComponent.RendererEntity))
        {
            var renderer =
            rendererComponentLookup.GetRefRW(objectComponent.RendererEntity);
            renderer.ValueRW.Layer = linkerFlag.Layer;
        }

        ecb.RemoveComponent<AnimationLinkerFlagComponent>(entity);
    }
}
