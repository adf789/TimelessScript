// Assets/TS/Scripts/HighLevel/ComponentData/TargetHolderComponent.cs
using Unity.Entities;
using Unity.Mathematics;

public struct TargetHolderComponent : IComponentData
{
    public TSObjectInfoComponent Target;
    public float2 TouchPosition;
}
