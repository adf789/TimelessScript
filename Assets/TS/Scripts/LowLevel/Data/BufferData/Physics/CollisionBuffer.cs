
using Unity.Entities;
using Unity.Mathematics;

// 충돌한 엔티티들을 저장하는 버퍼
public struct CollisionBuffer : IBufferElementData
{
    public Entity collidedEntity;
    public float2 separationVector;
    public bool isTrigger;
}