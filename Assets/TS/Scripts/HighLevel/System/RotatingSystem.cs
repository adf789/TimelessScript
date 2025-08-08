using Unity.Burst;
using Unity.Entities;

public partial struct RotatingSystem : ISystem
{
    //public void OnCreate(ref SystemState state)
    //    => state.RequireForUpdate<>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RotateUpdateJob rotateJob = new RotateUpdateJob()
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = rotateJob.ScheduleParallel(state.Dependency);
    }
}
