using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Spatial partitioning chunk for efficient culling
/// Each chunk represents a square region of tiles
/// </summary>
public struct TilemapChunkComponent : IComponentData
{
    public int2 ChunkCoord;         // Chunk coordinates in chunk grid
    public AABB ChunkBounds;        // World space bounds for frustum culling
    public int TileStartIndex;      // Start index in tile buffer
    public int TileCount;           // Number of tiles in this chunk
    public bool IsVisible;          // Visibility flag (set by culling system)
}
