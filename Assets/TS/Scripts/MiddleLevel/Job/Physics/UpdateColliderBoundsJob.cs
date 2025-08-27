
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct UpdateColliderBoundsJob : IJobEntity
{
    public void Execute(
        ref LightweightColliderComponent collider,
        ref ColliderBoundsComponent bounds,
        in LocalTransform transform)
    {
        // 위치 업데이트
        collider.position = transform.Position.xy;
        
        // Bounds 계산
        float2 center = collider.position + collider.offset;
        float2 halfSize = collider.size * 0.5f;
        
        bounds.center = center;
        bounds.min = center - halfSize;
        bounds.max = center + halfSize;
    }
}