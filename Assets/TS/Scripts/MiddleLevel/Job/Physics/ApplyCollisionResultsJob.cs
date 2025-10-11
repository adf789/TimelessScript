using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

/// <summary>
/// 충돌 검사 결과를 실제 버퍼에 적용하는 Job (단일 스레드)
/// NativeStream에서 결과를 읽어 각 엔티티의 CollisionBuffer와 CollisionInfo를 업데이트합니다.
/// </summary>
[BurstCompile]
public struct ApplyCollisionResultsJob : IJob
{
    public NativeStream.Reader collisionResultStream;
    [NativeDisableParallelForRestriction]
    public BufferLookup<CollisionBuffer> collisionBufferLookup;
    public ComponentLookup<CollisionInfoComponent> collisionInfoLookup;

    public void Execute()
    {
        int forEachCount = collisionResultStream.ForEachCount;

        for (int streamIndex = 0; streamIndex < forEachCount; streamIndex++)
        {
            int itemCount = collisionResultStream.BeginForEachIndex(streamIndex);

            for (int i = 0; i < itemCount; i++)
            {
                var result = collisionResultStream.Read<CollisionResult>();

                // EntityA에 충돌 데이터 추가
                if (collisionBufferLookup.HasBuffer(result.EntityA))
                {
                    var bufferA = collisionBufferLookup[result.EntityA];
                    bufferA.Add(new CollisionBuffer
                    {
                        CollidedEntity = result.EntityB,
                        SeparationVector = result.SeparationVector,
                        IsTrigger = result.IsTrigger,
                        IsGroundCollision = result.IsGroundCollision
                    });
                }

                // EntityB에 반대 방향 충돌 데이터 추가
                if (collisionBufferLookup.HasBuffer(result.EntityB))
                {
                    var bufferB = collisionBufferLookup[result.EntityB];
                    bufferB.Add(new CollisionBuffer
                    {
                        CollidedEntity = result.EntityA,
                        SeparationVector = -result.SeparationVector,
                        IsTrigger = result.IsTrigger,
                        IsGroundCollision = result.IsGroundCollision
                    });
                }

                // EntityA의 CollisionInfo 업데이트
                if (collisionInfoLookup.HasComponent(result.EntityA))
                {
                    var infoA = collisionInfoLookup[result.EntityA];
                    infoA.HasCollision = true;
                    infoA.CollidedEntity = result.EntityB;
                    infoA.SeparationVector = result.SeparationVector;
                    collisionInfoLookup[result.EntityA] = infoA;
                }

                // EntityB의 CollisionInfo 업데이트
                if (collisionInfoLookup.HasComponent(result.EntityB))
                {
                    var infoB = collisionInfoLookup[result.EntityB];
                    infoB.HasCollision = true;
                    infoB.CollidedEntity = result.EntityA;
                    infoB.SeparationVector = -result.SeparationVector;
                    collisionInfoLookup[result.EntityB] = infoB;
                }
            }

            collisionResultStream.EndForEachIndex();
        }
    }
}
