using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[DisableAutoCreation]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystem))]
[BurstCompile]
public partial struct CollisionSystem : ISystem
{
    // 병렬 처리 배치 크기 (성능 튜닝 가능)
    private const int PARALLEL_BATCH_SIZE = 32;

    // Spatial hash map 크기 계산 배수
    private const int SPATIAL_HASH_CAPACITY_MULTIPLIER = 4;

    private CollisionSystemConfig collisionConfig;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ColliderComponent>();

        // 기본 설정
        collisionConfig = new CollisionSystemConfig
        {
            useSpacialHashing = false,
            cellSize = 10f
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Collider bounds 및 버퍼 초기화 (병렬)
        UpdateBoundsAndClearBuffers(ref state);

        // 2. Spatial Hash 업데이트 (Spatial Hashing 사용 시)
        if (collisionConfig.useSpacialHashing)
        {
            UpdateSpatialHash(ref state);
        }

        // 3. 콜라이더 데이터 수집
        var colliderQuery = BuildColliderQuery(ref state, collisionConfig.useSpacialHashing);
        var colliderEntities = colliderQuery.ToEntityArray(Allocator.TempJob);
        var colliderBounds = colliderQuery.ToComponentDataArray<ColliderBoundsComponent>(Allocator.TempJob);
        var colliderComponents = colliderQuery.ToComponentDataArray<ColliderComponent>(Allocator.TempJob);

        // 4. 충돌 검사 파이프라인 실행
        if (collisionConfig.useSpacialHashing)
        {
            ExecuteSpatialHashingPipeline(ref state, colliderQuery, colliderEntities, colliderBounds, colliderComponents);
        }
        else
        {
            ExecuteBruteForcePipeline(ref state, colliderEntities, colliderBounds, colliderComponents);
        }

        // 5. 메모리 정리
        DisposeColliderData(colliderEntities, colliderBounds, colliderComponents);
    }

    /// <summary>
    /// Bounds 업데이트 및 충돌 버퍼 초기화
    /// </summary>
    private void UpdateBoundsAndClearBuffers(ref SystemState state)
    {
        // Bounds 업데이트
        var updateBoundsJob = new UpdateColliderBoundsJob();
        var boundsJobHandle = updateBoundsJob.ScheduleParallel(state.Dependency);
        boundsJobHandle.Complete();

        // 충돌 버퍼 초기화
        var clearBuffersJob = new ClearCollisionBuffersJob();
        var clearJobHandle = clearBuffersJob.ScheduleParallel(state.Dependency);
        clearJobHandle.Complete();
    }

    /// <summary>
    /// Spatial Hash 업데이트
    /// </summary>
    private void UpdateSpatialHash(ref SystemState state)
    {
        var spatialHashUpdateJob = new SpatialHashUpdateJob
        {
            cellSize = collisionConfig.cellSize
        };
        var spatialHashJobHandle = spatialHashUpdateJob.ScheduleParallel(state.Dependency);
        spatialHashJobHandle.Complete();
    }

    /// <summary>
    /// 콜라이더 쿼리 생성
    /// </summary>
    private EntityQuery BuildColliderQuery(ref SystemState state, bool useSpatialHashing)
    {
        if (useSpatialHashing)
        {
            return SystemAPI.QueryBuilder()
                .WithAll<ColliderComponent, ColliderBoundsComponent, SpatialHashKeyComponent>()
                .Build();
        }
        else
        {
            return SystemAPI.QueryBuilder()
                .WithAll<ColliderComponent, ColliderBoundsComponent>()
                .Build();
        }
    }

    /// <summary>
    /// 콜라이더 데이터 메모리 해제
    /// </summary>
    private void DisposeColliderData(
        NativeArray<Entity> entities,
        NativeArray<ColliderBoundsComponent> bounds,
        NativeArray<ColliderComponent> components)
    {
        entities.Dispose();
        bounds.Dispose();
        components.Dispose();
    }

