// Assets/TS/Scripts/HighLevel/ComponentData/TargetHolderComponent.cs
using Unity.Entities;
using Unity.Mathematics;

public struct TargetHolderComponent : IComponentData
{
    public TSObjectComponent Target;
    public float2 TouchPosition;

    public void Release()
    {
        Target = default;
        TouchPosition = float2.zero;
    }
}
