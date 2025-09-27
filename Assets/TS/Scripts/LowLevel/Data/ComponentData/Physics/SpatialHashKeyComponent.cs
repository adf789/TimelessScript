using Unity.Entities;
using Unity.Mathematics;

public struct SpatialHashKeyComponent : IComponentData
{
    public int2 CellPosition;
}