
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct SetNameJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;

    public void Execute([EntityIndexInQuery] int entityIndexInQuery,
        Entity entity,
    in SetNameComponent setName)
    {
        FixedString64Bytes name = $"{setName.Name}({entity.Index}:{entity.Version})";
        Ecb.SetName(entityIndexInQuery, entity, name);
        Ecb.RemoveComponent<SetNameComponent>(entityIndexInQuery, entity);
    }
}
