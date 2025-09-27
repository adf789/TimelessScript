
using Unity.Entities;
using Unity.Mathematics;

public struct GroundCollisionComponent : IComponentData
{
    public float2 CollisionNormal;
    public float2 IncomingVelocity;
    public float2 ResponseVelocity;
    public bool HasCollision;
}