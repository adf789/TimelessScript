using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct GroundSetupSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GroundSetupComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var setupJob = new GroundSetupJob();
        state.Dependency = setupJob.ScheduleParallel(state.Dependency);
    }
}