using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct NavigationNodeComponent : IComponentData
{
    public float2 Position;
    public TSObjectType NodeType;
    public bool IsWalkable;
    public float MinHeight;
    public float MaxHeight;
}

public struct NavigationConnection : IBufferElementData
{
    public Entity ConnectedNode;
    public MoveState ConnectionType;
    public float Distance;
}