    /// <summary>
    /// Spatial Hashing 병렬 파이프라인 실행
    /// </summary>
    private void ExecuteSpatialHashingPipeline(
        ref SystemState state,
        EntityQuery colliderQuery,
        NativeArray<Entity> colliderEntities,
        NativeArray<ColliderBoundsComponent> colliderBounds,
        NativeArray<ColliderComponent> colliderComponents)
    {
        var spatialHashKeys = colliderQuery.ToComponentDataArray<SpatialHashKeyComponent>(Allocator.TempJob);

        // Phase 1: Spatial hash map 생성
        var spatialHashMap = new NativeParallelMultiHashMap<Unity.Mathematics.int2, int>(
            colliderEntities.Length * SPATIAL_HASH_CAPACITY_MULTIPLIER, Allocator.TempJob);

        for (int i = 0; i < colliderEntities.Length; i++)
        {
            var hashKey = spatialHashKeys[i];
            for (int x = hashKey.MinCell.x; x <= hashKey.MaxCell.x; x++)
            {
                for (int y = hashKey.MinCell.y; y <= hashKey.MaxCell.y; y++)
                {
                    spatialHashMap.Add(new Unity.Mathematics.int2(x, y), i);
                }
            }
        }

        // Phase 2: 충돌 가능 쌍 수집 (병렬)
        var pairQueue = new NativeQueue<CollisionPair>(Allocator.TempJob);
        var collectPairsJob = new CollectCollisionPairsJob
        {
            allHashKeys = spatialHashKeys,
            spatialHash = spatialHashMap,
            pairQueue = pairQueue.AsParallelWriter()
        };
        var collectJobHandle = collectPairsJob.Schedule(colliderEntities.Length, PARALLEL_BATCH_SIZE, state.Dependency);
        collectJobHandle.Complete();

        // Phase 3: 중복 제거
        var uniquePairs = RemoveDuplicatePairs(pairQueue);
        pairQueue.Dispose();

        // Phase 4: 충돌 검사 (병렬)
        var collisionStream = new NativeStream(uniquePairs.Length, Allocator.TempJob);
        var checkPairsJob = new CheckCollisionPairsJob
        {
            pairs = uniquePairs.AsArray(),
            allEntities = colliderEntities,
            allBounds = colliderBounds,
            allColliders = colliderComponents,
            groundLookup = SystemAPI.GetComponentLookup<TSGroundComponent>(true),
            collisionResults = collisionStream.AsWriter()
        };
        var checkJobHandle = checkPairsJob.Schedule(uniquePairs.Length, PARALLEL_BATCH_SIZE, state.Dependency);
        checkJobHandle.Complete();

        // Phase 5: 결과 적용
        var applyResultsJob = new ApplyCollisionResultsJob
        {
            collisionResultStream = collisionStream.AsReader(),
            collisionBufferLookup = SystemAPI.GetBufferLookup<CollisionBuffer>(false),
            collisionInfoLookup = SystemAPI.GetComponentLookup<CollisionInfoComponent>(false)
        };
        var applyJobHandle = applyResultsJob.Schedule(state.Dependency);
        applyJobHandle.Complete();

        // 메모리 정리
        spatialHashKeys.Dispose();
        spatialHashMap.Dispose();
        uniquePairs.Dispose();
        collisionStream.Dispose();
    }

    /// <summary>
    /// Brute Force 병렬 파이프라인 실행
    /// </summary>
    private void ExecuteBruteForcePipeline(
        ref SystemState state,
        NativeArray<Entity> colliderEntities,
        NativeArray<ColliderBoundsComponent> colliderBounds,
        NativeArray<ColliderComponent> colliderComponents)
    {
        var collisionStream = new NativeStream(colliderEntities.Length, Allocator.TempJob);

        // Phase 1: 충돌 검사 (병렬)
        var bruteForceJob = new BruteForceParallelJob
        {
            allEntities = colliderEntities,
            allBounds = colliderBounds,
            allColliders = colliderComponents,
            groundLookup = SystemAPI.GetComponentLookup<TSGroundComponent>(true),
            collisionResultStream = collisionStream.AsWriter()
        };
        var bruteForceJobHandle = bruteForceJob.Schedule(colliderEntities.Length, PARALLEL_BATCH_SIZE, state.Dependency);
        bruteForceJobHandle.Complete();

        // Phase 2: 결과 적용
        var applyResultsJob = new ApplyCollisionResultsJob
        {
            collisionResultStream = collisionStream.AsReader(),
            collisionBufferLookup = SystemAPI.GetBufferLookup<CollisionBuffer>(false),
            collisionInfoLookup = SystemAPI.GetComponentLookup<CollisionInfoComponent>(false)
        };
        var applyJobHandle = applyResultsJob.Schedule(state.Dependency);
        applyJobHandle.Complete();

        collisionStream.Dispose();
    }

    /// <summary>
    /// 충돌 쌍 중복 제거 (단일 스레드)
    /// </summary>
    private NativeList<CollisionPair> RemoveDuplicatePairs(NativeQueue<CollisionPair> pairQueue)
    {
        var uniquePairs = new NativeList<CollisionPair>(pairQueue.Count, Allocator.TempJob);
        var seen = new NativeHashSet<CollisionPair>(pairQueue.Count, Allocator.Temp);

        while (pairQueue.TryDequeue(out var pair))
        {
            if (seen.Add(pair))
            {
                uniquePairs.Add(pair);
            }
        }

        seen.Dispose();
        return uniquePairs;
    }
}
