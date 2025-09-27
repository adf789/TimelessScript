using Unity.Entities;
using Unity.Mathematics;

public struct TSGroundComponent : IComponentData
{
    public float bounciness;
    public float friction;
    public GroundType groundType;
}