using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct LightweightCollisionSystem : ISystem
{
    private CollisionSystemComponent collisionConfig;
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LightweightColliderComponent>();
        
        // 기본 설정
        collisionConfig = new CollisionSystemComponent
        {
            useSpacialHashing = true,
            cellSize = 5f
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Collider bounds 업데이트
        var updateBoundsJob = new UpdateColliderBoundsJob();
        var boundsJobHandle = updateBoundsJob.ScheduleParallel(state.Dependency);
        
        // bounds 업데이트 완료 대기
        boundsJobHandle.Complete();
        
        // 2. 모든 콜라이더 데이터 수집
        var colliderQuery = SystemAPI.QueryBuilder()
            .WithAll<LightweightColliderComponent, ColliderBounds>()
            .Build();
        
        var colliderEntities = colliderQuery.ToEntityArray(Allocator.TempJob);
        var colliderBounds = colliderQuery.ToComponentDataArray<ColliderBounds>(Allocator.TempJob);
        var colliderComponents = colliderQuery.ToComponentDataArray<LightweightColliderComponent>(Allocator.TempJob);
        
        // 3. 충돌 검사 Job
        var collisionDetectionJob = new CollisionDetectionJob
        {
            allEntities = colliderEntities,
            allBounds = colliderBounds,
            allColliders = colliderComponents,
            collisionBufferLookup = SystemAPI.GetBufferLookup<CollisionBuffer>(false),
            collisionInfoLookup = SystemAPI.GetComponentLookup<CollisionInfo>(false),
            useSpacialHashing = collisionConfig.useSpacialHashing,
            cellSize = collisionConfig.cellSize
        };
        collisionDetectionJob.Run();
        
        // 4. 메모리 정리 (Job 완료 후 System에서 직접 해제)
        colliderEntities.Dispose();
        colliderBounds.Dispose();
        colliderComponents.Dispose();
    }
}

[BurstCompile]
public partial struct UpdateColliderBoundsJob : IJobEntity
{
    public void Execute(
        ref LightweightColliderComponent collider,
        ref ColliderBounds bounds,
        in LocalTransform transform)
    {
        // 위치 업데이트
        collider.position = transform.Position.xy;
        
        // Bounds 계산
        float2 center = collider.position + collider.offset;
        float2 halfSize = collider.size * 0.5f;
        
        bounds.center = center;
        bounds.min = center - halfSize;
        bounds.max = center + halfSize;
    }
}

[BurstCompile]
public partial struct CollisionDetectionJob : IJobEntity
{
    [ReadOnly] public NativeArray<Entity> allEntities;
    [ReadOnly] public NativeArray<ColliderBounds> allBounds;
    [ReadOnly] public NativeArray<LightweightColliderComponent> allColliders;
    [NativeDisableParallelForRestriction] public BufferLookup<CollisionBuffer> collisionBufferLookup;
    public ComponentLookup<CollisionInfo> collisionInfoLookup;
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
        
        // 모든 entity 쌍에 대해 충돌 검사
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
                
                // 충돌 검사
                if (CollisionUtility.BoundsIntersect(boundsA, boundsB))
                {
                    float2 separationVector = new float2(0,0);
                    CollisionUtility.GetSeparationVector(boundsA, boundsB, out separationVector);
                    bool isTriggerCollision = colliderA.isTrigger || colliderB.isTrigger;
                    
                    // entityA에 충돌 데이터 추가
                    if (collisionBufferLookup.HasBuffer(entityA))
                    {
                        var bufferA = collisionBufferLookup[entityA];
                        bufferA.Add(new CollisionBuffer
                        {
                            collidedEntity = entityB,
                            separationVector = separationVector,
                            isTrigger = isTriggerCollision
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
                            isTrigger = isTriggerCollision
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
    }
}


// 공간 해시를 위한 별도 Job
[BurstCompile]
public partial struct SpatialHashUpdateJob : IJobEntity
{
    [ReadOnly] public float cellSize;
    
    public void Execute(
        ref SpatialHashKey hashKey,
        in LightweightColliderComponent collider)
    {
        hashKey.cellPosition = new int2(
            (int)math.floor(collider.position.x / cellSize),
            (int)math.floor(collider.position.y / cellSize)
        );
    }
}

// 유틸리티 함수들을 위한 정적 클래스
[BurstCompile]
public static class CollisionUtility
{
    [BurstCompile]
    public static bool BoundsIntersect(in ColliderBounds bounds1, in ColliderBounds bounds2)
    {
        return bounds1.min.x < bounds2.max.x && bounds1.max.x > bounds2.min.x &&
               bounds1.min.y < bounds2.max.y && bounds1.max.y > bounds2.min.y;
    }
    
    [BurstCompile]
    public static void GetSeparationVector(in ColliderBounds bounds1, in ColliderBounds bounds2, out float2 separation)
    {
        separation = float2.zero;
        
        float overlapX = math.min(bounds1.max.x, bounds2.max.x) - 
                        math.max(bounds1.min.x, bounds2.min.x);
        float overlapY = math.min(bounds1.max.y, bounds2.max.y) - 
                        math.max(bounds1.min.y, bounds2.min.y);
        
        if (overlapX < overlapY)
        {
            // X축으로 분리
            separation.x = bounds1.center.x < bounds2.center.x ? -overlapX : overlapX;
        }
        else
        {
            // Y축으로 분리
            separation.y = bounds1.center.y < bounds2.center.y ? -overlapY : overlapY;
        }
    }
}