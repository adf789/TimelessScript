using Unity.Entities;

/// <summary>
/// Tag component indicating subscene is fully loaded
/// </summary>
public struct SubSceneLoadedComponent : IComponentData
{
    public Entity SceneEntity;      // Reference to loaded scene entity
    public int LoadedEntityCount;   // Number of entities loaded
}
