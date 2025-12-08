using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile(CompileSynchronously = true,
              OptimizeFor = OptimizeFor.Performance,
              FloatMode = FloatMode.Fast,
              FloatPrecision = FloatPrecision.Low)]
public partial struct OptimizedPhysicsSystem : ISystem
{
    private EntityQuery actorQuery;
    private EntityQuery environmentQuery;
    private EntityQuery groundQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Actor 쿼리: 물리 + 충돌 컴포넌트를 가진 엔티티
        actorQuery = SystemAPI.QueryBuilder()
            .WithAll<PhysicsComponent, ColliderComponent, ColliderBoundsComponent, LocalTransform, TSActorComponent>()
            .Build();

        // Environment 쿼리: collider가 변경될 수 있는 오브젝트
        environmentQuery = SystemAPI.QueryBuilder()
            .WithAll<ColliderComponent, ColliderBoundsComponent, LocalTransform>()
            .WithAny<TSGroundComponent, TSLadderComponent, TSGimmickComponent>() // Ground 또는 Object (Ladder 등)
            .Build();

        // Ground 쿼리: Actor와 충돌할 수 있는 Static 오브젝트
        groundQuery = SystemAPI.QueryBuilder()
            .WithAll<ColliderComponent, ColliderBoundsComponent, LocalTransform>()
            .WithAny<TSGroundComponent, TSLadderComponent>()
            .Build();

        // state.RequireForUpdate(actorQuery);
        // RequireForUpdate(groundQuery) 제거: Ground Entity가 프레임 중간에 삭제될 수 있어 안전하지 않음
    }

    [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
    public void OnUpdate(ref SystemState state)
    {
        // 물리 시뮬레이션 실행
        OnUpdatePhysics(ref state);
    }

    [BurstCompile]
    private void OnUpdatePhysics(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Gimmick Bounds 업데이트
        var updateJob = new UpdateColliderBoundsJob();
        state.Dependency = updateJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        var groundEntities = groundQuery.ToEntityArray(Allocator.TempJob);
        var groundBounds = groundQuery.ToComponentDataArray<ColliderBoundsComponent>(Allocator.TempJob);

        // ComponentLookup 생성 및 업데이트 (구조 변경 후 최신 상태 반영)
        var groundLookup = SystemAPI.GetComponentLookup<TSGroundComponent>(true);
        var objectLookup = SystemAPI.GetComponentLookup<TSObjectComponent>(true);
        var colliderLookup = SystemAPI.GetComponentLookup<ColliderComponent>(true);

        // Entity 구조 변경 후 Lookup 업데이트 (필수)
        groundLookup.Update(ref state);
        objectLookup.Update(ref state);
        colliderLookup.Update(ref state);

        // 최적화된 물리 Job 실행 (Actor별 병렬 처리)
        var physicsJob = new OptimizedPhysicsJob
        {
            DeltaTime = deltaTime,
            GroundEntities = groundEntities,
            ColliderBounds = groundBounds,
            GroundLookup = groundLookup,
            ObjectLookup = objectLookup,
            ColliderLookup = colliderLookup
        };

        state.Dependency = physicsJob.ScheduleParallel(actorQuery, state.Dependency);
        state.Dependency.Complete();

        groundEntities.Dispose();
    }
}
