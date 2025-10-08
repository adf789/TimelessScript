using Unity.Entities;

[InternalBufferCapacity(8)]
public struct AvailableLayerBuffer : IBufferElementData
{
    public int Layer;
}
