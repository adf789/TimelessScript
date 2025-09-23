
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(NavigationSystem))]
[BurstCompile]
public partial struct BehaviorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<TSObjectComponent>().WithAll<PhysicsComponent>().Build();

        state.RequireAnyForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var behaviorJob = new BehaviorJob()
        {
            AnimationComponentLookup = SystemAPI.GetComponentLookup<SpriteSheetAnimationComponent>(false),
            Speed = 3f,
            ClimbSpeed = 0.7f,
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        var behaviorJobHandle = behaviorJob.ScheduleParallel(state.Dependency);

        behaviorJobHandle.Complete();
    }
}