using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Ground Bounds 업데이트 Job
/// </summary>
[BurstCompile(CompileSynchronously = true,
              OptimizeFor = OptimizeFor.Performance,
              FloatMode = FloatMode.Fast,
              FloatPrecision = FloatPrecision.Low)]
public partial struct UpdateGroundBoundsJob : IJobEntity
{


    public void Execute(
        ref ColliderBoundsComponent bounds,
        in Parent parent,
        in LocalTransform transform,
        in ColliderComponent collider)
    {
        var position = transform.Position.xy;
        var halfSize = collider.Size * 0.5f;

        bounds.Center = position + collider.Offset;
        bounds.Min = bounds.Center - halfSize;
        bounds.Max = bounds.Center + halfSize;
    }
}