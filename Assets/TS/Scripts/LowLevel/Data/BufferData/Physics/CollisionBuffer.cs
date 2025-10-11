
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

// 병렬 충돌 검사 결과를 임시로 저장하는 구조체 (NativeStream용)
public struct CollisionResult
{
    public Entity EntityA;
    public Entity EntityB;
    public int EntityAIndex;
    public int EntityBIndex;
    public float2 SeparationVector;
    public bool IsTrigger;
    public bool IsGroundCollision;
}

// 충돌 가능한 엔티티 쌍 (인덱스로 저장)
public struct CollisionPair : System.IEquatable<CollisionPair>
{
    public int IndexA;
    public int IndexB;

    public bool Equals(CollisionPair other)
    {
        return IndexA == other.IndexA && IndexB == other.IndexB;
    }

    public override int GetHashCode()
    {
        return IndexA * 397 ^ IndexB;
    }
}