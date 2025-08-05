using UnityEngine;
using Unity.Entities;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject prefab = null;
    public float spawnRadius = 1;
    public int spawnCount = 10;
    public uint randomSeed = 100;

    private class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var data = new Config()
            {
                prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                spawnRadius = authoring.spawnRadius,
                spawnCount = authoring.spawnCount,
                randomSeed = authoring.randomSeed
            };

            AddComponent(GetEntity(TransformUsageFlags.None), data);
        }

    }
}
