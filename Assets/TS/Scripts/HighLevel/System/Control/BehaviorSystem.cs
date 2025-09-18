
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ControlSystem))]
[BurstCompile]
public partial struct BehaviorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<BehaviorComponent>().WithAll<LightweightPhysicsComponent>().Build();

        state.RequireAnyForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var behaviorJob = new BehaviorJob()
        {
            animationComponentLookup = SystemAPI.GetComponentLookup<SpriteSheetAnimationComponent>(false)
        };

        state.Dependency = behaviorJob.ScheduleParallel(state.Dependency);
    }
}