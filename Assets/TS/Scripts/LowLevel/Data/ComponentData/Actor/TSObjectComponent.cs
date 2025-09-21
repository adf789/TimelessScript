
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct TSObjectComponent : IComponentData
{
    public FixedString64Bytes Name;
    public Entity Self;
    public TSObjectType ObjectType;
    public TSObjectBehavior Behavior;
    public float RootOffset;
    public float Radius;

    public bool IsNull => Self == Entity.Null;
}