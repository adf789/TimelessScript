
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct CollisionDetectionJob : IJob
{
    [ReadOnly] public NativeArray<Entity> allEntities;
    [ReadOnly] public NativeArray<ColliderBoundsComponent> allBounds;
    [ReadOnly] public NativeArray<ColliderComponent> allColliders;
    [ReadOnly] public NativeArray<SpatialHashKeyComponent> allHashKeys;
    [NativeDisableParallelForRestriction] public BufferLookup<CollisionBuffer> collisionBufferLookup;
    public ComponentLookup<CollisionInfoComponent> collisionInfoLookup;
    [ReadOnly] public ComponentLookup<TSGroundComponent> GroundLookup;
    [ReadOnly] public bool useSpacialHashing;
    [ReadOnly] public float cellSize;

    public void Execute()
    {
        // 모든 entity의 충돌 버퍼 초기화
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            if (collisionBufferLookup.HasBuffer(entity))
            {
                var buffer = collisionBufferLookup[entity];
                buffer.Clear();
            }

            if (collisionInfoLookup.HasComponent(entity))
            {
                var info = collisionInfoLookup[entity];
                info.HasCollision = false;
                info.CollidedEntity = Entity.Null;
                collisionInfoLookup[entity] = info;
            }
        }

        if (useSpacialHashing)
        {
            // Spatial Hashing을 사용한 충돌 검사
            ExecuteWithSpatialHashing();
        }
        else
        {
            // 브루트 포스 충돌 검사
            ExecuteBruteForce();
        }
    }

    private void ExecuteWithSpatialHashing()
    {
        // Spatial Hash를 사용하여 Collider 크기를 고려한 충돌 검사
        var spatialHashMap = new NativeParallelMultiHashMap<int2, int>(allEntities.Length * 4, Allocator.Temp);

        // 모든 entity를 spatial hash에 등록 (각 entity가 차지하는 모든 셀에 등록)
        for (int i = 0; i < allEntities.Length; i++)
        {
            var hashKey = allHashKeys[i];

            // Collider가 차지하는 모든 셀에 entity 등록
            for (int x = hashKey.MinCell.x; x <= hashKey.MaxCell.x; x++)
            {
                for (int y = hashKey.MinCell.y; y <= hashKey.MaxCell.y; y++)
                {
                    spatialHashMap.Add(new int2(x, y), i);
                }
            }
        }

        // 중복 검사를 방지하기 위한 HashSet
        var checkedPairs = new NativeHashSet<int2>(allEntities.Length * allEntities.Length / 2, Allocator.Temp);

        // 각 entity에 대해 충돌 검사
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entityA = allEntities[i];
            var boundsA = allBounds[i];
            var colliderA = allColliders[i];
            var hashKeyA = allHashKeys[i];

            // 해당 entity가 차지하는 모든 셀을 검사
            for (int x = hashKeyA.MinCell.x; x <= hashKeyA.MaxCell.x; x++)
            {
                for (int y = hashKeyA.MinCell.y; y <= hashKeyA.MaxCell.y; y++)
                {
                    var cell = new int2(x, y);

                    if (spatialHashMap.TryGetFirstValue(cell, out int entityBIndex, out var iterator))
                    {
                        do
                        {
                            // 같은 엔티티는 건너뛰기
                            if (i == entityBIndex) continue;

                            // 중복 검사 방지 (i와 j의 순서를 정규화)
                            int minIndex = math.min(i, entityBIndex);
                            int maxIndex = math.max(i, entityBIndex);
                            var pairKey = new int2(minIndex, maxIndex);

                            if (checkedPairs.Contains(pairKey)) continue;
                            checkedPairs.Add(pairKey);

                            var entityB = allEntities[entityBIndex];
                            var boundsB = allBounds[entityBIndex];
                            var colliderB = allColliders[entityBIndex];

                            // 충돌 검사 및 처리
                            CheckAndProcessCollision(entityA, entityB, boundsA, boundsB, colliderA, colliderB);

                        } while (spatialHashMap.TryGetNextValue(out entityBIndex, ref iterator));
                    }
                }
            }
        }

        checkedPairs.Dispose();
        spatialHashMap.Dispose();
    }

    private void ExecuteBruteForce()
    {
        // 기존 브루트 포스 방식
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entityA = allEntities[i];
            var boundsA = allBounds[i];
            var colliderA = allColliders[i];

            for (int j = i + 1; j < allEntities.Length; j++)
            {
                var entityB = allEntities[j];
                var boundsB = allBounds[j];
                var colliderB = allColliders[j];

                CheckAndProcessCollision(entityA, entityB, boundsA, boundsB, colliderA, colliderB);
            }
        }
    }

    private void CheckAndProcessCollision(Entity entityA, Entity entityB,
        ColliderBoundsComponent boundsA, ColliderBoundsComponent boundsB,
        ColliderComponent colliderA, ColliderComponent colliderB)
    {
        if (!Utility.Physics.CheckAffectLayer(colliderA.Layer, colliderB.Layer))
            return;

        // 충돌 검사
        if (Utility.Physics.BoundsIntersect(boundsA, boundsB))
        {
            float2 separationVector = Utility.Physics.GetSeparationVector(boundsA, boundsB);
            bool isTriggerCollision = colliderA.IsTrigger || colliderB.IsTrigger;
            bool isGroundCollision = GroundLookup.HasComponent(entityA) || GroundLookup.HasComponent(entityB);

            // entityA에 충돌 데이터 추가
            if (collisionBufferLookup.HasBuffer(entityA))
            {
                var bufferA = collisionBufferLookup[entityA];
                bufferA.Add(new CollisionBuffer
                {
                    CollidedEntity = entityB,
                    SeparationVector = separationVector,
                    IsTrigger = isTriggerCollision,
                    IsGroundCollision = isGroundCollision
                });
            }

            // entityB에 반대 방향 충돌 데이터 추가
            if (collisionBufferLookup.HasBuffer(entityB))
            {
                var bufferB = collisionBufferLookup[entityB];
                bufferB.Add(new CollisionBuffer
                {
                    CollidedEntity = entityA,
                    SeparationVector = -separationVector,
                    IsTrigger = isTriggerCollision,
                    IsGroundCollision = isGroundCollision
                });
            }

            // CollisionInfo 업데이트
            if (collisionInfoLookup.HasComponent(entityA))
            {
                var infoA = collisionInfoLookup[entityA];
                infoA.HasCollision = true;
                infoA.CollidedEntity = entityB;
                infoA.SeparationVector = separationVector;
                collisionInfoLookup[entityA] = infoA;
            }

            if (collisionInfoLookup.HasComponent(entityB))
            {
                var infoB = collisionInfoLookup[entityB];
                infoB.HasCollision = true;
                infoB.CollidedEntity = entityA;
                infoB.SeparationVector = -separationVector;
                collisionInfoLookup[entityB] = infoB;
            }
        }
    }
}