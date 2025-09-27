
using Unity.Entities;
using Unity.Mathematics;

// 충돌한 엔티티들을 저장하는 버퍼
public struct CollisionBuffer : IBufferElementData
{
    public Entity CollidedEntity;
    public float2 SeparationVector;
    public bool IsTrigger;
    public bool IsGroundCollision; // 지면 충돌 여부
}