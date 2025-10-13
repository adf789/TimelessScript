using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

/// <summary>
/// ECS system for processing subscene load requests
/// Works with SubSceneLoadingManager for entity-based scene loading
/// </summary>
[DisableAutoCreation]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SubSceneLoadingSystem : ISystem
{
    private EntityQuery loadRequestQuery;

    public void OnCreate(ref SystemState state)
    {
        loadRequestQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<SubSceneLoadRequest>(),
            ComponentType.Exclude<SubSceneLoadedComponent>()
        );
    }

    public void OnUpdate(ref SystemState state)
    {
        // Process all load requests
        var requests = loadRequestQuery.ToComponentDataArray<SubSceneLoadRequest>(Allocator.Temp);
        var requestEntities = loadRequestQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < requests.Length; i++)
        {
            var request = requests[i];
            var requestEntity = requestEntities[i];

            // Load scene using SceneSystem static API
            var loadParams = new SceneSystem.LoadParameters
            {
                Flags = request.BlockOnStreamIn ? SceneLoadFlags.BlockOnStreamIn : SceneLoadFlags.DisableAutoLoad
            };

            var sceneEntity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, request.SceneGUID, loadParams);

            // Check if scene is loaded
            if (SceneSystem.IsSceneLoaded(state.WorldUnmanaged, sceneEntity))
            {
                // Count entities in loaded scene
                int entityCount = CountSceneEntities(ref state, sceneEntity);

                // Mark request as completed
                state.EntityManager.AddComponentData(requestEntity, new SubSceneLoadedComponent
                {
                    SceneEntity = sceneEntity,
                    LoadedEntityCount = entityCount
                });
            }
        }

        requests.Dispose();
        requestEntities.Dispose();
    }

    private int CountSceneEntities(ref SystemState state, Entity sceneEntity)
    {
        // SceneTag is ISharedComponentData, use SetSharedComponentFilter
        var query = state.GetEntityQuery(
            ComponentType.ReadOnly<SceneTag>()
        );

        // Filter by scene entity using shared component
        query.SetSharedComponentFilter(new SceneTag { SceneEntity = sceneEntity });

        int count = query.CalculateEntityCount();

        query.ResetFilter();
        return count;
    }
}
