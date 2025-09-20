using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct PhysicsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // 물리 업데이트 Job
        var physicsJob = new PhysicsUpdateJob
        {
            deltaTime = deltaTime
        };
        state.Dependency = physicsJob.ScheduleParallel(state.Dependency);
        
        // 충돌 처리 Job
        var collisionJob = new PhysicsCollisionJob
        {
            TSObjectLookup = SystemAPI.GetComponentLookup<TSObjectComponent>(true),
            ColliderLookup = SystemAPI.GetComponentLookup<ColliderComponent>(true),
        };
        state.Dependency = collisionJob.ScheduleParallel(state.Dependency);
        
        // 트리거 처리 Job
        var triggerJob = new TriggerHandlingJob();
        state.Dependency = triggerJob.ScheduleParallel(state.Dependency);
    }
}