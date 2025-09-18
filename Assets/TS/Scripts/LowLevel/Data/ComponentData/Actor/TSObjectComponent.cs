
using Unity.Entities;
using Unity.Mathematics;

public struct TSObjectComponent : IComponentData
{
    public Entity Self;
    public TSObjectType ObjectType;
    public TSObjectBehavior Behavior;
    public float RootOffset;

    public bool IsNull => Self == Entity.Null;
}