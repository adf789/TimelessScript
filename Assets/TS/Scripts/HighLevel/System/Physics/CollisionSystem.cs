using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystem))]
[BurstCompile]
public partial struct CollisionSystem : ISystem
{
    private CollisionSystemComponent collisionConfig;
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ColliderComponent>();
        
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
            .WithAll<ColliderComponent, ColliderBoundsComponent>()
            .Build();
        
        var colliderEntities = colliderQuery.ToEntityArray(Allocator.TempJob);
        var colliderBounds = colliderQuery.ToComponentDataArray<ColliderBoundsComponent>(Allocator.TempJob);
        var colliderComponents = colliderQuery.ToComponentDataArray<ColliderComponent>(Allocator.TempJob);
        
        // 3. 충돌 검사 Job
        var collisionDetectionJob = new CollisionDetectionJob
        {
            allEntities = colliderEntities,
            allBounds = colliderBounds,
            allColliders = colliderComponents,
            collisionBufferLookup = SystemAPI.GetBufferLookup<CollisionBuffer>(false),
            collisionInfoLookup = SystemAPI.GetComponentLookup<CollisionInfoComponent>(false),
            GroundLookup = SystemAPI.GetComponentLookup<GroundComponent>(true),
            useSpacialHashing = collisionConfig.useSpacialHashing,
            cellSize = collisionConfig.cellSize
        };
        collisionDetectionJob.Schedule(state.Dependency).Complete();
        
        // 4. 메모리 정리 (Job 완료 후 System에서 직접 해제)
        colliderEntities.Dispose();
        colliderBounds.Dispose();
        colliderComponents.Dispose();
    }
}