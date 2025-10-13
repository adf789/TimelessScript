using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

/// <summary>
/// Authoring component that bakes Unity Tilemap to ECS
/// Optimizes tilemap data for high-performance rendering
/// </summary>
public class TilemapAuthoring : MonoBehaviour
{
    [Header("Tilemap Reference")]
    [SerializeField] private Tilemap tilemap;

    [Header("Chunk Settings")]
    [SerializeField] private int chunkSize = 16; // 16x16 tiles per chunk

    [Header("Rendering")]
    [SerializeField] private Material tilemapMaterial;
    [SerializeField] private Texture2D spriteAtlas;

    private class Baker : Baker<TilemapAuthoring>
    {
        public override void Bake(TilemapAuthoring authoring)
        {
            if (authoring.tilemap == null)
            {
                Debug.LogWarning($"TilemapAuthoring on {authoring.gameObject.name}: Tilemap is null!");
                return;
            }

            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Get tilemap bounds
            BoundsInt bounds = authoring.tilemap.cellBounds;
            var tileArray = authoring.tilemap.GetTilesBlock(bounds);

            // Create main tilemap component
            var tilemapComponent = new TilemapComponent
            {
                GridSize = new int2(bounds.size.x, bounds.size.y),
                TileSize = new float2(authoring.tilemap.cellSize.x, authoring.tilemap.cellSize.y),
                Origin = new float2(bounds.min.x, bounds.min.y),
                ChunkSize = authoring.chunkSize
            };
            AddComponent(entity, tilemapComponent);

            // Create tile data buffer
            var tileBuffer = AddBuffer<TilemapTileData>(entity);

            // Bake tiles from Unity Tilemap
            for (int y = 0; y < bounds.size.y; y++)
            {
                for (int x = 0; x < bounds.size.x; x++)
                {
                    int index = x + y * bounds.size.x;
                    var tile = tileArray[index];

                    if (tile == null) continue;

                    var gridPos = new int2(bounds.min.x + x, bounds.min.y + y);
                    var worldPos = authoring.tilemap.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));

                    // Get tile sprite (if available)
                    int spriteIndex = 0;
                    Color tileColor = Color.white;

                    if (tile is Tile unityTile)
                    {
                        spriteIndex = GetSpriteIndex(unityTile.sprite, authoring.spriteAtlas);
                        tileColor = unityTile.color;
                    }

                    var tileData = new TilemapTileData
                    {
                        GridPosition = gridPos,
                        SpriteIndex = spriteIndex,
                        Color = new float4(tileColor.r, tileColor.g, tileColor.b, tileColor.a),
                        Flags = TilemapTileData.FLAG_VISIBLE
                    };

                    tileBuffer.Add(tileData);
                }
            }

            Debug.Log($"Baked tilemap: {tileBuffer.Length} tiles, Grid: {tilemapComponent.GridSize}, ChunkSize: {authoring.chunkSize}");
        }

        private int GetSpriteIndex(Sprite sprite, Texture2D atlas)
        {
            if (sprite == null || atlas == null) return 0;

            // TODO: Implement sprite atlas lookup
            // For now, return 0 (first sprite)
            return 0;
        }
    }
}
