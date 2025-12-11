using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Ground Bounds 업데이트 Job
/// </summary>
[BurstCompile(CompileSynchronously = true,
              OptimizeFor = OptimizeFor.Performance,
              FloatMode = FloatMode.Fast,
              FloatPrecision = FloatPrecision.Low)]
public partial struct UpdateColliderBoundsJob : IJobEntity
{


    public void Execute(
        Entity entity,
        ref ColliderBoundsComponent bounds,
        in LocalToWorld worldPosition,
        in ColliderComponent collider)
    {
        var position = worldPosition.Position.xy;
        var halfSize = collider.Size * 0.5f;

        bounds.Center = position + collider.Offset;
        bounds.Min = bounds.Center - halfSize;
        bounds.Max = bounds.Center + halfSize;
    }
}