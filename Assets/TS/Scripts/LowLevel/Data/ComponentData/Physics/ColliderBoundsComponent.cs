
using Unity.Entities;
using Unity.Mathematics;

public struct ColliderBoundsComponent : IComponentData
{
    public float2 Center;
    public float2 Min;
    public float2 Max;
}