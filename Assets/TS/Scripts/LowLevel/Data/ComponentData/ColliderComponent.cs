using Unity.Entities;
using Unity.Mathematics;

public struct LightweightColliderComponent : IComponentData
{
    public float2 size;
    public float2 offset;
    public bool isTrigger;
    public float2 position;
}

public struct ColliderBounds : IComponentData
{
    public float2 center;
    public float2 min;
    public float2 max;
}

public struct CollisionInfo : IComponentData
{
    public bool hasCollision;
    public float2 separationVector;
    public Entity collidedEntity;
    public float2 contactPoint;
}

// 충돌한 엔티티들을 저장하는 버퍼
public struct CollisionBuffer : IBufferElementData
{
    public Entity collidedEntity;
    public float2 separationVector;
    public bool isTrigger;
}