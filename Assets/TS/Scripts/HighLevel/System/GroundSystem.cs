using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct GroundSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Ground 충돌 응답 처리
        var groundCollisionJob = new GroundCollisionJob();
        state.Dependency = groundCollisionJob.ScheduleParallel(state.Dependency);
    }
}