
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct PendingPositioningJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute([EntityIndexInQuery] int index,
        Entity entity,
    ref LocalTransform localTransform,
    in PendingPositionComponent pendingPosition)
    {
        localTransform.Position = pendingPosition.Position;

        ecb.RemoveComponent<PendingPositionComponent>(index, entity);
    }
}
