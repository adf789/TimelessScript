using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesSubManager : SubBaseManager<ResourcesSubManager>
{
    private Dictionary<ResourceType, ResourcesPath> paths = null;

    public void SetPaths(IEnumerable<(ResourceType type, ResourcesPath path)> pathList)
    {
        if (pathList == null)
            return;

        paths = new Dictionary<ResourceType, ResourcesPath>();

        foreach (var path in pathList)
        {
            paths[path.type] = path.path;
        }
    }

    public ResourcesPath GetResourcesPath(ResourceType type)
    {
        if (paths == null)
            return null;

        if(paths.TryGetValue(type, out ResourcesPath path))
            return path;

        return null;
    }

    public async UniTask<T> LoadAsset<T>(ResourceType resourceType, string guid) where T : UnityEngine.Object
    {
        var resourcePath = GetResourcesPath(resourceType);

        if (resourcePath == null)
            return null;

        return await resourcePath.LoadAsset<T>(guid);
    }

    public async UniTask<T> LoadAssetByName<T>(ResourceType resourceType, string name) where T : UnityEngine.Object
    {
        var resourcePath = GetResourcesPath(resourceType);

        if (resourcePath == null)
            return null;

        return await resourcePath.LoadAssetByName<T>(name);
    }
}
