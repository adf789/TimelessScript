using Unity.Entities;

[InternalBufferCapacity(16)]
public struct SpawnedEntityBuffer : IBufferElementData
{
    public Entity SpawnedEntity;
    public float SpawnTime;
}