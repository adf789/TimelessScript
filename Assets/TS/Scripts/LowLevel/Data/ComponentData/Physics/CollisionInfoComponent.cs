
using Unity.Entities;
using Unity.Mathematics;

public struct CollisionInfoComponent : IComponentData
{
    public bool HasCollision;
    public float2 SeparationVector;
    public Entity CollidedEntity;
    public float2 ContactPoint;
}