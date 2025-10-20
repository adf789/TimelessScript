
using Unity.Entities;
using Unity.Mathematics;

public struct SpriteRendererComponent : IComponentData
{
    public int Layer;
    public bool IsFlip;
    public bool IsEmphasis;
}
