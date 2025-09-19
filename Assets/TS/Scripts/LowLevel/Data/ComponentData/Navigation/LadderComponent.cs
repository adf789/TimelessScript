using Unity.Entities;
using Unity.Mathematics;

public struct LadderComponent : IComponentData
{
    public float Height;
    public bool IsUsable;
    public float2 TopPosition;
    public float2 BottomPosition;
}