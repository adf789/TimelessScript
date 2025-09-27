
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct UpdateColliderBoundsJob : IJobEntity
{
    public void Execute(
        ref ColliderComponent collider,
        ref ColliderBoundsComponent bounds,
        in LocalTransform transform)
    {
        // 위치 가져옴
        var position = transform.Position.xy;

        // Bounds 계산
        float2 center = position + collider.Offset;
        float2 halfSize = collider.Size * 0.5f;

        bounds.Center = center;
        bounds.Min = center - halfSize;
        bounds.Max = center + halfSize;
    }
}