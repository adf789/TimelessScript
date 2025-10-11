using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// 수집된 충돌 쌍을 검사하는 Job (병렬 처리)
/// 각 쌍에 대해 실제 충돌 여부를 검사하고 결과를 스트림에 저장합니다.
/// </summary>
[BurstCompile]
public struct CheckCollisionPairsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<CollisionPair> pairs;
    [ReadOnly] public NativeArray<Entity> allEntities;
    [ReadOnly] public NativeArray<ColliderBoundsComponent> allBounds;
    [ReadOnly] public NativeArray<ColliderComponent> allColliders;
    [ReadOnly] public ComponentLookup<TSGroundComponent> groundLookup;

    public NativeStream.Writer collisionResults;

    public void Execute(int pairIndex)
    {
        collisionResults.BeginForEachIndex(pairIndex);

        var pair = pairs[pairIndex];
        var entityA = allEntities[pair.IndexA];
        var entityB = allEntities[pair.IndexB];
        var boundsA = allBounds[pair.IndexA];
        var boundsB = allBounds[pair.IndexB];
        var colliderA = allColliders[pair.IndexA];
        var colliderB = allColliders[pair.IndexB];

        // 레이어 체크
        if (!Utility.Physics.CheckAffectLayer(colliderA.Layer, colliderB.Layer))
        {
            collisionResults.EndForEachIndex();
            return;
        }

        // 충돌 검사
        if (Utility.Physics.BoundsIntersect(boundsA, boundsB))
        {
            float2 separationVector = Utility.Physics.GetSeparationVector(boundsA, boundsB);
            bool isTrigger = colliderA.IsTrigger || colliderB.IsTrigger;
            bool isGround = groundLookup.HasComponent(entityA) ||
                           groundLookup.HasComponent(entityB);

            // 스트림에 결과 쓰기
            collisionResults.Write(new CollisionResult
            {
                EntityA = entityA,
                EntityB = entityB,
                EntityAIndex = pair.IndexA,
                EntityBIndex = pair.IndexB,
                SeparationVector = separationVector,
                IsTrigger = isTrigger,
                IsGroundCollision = isGround
            });
        }

        collisionResults.EndForEachIndex();
    }
}
