using Unity.Entities;
using Unity.Mathematics;

public struct ColliderComponent : IComponentData
{
    public ColliderLayer Layer;
    public float2 Size;
    public float2 Offset;
    public bool IsTrigger;
}