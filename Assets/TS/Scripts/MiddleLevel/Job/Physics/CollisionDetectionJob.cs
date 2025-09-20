
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
    [ReadOnly] public ComponentLookup<GroundComponent> GroundLookup;
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
                info.hasCollision = false;
                info.collidedEntity = Entity.Null;
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
        // Spatial Hash를 사용하여 같은 셀과 인접 셀에 있는 entity들만 검사
        var spatialHashMap = new NativeParallelMultiHashMap<int2, int>(allEntities.Length, Allocator.Temp);

        // 모든 entity를 spatial hash에 등록
        for (int i = 0; i < allEntities.Length; i++)
        {
            var hashKey = allHashKeys[i];
            spatialHashMap.Add(hashKey.cellPosition, i);
        }

        // 각 entity에 대해 충돌 검사
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entityA = allEntities[i];
            var boundsA = allBounds[i];
            var colliderA = allColliders[i];
            var hashKeyA = allHashKeys[i];

            // 현재 셀과 인접 8개 셀을 검사
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var neighborCell = hashKeyA.cellPosition + new int2(x, y);

                    if (spatialHashMap.TryGetFirstValue(neighborCell, out int entityBIndex, out var iterator))
                    {
                        do
                        {
                            // 같은 entity는 건너뛰기
                            if (i == entityBIndex) continue;

                            var entityB = allEntities[entityBIndex];
                            var boundsB = allBounds[entityBIndex];
                            var colliderB = allColliders[entityBIndex];

                            // 이미 검사한 쌍은 건너뛰기 (i < j 조건으로 중복 제거)
                            if (i >= entityBIndex) continue;

                            // 충돌 검사 및 처리
                            CheckAndProcessCollision(entityA, entityB, boundsA, boundsB, colliderA, colliderB);

                        } while (spatialHashMap.TryGetNextValue(out entityBIndex, ref iterator));
                    }
                }
            }
        }

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
        // 충돌 검사
        if (Utility.Physics.BoundsIntersect(boundsA, boundsB))
        {
            float2 separationVector = Utility.Physics.GetSeparationVector(boundsA, boundsB);
            bool isTriggerCollision = colliderA.isTrigger || colliderB.isTrigger;
            bool isGroundCollision = GroundLookup.HasComponent(entityA) || GroundLookup.HasComponent(entityB);

            // entityA에 충돌 데이터 추가
            if (collisionBufferLookup.HasBuffer(entityA))
            {
                var bufferA = collisionBufferLookup[entityA];
                bufferA.Add(new CollisionBuffer
                {
                    collidedEntity = entityB,
                    separationVector = separationVector,
                    isTrigger = isTriggerCollision,
                    isGroundCollision = isGroundCollision
                });
            }

            // entityB에 반대 방향 충돌 데이터 추가
            if (collisionBufferLookup.HasBuffer(entityB))
            {
                var bufferB = collisionBufferLookup[entityB];
                bufferB.Add(new CollisionBuffer
                {
                    collidedEntity = entityA,
                    separationVector = -separationVector,
                    isTrigger = isTriggerCollision,
                    isGroundCollision = isGroundCollision
                });
            }

            // CollisionInfo 업데이트
            if (collisionInfoLookup.HasComponent(entityA))
            {
                var infoA = collisionInfoLookup[entityA];
                infoA.hasCollision = true;
                infoA.collidedEntity = entityB;
                infoA.separationVector = separationVector;
                collisionInfoLookup[entityA] = infoA;
            }

            if (collisionInfoLookup.HasComponent(entityB))
            {
                var infoB = collisionInfoLookup[entityB];
                infoB.hasCollision = true;
                infoB.collidedEntity = entityA;
                infoB.separationVector = -separationVector;
                collisionInfoLookup[entityB] = infoB;
            }
        }
    }
}