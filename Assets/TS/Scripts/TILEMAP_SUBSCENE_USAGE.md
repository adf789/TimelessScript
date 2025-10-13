# ECS Tilemap & SubScene Loading System

Performance-optimized runtime subscene loading with tilemap support for Unity ECS.

## 🏗️ Architecture Overview

### Component Layers
```
LowLevel (Data/ComponentData):
├── Tilemap/
│   ├── TilemapComponent.cs          - Grid configuration
│   ├── TilemapTileData.cs           - Tile data buffer
│   └── TilemapChunkComponent.cs     - Spatial partitioning
└── Scene/
    ├── SubSceneLoadRequest.cs       - Load request component
    └── SubSceneLoadedComponent.cs   - Load completion tag

MiddleLevel (Authoring):
├── Tilemap/
│   └── TilemapAuthoring.cs          - Unity Tilemap → ECS baker
└── Job/
    └── Tilemap/
        └── TilemapCullingJob.cs     - Burst-compiled frustum culling

HighLevel (Systems & Managers):
├── System/
│   ├── Tilemap/
│   │   └── TilemapRenderingSystem.cs - Main rendering system
│   └── Scene/
│       └── SubSceneLoadingSystem.cs  - Scene loading processor
└── Manager/
    └── SubSceneLoadingManager.cs     - Runtime scene manager
```

## 🚀 Quick Start

### 1. Setup Tilemap in SubScene

**Create SubScene:**
```
1. Create new scene: File → New Scene
2. Add Tilemap: GameObject → 2D Object → Tilemap → Rectangular
3. Paint tiles using Tile Palette
4. Add TilemapAuthoring component to Tilemap GameObject
5. Configure settings:
   - Tilemap: Reference to Tilemap component
   - Chunk Size: 16 (tiles per chunk for culling)
   - Tilemap Material: Your sprite material
   - Sprite Atlas: Your tile sprite atlas texture
```

**Convert to SubScene:**
```
1. Select scene root GameObject
2. Component → Entities → SubScene
3. Unity will create .unity subscene file
4. Note the Scene GUID in Inspector
```

### 2. Runtime Loading

**Method A: Via Manager (Recommended)**
```csharp
using Unity.Collections;

public class GameController : MonoBehaviour
{
    [SerializeField] private string sceneGUIDString; // Paste from Inspector

    private async void Start()
    {
        var sceneGUID = new Hash128(sceneGUIDString);

        // Load single subscene
        await SubSceneLoadingManager.Instance.LoadSubSceneAsync(sceneGUID);

        // Or load multiple scenes
        var sceneGUIDs = new List<Hash128>
        {
            new Hash128("guid1"),
            new Hash128("guid2")
        };
        await SubSceneLoadingManager.Instance.LoadMultipleSubScenesAsync(sceneGUIDs);
    }
}
```

**Method B: Via ECS Component**
```csharp
// In any ECS system
var requestEntity = EntityManager.CreateEntity();
EntityManager.AddComponentData(requestEntity, new SubSceneLoadRequest
{
    SceneGUID = new Hash128("your-scene-guid"),
    Priority = 128,
    LoadAsync = true,
    BlockOnStreamIn = false
});
```

### 3. Enable Systems

**Add to GameManager or Bootstrap:**
```csharp
public class GameManager : BaseManager<GameManager>
{
    private void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;

        // Enable subscene loading system
        var loadingSystem = world.GetOrCreateSystemManaged<SubSceneLoadingSystem>();
        world.GetOrCreateSystemGroup<InitializationSystemGroup>().AddSystemToUpdateList(loadingSystem);

        // Enable tilemap rendering system
        var renderSystem = world.GetOrCreateSystem<TilemapRenderingSystem>();
        world.GetOrCreateSystemGroup<PresentationSystemGroup>().AddSystemToUpdateList(renderSystem);
    }
}
```

## 🎨 Tilemap Features

### Tile Properties
```csharp
// TilemapTileData flags
FLAG_VISIBLE      - Tile is rendered
FLAG_FLIP_X       - Flip horizontally
FLAG_FLIP_Y       - Flip vertically
FLAG_ROTATE_90    - Rotate 90 degrees
FLAG_ROTATE_180   - Rotate 180 degrees
FLAG_COLLIDABLE   - Tile has collision (for future physics integration)
```

