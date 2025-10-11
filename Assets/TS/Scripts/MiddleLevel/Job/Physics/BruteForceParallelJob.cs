using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Brute Force 방식 충돌 검사 (병렬 처리)
/// 각 엔티티가 자신보다 인덱스가 큰 엔티티들과의 충돌을 검사하여 결과를 스트림에 저장합니다.
/// </summary>
[BurstCompile]
public struct BruteForceParallelJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Entity> allEntities;
    [ReadOnly] public NativeArray<ColliderBoundsComponent> allBounds;
    [ReadOnly] public NativeArray<ColliderComponent> allColliders;
    [ReadOnly] public ComponentLookup<TSGroundComponent> groundLookup;

    public NativeStream.Writer collisionResultStream;

    public void Execute(int entityAIndex)
    {
        collisionResultStream.BeginForEachIndex(entityAIndex);

        var entityA = allEntities[entityAIndex];
        var boundsA = allBounds[entityAIndex];
        var colliderA = allColliders[entityAIndex];

        // entityAIndex보다 큰 인덱스만 검사하여 중복 방지
        for (int j = entityAIndex + 1; j < allEntities.Length; j++)
        {
            var entityB = allEntities[j];
            var boundsB = allBounds[j];
            var colliderB = allColliders[j];

            // 레이어 체크
            if (!Utility.Physics.CheckAffectLayer(colliderA.Layer, colliderB.Layer))
                continue;

            // 충돌 검사
            if (Utility.Physics.BoundsIntersect(boundsA, boundsB))
            {
                float2 separationVector = Utility.Physics.GetSeparationVector(boundsA, boundsB);
                bool isTrigger = colliderA.IsTrigger || colliderB.IsTrigger;
                bool isGround = groundLookup.HasComponent(entityA) ||
                               groundLookup.HasComponent(entityB);

                // 스트림에 결과 쓰기 (스레드 안전)
                collisionResultStream.Write(new CollisionResult
                {
                    EntityA = entityA,
                    EntityB = entityB,
                    EntityAIndex = entityAIndex,
                    EntityBIndex = j,
                    SeparationVector = separationVector,
                    IsTrigger = isTrigger,
                    IsGroundCollision = isGround
                });
            }
        }

        collisionResultStream.EndForEachIndex();
    }
}
