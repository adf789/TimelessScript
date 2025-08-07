using UnityEngine;

public class ResourceManager : BaseManager<ResourceManager>
{
    [SerializeField]
    private TypeValuePair<ResourceType, ResourcePath>[] resources = null;

    public ResourcePath GetResourcePath(ResourceType resourceType)
    {
        return System.Array.Find(resources, r => r.type == resourceType).value;
    }

    private void Start()
    {
        
    }
}
