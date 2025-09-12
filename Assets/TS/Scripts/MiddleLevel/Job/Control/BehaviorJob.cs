
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct BehaviorJob : IJobEntity
{
    
    public void Execute(Entity entity,
    ref LocalTransform localTransform)
    {

    }
}