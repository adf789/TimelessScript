using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public struct CollisionSystemComponent : IComponentData
{
    public bool useSpacialHashing;
    public float cellSize;
}

public struct SpatialHashCell : IBufferElementData
{
    public Entity entity;
}

public struct SpatialHashKey : IComponentData
{
    public int2 cellPosition;
}