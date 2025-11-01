using Unity.Mathematics;
using System.Runtime.InteropServices;

/// <summary>
/// Axis-Aligned Bounding Box for spatial queries
/// Simple bounds representation for culling and intersection tests
/// </summary>
[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct AABB
{
    public float3 Center;
    public float3 Extents;

    /// <summary>
    /// Minimum point of the bounds
    /// </summary>
    public float3 Min => Center - Extents;

    /// <summary>
    /// Maximum point of the bounds
    /// </summary>
    public float3 Max => Center + Extents;

    /// <summary>
    /// Create AABB from min and max points
    /// </summary>
    public static AABB CreateFromMinMax(float3 min, float3 max)
    {
        return new AABB
        {
            Center = (min + max) * 0.5f,
            Extents = (max - min) * 0.5f
        };
    }

    /// <summary>
    /// Create AABB from center and size
    /// </summary>
    public static AABB CreateFromCenterSize(float3 center, float3 size)
    {
        return new AABB
        {
            Center = center,
            Extents = size * 0.5f
        };
    }

    /// <summary>
    /// Check if this AABB contains a point
    /// </summary>
    public bool Contains(float3 point)
    {
        float3 min = Min;
        float3 max = Max;
        return point.x >= min.x && point.x <= max.x &&
               point.y >= min.y && point.y <= max.y &&
               point.z >= min.z && point.z <= max.z;
    }

    /// <summary>
    /// Check if this AABB intersects another AABB
    /// </summary>
    public bool Intersects(AABB other)
    {
        float3 aMin = Min;
        float3 aMax = Max;
        float3 bMin = other.Min;
        float3 bMax = other.Max;

        return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
               (aMin.y <= bMax.y && aMax.y >= bMin.y) &&
               (aMin.z <= bMax.z && aMax.z >= bMin.z);
    }

    /// <summary>
    /// Get the size (width, height, depth) of the AABB
    /// </summary>
    public float3 Size => Extents * 2f;
}
