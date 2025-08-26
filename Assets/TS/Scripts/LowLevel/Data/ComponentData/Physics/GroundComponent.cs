using Unity.Entities;
using Unity.Mathematics;

public struct GroundComponent : IComponentData
{
    public float bounciness;
    public float friction;
    public bool isOneWayPlatform;
    public GroundType groundType;
}