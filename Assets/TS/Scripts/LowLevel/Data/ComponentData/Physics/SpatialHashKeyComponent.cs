using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SpatialHashKeyComponent : IComponentData
{
    public int2 MinCell;
    public int2 MaxCell;
}