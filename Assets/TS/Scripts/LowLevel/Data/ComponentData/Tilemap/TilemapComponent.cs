using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Main tilemap component containing grid configuration
/// </summary>
public struct TilemapComponent : IComponentData
{
    public int2 GridSize;           // Grid dimensions (width x height)
    public float2 TileSize;         // Size of each tile in world units
    public float2 Origin;           // Tilemap origin position
    public int ChunkSize;           // Tiles per chunk (for spatial partitioning)
    public Entity SpriteAtlasEntity; // Reference to sprite atlas
}
