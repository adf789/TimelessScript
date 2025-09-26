
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct TSObjectComponent : IComponentData
{
    public FixedString64Bytes Name;
    public Entity Self;
    public TSObjectType ObjectType;
    public uint DataID;
    public float RootOffset;
    public Entity AnimationEntity;

    public bool IsNull => Self == Entity.Null;
}