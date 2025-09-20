using Unity.Entities;
using Unity.Mathematics;

public struct ColliderComponent : IComponentData
{
    public float2 size;
    public float2 offset;
    public bool isTrigger;
    public float2 position;
}