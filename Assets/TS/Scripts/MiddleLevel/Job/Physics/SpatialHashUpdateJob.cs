
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
        var colliderCenter = position + collider.Offset;
        var halfSize = collider.Size * 0.5f;

        // Collider가 차지하는 영역의 최소/최대 좌표 계산
        var minBounds = colliderCenter - halfSize;
        var maxBounds = colliderCenter + halfSize;

        // 해당 영역이 차지하는 셀의 범위 계산
        hashKey.MinCell = new int2(
            (int) math.floor(minBounds.x / cellSize),
            (int) math.floor(minBounds.y / cellSize)
        );

        hashKey.MaxCell = new int2(
            (int) math.floor(maxBounds.x / cellSize),
            (int) math.floor(maxBounds.y / cellSize)
        );
    }
}