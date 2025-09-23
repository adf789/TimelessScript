
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct InteractComponent : IComponentData
{
    public int DataID;
    public TSObjectType DataType;
}
