using Unity.Entities;
using Unity.Mathematics;

public struct SpawnConfigComponent : IComponentData
{
    public Entity spawnObjectPrefab;
    public int maxSpawnCount;
    public int currentSpawnCount;
    public float spawnCooldown;
    public float nextSpawnTime;
    public float minSpawnDistance;
    public bool autoSpawn;
}