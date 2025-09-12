
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct SpriteSheetAnimationJob : IJobEntity
{
    
    public void Execute(Entity entity,
    ref LocalTransform localTransform)
    {

    }
}