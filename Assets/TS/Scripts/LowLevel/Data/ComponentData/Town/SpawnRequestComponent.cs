using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SpawnRequestComponent : IComponentData
{
    public Entity SpawnObject;
    public Entity Spawner; // 스포너 Entity 참조 추가
    public TSObjectType ObjectType;
    public FixedString64Bytes Name;
    public float3 SpawnPosition;
    public int LayerOffset;
    public bool IsActive;
}