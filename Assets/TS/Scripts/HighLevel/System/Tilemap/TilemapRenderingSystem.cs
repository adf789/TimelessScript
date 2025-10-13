using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// High-performance tilemap rendering system using ECS and Burst
/// Handles frustum culling and GPU instanced batch rendering
/// </summary>
[DisableAutoCreation]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct TilemapRenderingSystem : ISystem
{
    private EntityQuery tilemapQuery;
    private EntityQuery chunkQuery;

    // Rendering resources (shared across all tilemaps)
    private static Mesh quadMesh;
    private static MaterialPropertyBlock propertyBlock;

    // Batch rendering constants
    private const int MAX_INSTANCES_PER_BATCH = 1023; // GPU instancing limit

    public void OnCreate(ref SystemState state)
    {
        tilemapQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<TilemapComponent>(),
            ComponentType.ReadOnly<TilemapTileData>()
        );

        chunkQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<TilemapChunkComponent>()
        );

        state.RequireForUpdate<TilemapComponent>();

        // Create shared resources
        if (quadMesh == null)
        {
            quadMesh = CreateQuadMesh();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        // Get camera data from ECS component
        if (!SystemAPI.TryGetSingleton<MainCameraComponent>(out var cameraComp))
            return;

        // Get camera data
        float3 cameraPos = cameraComp.Position;
        float cameraSize = cameraComp.OrthographicSize;
        float cameraAspect = cameraComp.Aspect;

        // Step 1: Frustum culling job (if chunks are used)
        if (!chunkQuery.IsEmpty)
        {
            var cullingJob = new TilemapCullingJob
            {
                CameraPosition = cameraPos,
                CameraOrthographicSize = cameraSize,
                CameraAspect = cameraAspect,
                ChunkHandle = state.GetComponentTypeHandle<TilemapChunkComponent>(false)
            };
            state.Dependency = cullingJob.ScheduleParallel(chunkQuery, state.Dependency);
            state.Dependency.Complete();
        }

        // Step 2: Render visible tilemaps using GPU instancing
        RenderTilemaps(ref state, cameraComp);
    }

    private void RenderTilemaps(ref SystemState state, MainCameraComponent camera)
    {
        foreach (var (tilemap, tileBuffer, entity) in SystemAPI.Query<
            RefRO<TilemapComponent>,
            DynamicBuffer<TilemapTileData>>()
            .WithEntityAccess())
        {
            var tilemapComp = tilemap.ValueRO;

            // Collect visible tiles
            using var visibleTiles = new NativeList<TilemapTileData>(tileBuffer.Length, Allocator.Temp);

            foreach (var tile in tileBuffer)
            {
                // Skip invisible tiles
                if ((tile.Flags & TilemapTileData.FLAG_VISIBLE) == 0)
                    continue;

                // Simple frustum culling (can be optimized with chunk culling)
                float2 worldPos = tilemapComp.Origin + (float2)tile.GridPosition * tilemapComp.TileSize;
                if (IsTileVisible(worldPos, tilemapComp.TileSize, camera))
                {
                    visibleTiles.Add(tile);
                }
            }

            // Render in batches using GPU instancing
            RenderTileBatch(visibleTiles, tilemapComp);
        }
    }

    private void RenderTileBatch(NativeList<TilemapTileData> tiles, TilemapComponent tilemap)
    {
        int tileCount = tiles.Length;
        if (tileCount == 0) return;

        // TODO: Get material from tilemap component or manager
        // For now, this is a placeholder - you need to provide a material
        Material tileMaterial = GetTileMaterial();
        if (tileMaterial == null) return;

        // Process tiles in batches (max 1023 per batch for GPU instancing)
        int batchCount = (tileCount + MAX_INSTANCES_PER_BATCH - 1) / MAX_INSTANCES_PER_BATCH;

        for (int batchIdx = 0; batchIdx < batchCount; batchIdx++)
        {
            int startIdx = batchIdx * MAX_INSTANCES_PER_BATCH;
            int endIdx = math.min(startIdx + MAX_INSTANCES_PER_BATCH, tileCount);
            int instanceCount = endIdx - startIdx;

            // Build transformation matrices for this batch
            var matrices = new Matrix4x4[instanceCount];
            var colors = new Vector4[instanceCount];

            for (int i = 0; i < instanceCount; i++)
            {
                var tile = tiles[startIdx + i];

                // Calculate world position
                float2 worldPos = tilemap.Origin + (float2)tile.GridPosition * tilemap.TileSize;

                // Build transformation matrix
                Vector3 position = new Vector3(worldPos.x, worldPos.y, 0);
                Quaternion rotation = GetTileRotation(tile.Flags);
                Vector3 scale = new Vector3(tilemap.TileSize.x, tilemap.TileSize.y, 1);

                matrices[i] = Matrix4x4.TRS(position, rotation, scale);
                colors[i] = new Vector4(tile.Color.x, tile.Color.y, tile.Color.z, tile.Color.w);
            }

            // Set material properties for batch
            propertyBlock.SetVectorArray("_Color", colors);

            // GPU instanced rendering
            Graphics.DrawMeshInstanced(
                mesh: quadMesh,
                submeshIndex: 0,
                material: tileMaterial,
                matrices: matrices,
                count: instanceCount,
                properties: propertyBlock,
                castShadows: UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows: false,
                layer: 0
            );
        }
    }

    private bool IsTileVisible(float2 tilePos, float2 tileSize, MainCameraComponent camera)
    {
        // Simple AABB frustum culling
        float camHeight = camera.OrthographicSize * 2f;
        float camWidth = camHeight * camera.Aspect;

        float minX = camera.Position.x - camWidth * 0.5f;
        float maxX = camera.Position.x + camWidth * 0.5f;
        float minY = camera.Position.y - camHeight * 0.5f;
        float maxY = camera.Position.y + camHeight * 0.5f;

        float tileMinX = tilePos.x - tileSize.x * 0.5f;
        float tileMaxX = tilePos.x + tileSize.x * 0.5f;
        float tileMinY = tilePos.y - tileSize.y * 0.5f;
        float tileMaxY = tilePos.y + tileSize.y * 0.5f;

        return !(tileMaxX < minX || tileMinX > maxX || tileMaxY < minY || tileMinY > maxY);
    }

    private Quaternion GetTileRotation(byte flags)
    {
        // Handle tile rotation based on flags
        if ((flags & TilemapTileData.FLAG_ROTATE_180) != 0)
            return Quaternion.Euler(0, 0, 180);
        if ((flags & TilemapTileData.FLAG_ROTATE_90) != 0)
            return Quaternion.Euler(0, 0, 90);

        return Quaternion.identity;
    }

    private Material GetTileMaterial()
    {
        // TODO: Implement material management
        // Options:
        // 1. Store in singleton component
        // 2. Get from TilemapAuthoring
        // 3. Use Resources.Load
        // 4. Use Addressables

        // For now, return null - you need to implement this
        // Example: return Resources.Load<Material>("Materials/TilemapMaterial");
        return null;
    }

    private static Mesh CreateQuadMesh()
    {
        var mesh = new Mesh
        {
            name = "TilemapQuad"
        };

        // Vertices for a centered quad (-0.5 to 0.5)
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };

        // UVs
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        // Triangles
        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public void OnDestroy(ref SystemState state)
    {
        // Cleanup is handled by Unity for static resources
    }
}
