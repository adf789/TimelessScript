using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using Unity.Scenes;

/// <summary>
/// Manager for loading multiple subscenes at runtime
/// Integrates with ECS for entity-based scene management
/// </summary>
public class SubSceneLoadingManager : BaseManager<SubSceneLoadingManager>
{
    [Header("Scene Loading")]
    [SerializeField] private bool loadAsync = true;
    [SerializeField] private int maxConcurrentLoads = 3;

    private Dictionary<UnityEngine.Hash128, Entity> loadedScenes = new Dictionary<UnityEngine.Hash128, Entity>();
    private Queue<SubSceneLoadRequest> loadQueue = new Queue<SubSceneLoadRequest>();
    private int currentLoadingCount = 0;

    private EntityManager entityManager;
    private World world;

    private void Start()
    {
        world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
    }

    /// <summary>
    /// Load a subscene by GUID
    /// </summary>
    public async UniTask<Entity> LoadSubSceneAsync(UnityEngine.Hash128 sceneGUID, byte priority = 128, bool blockOnStreamIn = false)
    {
        // Check if already loaded
        if (loadedScenes.TryGetValue(sceneGUID, out Entity existingEntity))
        {
            Debug.Log($"SubScene {sceneGUID} already loaded");
            return existingEntity;
        }

        // Create load request entity
        var requestEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(requestEntity, new SubSceneLoadRequest
        {
            SceneGUID = sceneGUID,
            Priority = priority,
            LoadAsync = loadAsync,
            BlockOnStreamIn = blockOnStreamIn
        });

        // Wait for scene to load
        while (!entityManager.HasComponent<SubSceneLoadedComponent>(requestEntity))
        {
            await UniTask.Yield();
        }

        var loadedComp = entityManager.GetComponentData<SubSceneLoadedComponent>(requestEntity);
        loadedScenes[sceneGUID] = loadedComp.SceneEntity;

        // Cleanup request entity
        entityManager.DestroyEntity(requestEntity);

        Debug.Log($"SubScene {sceneGUID} loaded with {loadedComp.LoadedEntityCount} entities");
        return loadedComp.SceneEntity;
    }

    /// <summary>
    /// Load multiple subscenes concurrently
    /// </summary>
    public async UniTask LoadMultipleSubScenesAsync(List<UnityEngine.Hash128> sceneGUIDs)
    {
        var tasks = new List<UniTask<Entity>>();

        foreach (var guid in sceneGUIDs)
        {
            tasks.Add(LoadSubSceneAsync(guid));
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// Unload a subscene
    /// </summary>
    public void UnloadSubScene(UnityEngine.Hash128 sceneGUID)
    {
        if (!loadedScenes.TryGetValue(sceneGUID, out Entity sceneEntity))
        {
            Debug.LogWarning($"SubScene {sceneGUID} not loaded");
            return;
        }

        // Unload by destroying the scene entity
        // Unity.Scenes will automatically clean up all entities in the scene
        if (entityManager.Exists(sceneEntity))
        {
            entityManager.DestroyEntity(sceneEntity);
        }

        loadedScenes.Remove(sceneGUID);
        Debug.Log($"SubScene {sceneGUID} unloaded");
    }

    /// <summary>
    /// Unload all subscenes
    /// </summary>
    public void UnloadAllSubScenes()
    {
        var scenesToUnload = new List<UnityEngine.Hash128>(loadedScenes.Keys);

        foreach (var guid in scenesToUnload)
        {
            UnloadSubScene(guid);
        }
    }

    /// <summary>
    /// Check if a subscene is loaded
    /// </summary>
    public bool IsSubSceneLoaded(UnityEngine.Hash128 sceneGUID)
    {
        return loadedScenes.ContainsKey(sceneGUID);
    }

    /// <summary>
    /// Get loaded subscene entity
    /// </summary>
    public Entity GetLoadedSubScene(UnityEngine.Hash128 sceneGUID)
    {
        return loadedScenes.TryGetValue(sceneGUID, out Entity entity) ? entity : Entity.Null;
    }
}
