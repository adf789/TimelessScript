using Unity.Collections;
using Unity.Entities;

public struct SpawnConfigComponent : IComponentData
{
    public Entity SpawnObjectPrefab;
    public Entity SpawnParent;
    public TSObjectType ObjectType;
    public FixedString64Bytes Name;
    public int LayerOffset;
    public int MaxSpawnCount;
    public int ReadySpawnCount;
    public int CurrentSpawnCount;
    public float LifeTime;
    public float SpawnCooldown;
    public float NextSpawnTime;
    public float PositionYOffset;
}