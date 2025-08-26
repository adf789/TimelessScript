
using Unity.Entities;
using Unity.Mathematics;

public struct GroundCollisionComponent : IComponentData
{
    public float2 collisionNormal;
    public float2 incomingVelocity;
    public float2 responseVelocity;
    public bool hasCollision;
}