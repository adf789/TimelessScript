using Unity.Entities;
using Unity.Collections;

/// <summary>
/// Component to request subscene loading
/// Add this to an entity to trigger subscene load
/// </summary>
public struct SubSceneLoadRequest : IComponentData
{
    public Hash128 SceneGUID;       // Scene GUID for loading
    public byte Priority;           // Loading priority (0-255)
    public bool LoadAsync;          // Load asynchronously?
    public bool BlockOnStreamIn;    // Block until fully loaded?
}
