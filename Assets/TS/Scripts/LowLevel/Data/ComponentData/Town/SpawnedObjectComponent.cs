using Unity.Entities;

public struct SpawnedObjectComponent : IComponentData
{
    public Entity spawner;
    public float spawnTime;
    public bool isManaged;
}