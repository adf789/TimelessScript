using Unity.Entities;
using Unity.Mathematics;

public struct TSGroundComponent : IComponentData
{
    public float Bounciness;
    public float Friction;
    public GroundType GroundType;
}