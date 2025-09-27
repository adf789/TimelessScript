using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SpawnRequestComponent : IComponentData
{
    public Entity SpawnObject;
    public FixedString64Bytes Name;
    public float2 SpawnPosition;
    public bool IsActive;
}