using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

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

[BurstCompile]
public partial struct GroundSetupJob : IJobEntity
{
    public void Execute(
        Entity entity,
        ref GroundSetupComponent setup,
        ref GroundComponent ground,
        DynamicBuffer<GroundPresetBuffer> presets)
    {
        if (!setup.autoSetupOnStart)
            return;
            
        if (setup.selectedPresetIndex < 0 || setup.selectedPresetIndex >= presets.Length)
            return;
        
        var selectedPreset = presets[setup.selectedPresetIndex].preset;
        
        // Ground Component 설정
        ground.groundType = selectedPreset.groundType;
        ground.bounciness = selectedPreset.bounciness;
        ground.friction = selectedPreset.friction;
        ground.isOneWayPlatform = selectedPreset.isOneWayPlatform;
        
        // 자동 설정 완료 플래그
        setup.autoSetupOnStart = false;
    }
}