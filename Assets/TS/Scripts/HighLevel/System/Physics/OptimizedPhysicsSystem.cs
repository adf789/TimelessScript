using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// 2D 플랫포머에 최적화된 물리 시스템
/// - Actor-Ground 전용 충돌 (단방향)
/// - Static Ground 캐싱 (매 프레임 할당 제거)
/// - Burst 최대 최적화
/// - 700+ 엔티티 목표: 60 FPS
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile(CompileSynchronously = true,
              OptimizeFor = OptimizeFor.Performance,
              FloatMode = FloatMode.Fast,
              FloatPrecision = FloatPrecision.Low)]
public partial struct OptimizedPhysicsSystem : ISystem
{
    private EntityQuery actorQuery;
    private EntityQuery groundQuery;

    // Ground는 Static이므로 캐싱 (매 프레임 할당 제거)
    private NativeArray<Entity> cachedGroundEntities;
    private NativeArray<ColliderBoundsComponent> cachedGroundBounds;
    private bool groundCacheInitialized;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Actor 쿼리: 물리 + 충돌 컴포넌트를 가진 엔티티
        actorQuery = SystemAPI.QueryBuilder()
            .WithAll<PhysicsComponent, ColliderComponent, ColliderBoundsComponent, LocalTransform>()
            .WithNone<TSGroundComponent>() // Ground는 제외
            .Build();

        // Ground 쿼리: Actor와 충돌할 수 있는 Static 오브젝트
        groundQuery = SystemAPI.QueryBuilder()
            .WithAll<ColliderComponent, ColliderBoundsComponent, LocalTransform>()
            .WithAny<TSGroundComponent, TSLadderAuthoring>() // Ground 또는 Object (Ladder 등)
            .Build();

        state.RequireForUpdate(actorQuery);
        groundCacheInitialized = false;
    }

    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public void OnUpdate(ref SystemState state)
    {
        // Ground 캐시 초기화 (최초 1회만)
        if (!groundCacheInitialized || !cachedGroundEntities.IsCreated)
        {
            InitializeGroundCache(ref state);
        }

        float deltaTime = SystemAPI.Time.DeltaTime;

        // 최적화된 물리 Job 실행 (Actor별 병렬 처리)
        var physicsJob = new OptimizedPhysicsJob
        {
            deltaTime = deltaTime,
            groundEntities = cachedGroundEntities,
            groundBounds = cachedGroundBounds,
            groundLookup = SystemAPI.GetComponentLookup<TSGroundComponent>(true),
            objectLookup = SystemAPI.GetComponentLookup<TSObjectComponent>(true),
            colliderLookup = SystemAPI.GetComponentLookup<ColliderComponent>(true)
        };

        state.Dependency = physicsJob.ScheduleParallel(actorQuery, state.Dependency);
    }

    /// <summary>
    /// Ground 캐시 초기화 (Static이므로 최초 1회만)
    /// </summary>
    [BurstCompile]
    private void InitializeGroundCache(ref SystemState state)
    {
        // 기존 캐시 해제
        if (cachedGroundEntities.IsCreated)
            cachedGroundEntities.Dispose();
        if (cachedGroundBounds.IsCreated)
            cachedGroundBounds.Dispose();

        // Ground Bounds 업데이트
        var updateJob = new UpdateGroundBoundsJob();
        state.Dependency = updateJob.ScheduleParallel(groundQuery, state.Dependency);
        state.Dependency.Complete();

        // Ground 데이터 캐싱 (Persistent 할당)
        cachedGroundEntities = groundQuery.ToEntityArray(Allocator.Persistent);
        cachedGroundBounds = groundQuery.ToComponentDataArray<ColliderBoundsComponent>(Allocator.Persistent);

        groundCacheInitialized = true;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // 캐시 메모리 해제
        if (cachedGroundEntities.IsCreated)
            cachedGroundEntities.Dispose();
        if (cachedGroundBounds.IsCreated)
            cachedGroundBounds.Dispose();
    }
}

/// <summary>
/// Ground Bounds 업데이트 Job
/// </summary>
[BurstCompile(CompileSynchronously = true,
              OptimizeFor = OptimizeFor.Performance,
              FloatMode = FloatMode.Fast,
              FloatPrecision = FloatPrecision.Low)]
public partial struct UpdateGroundBoundsJob : IJobEntity
{
    public void Execute(
        ref ColliderBoundsComponent bounds,
        in LocalTransform transform,
        in ColliderComponent collider)
    {
        var position = transform.Position.xy;
        bounds.Center = position + collider.Offset;
        var halfSize = collider.Size * 0.5f;
        bounds.Min = bounds.Center - halfSize;
        bounds.Max = bounds.Center + halfSize;
    }
}
