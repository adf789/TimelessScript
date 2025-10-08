
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct LinkAnimationJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public ComponentLookup<SpriteSheetAnimationComponent> animationComponentLookup;
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

                if (animationComponentLookup.HasComponent(childEntity))
                {
                    objectComponent.AnimationEntity = childEntity;
                    break;
                }
            }
        }

        // SpriteRenderer SortingOrder 설정
        if (objectComponent.AnimationEntity != Entity.Null &&
            animationComponentLookup.HasComponent(objectComponent.AnimationEntity))
        {
            var anim =
            animationComponentLookup.GetRefRW(objectComponent.AnimationEntity);
            anim.ValueRW.SetLayer(linkerFlag.Layer);
        }

        ecb.RemoveComponent<AnimationLinkerFlagComponent>(entity);
    }
}
