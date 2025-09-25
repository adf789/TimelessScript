using Unity.Entities;
using Unity.Mathematics;

public struct TSGroundComponent : IComponentData
{
    public float bounciness;
    public float friction;
    public bool isOneWayPlatform;
    public GroundType groundType;
}