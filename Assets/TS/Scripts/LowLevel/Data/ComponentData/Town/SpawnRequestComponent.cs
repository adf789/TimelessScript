using Unity.Entities;
using Unity.Mathematics;

public struct SpawnRequestComponent : IComponentData
{
    public Entity spawnObject;
    public float2 spawnPosition;
    public bool isActive;
}