### Performance Optimizations

**Spatial Partitioning:**
- Tiles organized into chunks (default 16x16)
- Chunk-based frustum culling
- Only visible chunks are processed

**Burst Compilation:**
- TilemapCullingJob uses Burst for maximum performance
- Parallel job execution across chunks
- Zero managed allocations during runtime

**Memory Efficiency:**
- DynamicBuffer for tile data (cache-friendly)
- Component-based architecture (optimal memory layout)
- No GameObject overhead per tile

## 📊 Performance Characteristics

**Tilemap Rendering:**
- Chunk culling: O(chunks) with Burst
- Tile rendering: O(visible_tiles)
- Memory: ~24 bytes per tile + chunk overhead

**SubScene Loading:**
- Async loading: Non-blocking
- Priority system: 0-255 (higher = more priority)
- Concurrent loads: Configurable (default 3)

## 🔧 Advanced Usage

### Runtime Tile Modification

```csharp
// In any ISystem
foreach (var (tilemapComp, tileBuffer, entity) in
    SystemAPI.Query<RefRO<TilemapComponent>, DynamicBuffer<TilemapTileData>>()
    .WithEntityAccess())
{
    for (int i = 0; i < tileBuffer.Length; i++)
    {
        var tile = tileBuffer[i];

        // Modify tile
        tile.Color = new float4(1, 0, 0, 1); // Red tint
        tile.Flags |= TilemapTileData.FLAG_FLIP_X;

        tileBuffer[i] = tile;
    }
}
```

### Custom Rendering

The current `TilemapRenderingSystem.RenderTilemaps()` is a placeholder. Implement using:

**Option 1: Graphics.DrawMeshInstanced**
```csharp
// Batch render tiles with GPU instancing
Matrix4x4[] matrices = new Matrix4x4[visibleTiles.Length];
Graphics.DrawMeshInstanced(tileMesh, 0, material, matrices);
```

**Option 2: Custom Render Pipeline**
```csharp
// Integrate with URP/HDRP custom rendering
// Use CommandBuffer for efficient batch rendering
```

### Scene Management

**Unload SubScene:**
```csharp
SubSceneLoadingManager.Instance.UnloadSubScene(sceneGUID);
```

**Check Load Status:**
```csharp
bool isLoaded = SubSceneLoadingManager.Instance.IsSubSceneLoaded(sceneGUID);
Entity sceneEntity = SubSceneLoadingManager.Instance.GetLoadedSubScene(sceneGUID);
```

## 🐛 Troubleshooting

**Tilemap not rendering:**
- Check TilemapRenderingSystem is enabled and updating
- Verify TilemapAuthoring baked successfully (check console)
- Ensure camera is orthographic (required for 2D)

**SubScene not loading:**
- Verify Scene GUID is correct (copy from SubScene component Inspector)
- Check SubSceneLoadingSystem is enabled
- Ensure SceneSystem exists in world

**Performance issues:**
- Reduce chunk size for more granular culling
- Implement proper rendering in RenderTilemaps() (currently placeholder)
- Profile with Unity Profiler → Jobs section

## 📝 TODO: Production Enhancements

1. **Rendering Implementation:**
   - GPU instancing for batch rendering
   - Material property blocks for per-tile colors
   - Sprite atlas UV mapping

2. **Physics Integration:**
   - Tile collision generation from FLAG_COLLIDABLE
   - Integration with LightweightPhysics2D system

3. **Chunk System:**
   - Dynamic chunk entity creation
   - Chunk entity pooling
   - LOD system for distant chunks

4. **Asset Management:**
   - Sprite atlas integration
   - Material variant support
   - Addressables integration

## 🎯 Integration Points

**Existing Systems:**
- PhysicsSystem: Add tile collision support
- SpriteRendererComponent: Reuse rendering pipeline
- GameManager: Integrate scene loading workflow

**Assembly Structure:**
- LowLevel: Data-only components ✓
- MiddleLevel: Authoring + Jobs ✓
- HighLevel: Systems + Managers ✓
- EditorLevel: Future editor tools

---

**Created:** 2025
**Unity Version:** 6000.2.0b12
**ECS Version:** 1.3.14
