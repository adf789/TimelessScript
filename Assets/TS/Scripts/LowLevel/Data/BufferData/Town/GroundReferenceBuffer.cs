
using Unity.Entities;
using Unity.Mathematics;

public struct GroundReferenceBuffer : IBufferElementData
{
    public Entity Ground;
    public int2 Min;
    public int2 Max;
}
