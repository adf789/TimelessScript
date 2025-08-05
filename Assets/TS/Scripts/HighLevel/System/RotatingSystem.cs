using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial struct RotatingSystem : ISystem
{
    //public void OnCreate(ref SystemState state)
    //    => state.RequireForUpdate<RotateSpeed>();

    
    public void OnUpdate(ref SystemState state)
    {
        var rotateJob = new RotateUpdateJob()
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = rotateJob.ScheduleParallel(state.Dependency);
    }
}
