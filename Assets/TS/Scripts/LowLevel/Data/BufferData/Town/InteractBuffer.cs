
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct InteractBuffer : IBufferElementData
{
    public uint DataID;
    public TableDataType DataType;
}
