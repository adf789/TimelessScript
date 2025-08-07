using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;

public class ResourcesManager : BaseManager<ResourcesManager>
{
    [SerializeField]
    private TypeValuePair<ResourceType, ResourcesPath>[] resources = null;

    private void Awake()
    {
        BindingPaths();
    }

    private void BindingPaths()
    {
        if (resources == null)
            return;

        ResourcesSubManager.Instance.SetPaths(from resource in resources select (resource.type, resource.value));
    }

    public ResourcesPath GetResourcesPath(ResourceType resourceType)
    {
        return System.Array.Find(resources, r => r.type == resourceType).value;
    }

    public async UniTask<T> LoadAsset<T>(ResourceType resourceType, string guid) where T : Object
    {
        var resourcePath = GetResourcesPath(resourceType);

        if (resourcePath == null)
            return null;

        return await resourcePath.LoadAsset<T>(guid);
    }

    public async UniTask<T> LoadAssetByName<T>(ResourceType resourceType, string name) where T : Object
    {
        var resourcePath = GetResourcesPath(resourceType);

        if (resourcePath == null)
            return null;

        return await resourcePath.LoadAssetByName<T>(name);
    }
}
