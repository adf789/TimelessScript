
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NavigationSystem))]
// [BurstCompile]
public partial struct BehaviorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder()
        .WithAll<TSObjectComponent, TSActorComponent, PhysicsComponent, NavigationComponent>()
        .Build();

        state.RequireAnyForUpdate(query);
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var behaviorJob = new BehaviorJob()
        {
            AnimationComponentLookup = SystemAPI.GetComponentLookup<SpriteSheetAnimationComponent>(false),
            ObjectTargetComponentLookup = SystemAPI.GetComponentLookup<ObjectTargetComponent>(false),
            Ecb = ecb.AsParallelWriter(),
            Speed = 3f,
            ClimbSpeed = 0.7f,
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        var behaviorJobHandle = behaviorJob.ScheduleParallel(state.Dependency);

        behaviorJobHandle.Complete();
    }
}
