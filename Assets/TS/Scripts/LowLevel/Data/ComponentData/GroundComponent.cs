using Unity.Entities;
using Unity.Mathematics;

public struct GroundComponent : IComponentData
{
    public float bounciness;
    public float friction;
    public bool isOneWayPlatform;
    public GroundType groundType;
}

public struct GroundCollisionData : IComponentData
{
    public float2 collisionNormal;
    public float2 incomingVelocity;
    public float2 responseVelocity;
    public bool hasCollision;
}