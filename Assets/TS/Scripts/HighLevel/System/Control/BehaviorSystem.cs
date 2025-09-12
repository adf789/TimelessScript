
using Unity.Burst;
using Unity.Entities;

public partial struct BehaviorSystem : ISystem
{
    //public void OnCreate(ref SystemState state)
    //    => state.RequireForUpdate<>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //var job = new BehaviorJob();

        //state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}