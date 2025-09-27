using Unity.Entities;

public struct SpawnedObjectComponent : IComponentData
{
    public Entity Spawner;
    public float SpawnTime;
    public bool IsManaged;
}