using Unity.Entities;
using Unity.Mathematics;

public struct NavigationWaypoint : IBufferElementData
{
    public float2 Position;
    public Entity TargetEntity;
    public TSObjectType ObjectType;
    public MoveState MoveType;
}