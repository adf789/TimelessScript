using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Burst-compiled job for frustum culling of tilemap chunks
/// Marks chunks as visible/invisible based on camera frustum
/// </summary>
[BurstCompile]
public struct TilemapCullingJob : IJobChunk
{
    [ReadOnly] public float3 CameraPosition;
    [ReadOnly] public float CameraOrthographicSize;
    [ReadOnly] public float CameraAspect;

    public ComponentTypeHandle<TilemapChunkComponent> ChunkHandle;

    [BurstCompile]
    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var chunks = chunk.GetNativeArray(ref ChunkHandle);

        // Calculate camera frustum bounds
        float camHeight = CameraOrthographicSize * 2f;
        float camWidth = camHeight * CameraAspect;

        float minX = CameraPosition.x - camWidth * 0.5f;
        float maxX = CameraPosition.x + camWidth * 0.5f;
        float minY = CameraPosition.y - camHeight * 0.5f;
        float maxY = CameraPosition.y + camHeight * 0.5f;

        var frustum = new AABB
        {
            Center = CameraPosition,
            Extents = new float3(camWidth * 0.5f, camHeight * 0.5f, 1000f)
        };

        for (int i = 0; i < chunk.Count; i++)
        {
            var chunkComp = chunks[i];

            // Check if chunk bounds intersect with camera frustum
            bool isVisible = AABBIntersectsAABB(chunkComp.ChunkBounds, frustum);

            chunkComp.IsVisible = isVisible;
            chunks[i] = chunkComp;
        }
    }

    [BurstCompile]
    private static bool AABBIntersectsAABB(AABB a, AABB b)
    {
        float3 aMin = a.Center - a.Extents;
        float3 aMax = a.Center + a.Extents;
        float3 bMin = b.Center - b.Extents;
        float3 bMax = b.Center + b.Extents;

        return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
               (aMin.y <= bMax.y && aMax.y >= bMin.y);
    }
}
