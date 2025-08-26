
using Unity.Entities;
using Unity.Mathematics;

public struct CollisionInfoComponent : IComponentData
{
    public bool hasCollision;
    public float2 separationVector;
    public Entity collidedEntity;
    public float2 contactPoint;
}