
using Unity.Entities;
using Unity.Mathematics;

public struct ColliderBoundsComponent : IComponentData
{
    public float2 center;
    public float2 min;
    public float2 max;
}