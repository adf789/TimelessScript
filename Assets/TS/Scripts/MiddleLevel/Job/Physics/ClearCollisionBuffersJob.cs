using Unity.Burst;
using Unity.Entities;

/// <summary>
/// 충돌 버퍼 초기화 Job (병렬 처리)
/// 각 엔티티의 CollisionBuffer와 CollisionInfo를 초기화합니다.
/// </summary>
[BurstCompile]
public partial struct ClearCollisionBuffersJob : IJobEntity
{
    public void Execute(
        ref DynamicBuffer<CollisionBuffer> collisionBuffer,
        ref CollisionInfoComponent collisionInfo)
    {
        collisionBuffer.Clear();
        collisionInfo.HasCollision = false;
        collisionInfo.CollidedEntity = Entity.Null;
    }
}
