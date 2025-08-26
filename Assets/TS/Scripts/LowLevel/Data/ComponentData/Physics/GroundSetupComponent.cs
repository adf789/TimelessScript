
using Unity.Entities;
using Unity.Mathematics;

public struct GroundSetupComponent : IComponentData
{
    public int selectedPresetIndex;
    public bool autoSetupOnStart;
}