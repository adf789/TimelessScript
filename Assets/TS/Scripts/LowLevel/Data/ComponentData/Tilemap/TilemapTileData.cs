using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Individual tile data stored in DynamicBuffer
/// Optimized for memory and performance
/// </summary>
[InternalBufferCapacity(1024)]
public struct TilemapTileData : IBufferElementData
{
    public int2 GridPosition;       // Position in grid (x, y)
    public int SpriteIndex;         // Index in sprite atlas
    public float4 Color;            // Tint color (r, g, b, a)
    public byte Flags;              // Bit flags for tile properties (flipped, rotated, etc.)

    // Flag constants
    public const byte FLAG_NONE = 0;
    public const byte FLAG_FLIP_X = 1 << 0;
    public const byte FLAG_FLIP_Y = 1 << 1;
    public const byte FLAG_ROTATE_90 = 1 << 2;
    public const byte FLAG_ROTATE_180 = 1 << 3;
    public const byte FLAG_VISIBLE = 1 << 4;
    public const byte FLAG_COLLIDABLE = 1 << 5;
}
