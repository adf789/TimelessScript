
using Unity.Burst;
using Unity.Entities;

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
        
        var selectedPreset = presets[setup.selectedPresetIndex];
        
        // Ground Component 설정
        ground.groundType = selectedPreset.groundType;
        ground.bounciness = selectedPreset.bounciness;
        ground.friction = selectedPreset.friction;
        ground.isOneWayPlatform = selectedPreset.isOneWayPlatform;
        
        // 자동 설정 완료 플래그
        setup.autoSetupOnStart = false;
    }
}