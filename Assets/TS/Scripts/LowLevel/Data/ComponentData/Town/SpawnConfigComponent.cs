using Unity.Entities;
using Unity.Mathematics;

public struct SpawnConfigComponent : IComponentData
{
    public Entity SpawnObjectPrefab;
    public int MaxSpawnCount;
    public int CurrentSpawnCount;
    public float SpawnCooldown;
    public float NextSpawnTime;
    public float MinSpawnDistance;
    public bool AutoSpawn;
}