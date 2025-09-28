using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SpawnConfigComponent : IComponentData
{
    public Entity SpawnObjectPrefab;
    public FixedString64Bytes Name;
    public int MaxSpawnCount;
    public int CurrentSpawnCount;
    public float SpawnCooldown;
    public float NextSpawnTime;
    public float MinSpawnDistance;
}