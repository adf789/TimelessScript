
using Unity.Entities;
using Unity.Mathematics;

public struct ColliderBoundsComponent : IComponentData
{
    public float2 Center;
    public float2 Min;
    public float2 Max;

    public float2 Size => new float2(Max.x - Min.x, Max.y - Min.y);
    public float2 RootPosition => new float2(Center.x, Min.y);
}