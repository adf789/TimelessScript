
using Unity.Entities;

public struct TSObjectComponent : IComponentData
{
    public Entity Self;
    public TSObjectType ObjectType;
    public uint DataID;
    public float RootOffset;
    public Entity RendererEntity;

    public bool IsNull => Self == Entity.Null;
}