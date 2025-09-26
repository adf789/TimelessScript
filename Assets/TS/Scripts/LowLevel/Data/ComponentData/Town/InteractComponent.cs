
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct InteractComponent : IComponentData
{
    public uint DataID;
    public TSObjectType DataType;
}
