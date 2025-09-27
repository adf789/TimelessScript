
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// 공간 해시를 위한 별도 Job
[BurstCompile]
public partial struct SpatialHashUpdateJob : IJobEntity
{
    [ReadOnly] public float cellSize;

    public void Execute(
        Entity entity,
        in LocalTransform transform,
        ref SpatialHashKeyComponent hashKey,
        in ColliderComponent collider)
    {
        var position = transform.Position.xy;

        hashKey.CellPosition = new int2(
            (int) math.floor(position.x / cellSize),
            (int) math.floor(position.y / cellSize)
        );
    }
}