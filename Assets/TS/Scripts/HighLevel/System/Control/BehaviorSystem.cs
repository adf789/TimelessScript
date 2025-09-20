
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ControlSystem))]
[BurstCompile]
public partial struct BehaviorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<TSObjectComponent>().WithAll<LightweightPhysicsComponent>().Build();

        state.RequireAnyForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var behaviorJob = new BehaviorJob()
        {
            AnimationComponentLookup = SystemAPI.GetComponentLookup<SpriteSheetAnimationComponent>(false),
            Speed = 2f,
            ClimbSpeed = 0.7f,
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = behaviorJob.ScheduleParallel(state.Dependency);
    }
}