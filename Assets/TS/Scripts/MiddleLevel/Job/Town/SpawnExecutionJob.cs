using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnExecutionJob : IJobEntity
{
    [ReadOnly] public float currentTime;
    [ReadOnly] public ComponentLookup<TSObjectComponent> objectLookup;
    [ReadOnly] public ComponentLookup<SpawnConfigComponent> spawnConfigLookup;

    [ReadOnly] public BufferLookup<SpawnedEntityBuffer> spawnedEntityBufferLookup;
    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [EntityIndexInQuery] int entityIndexInQuery,
        Entity entity,
        ref SpawnRequestComponent spawnRequest)
    {
        if (!spawnRequest.IsActive)
            return;

        // 스폰 오브젝트 인스턴스 생성 (하위 오브젝트들도 함께 생성됨)
        var spawnedEntity = ecb.Instantiate(entityIndexInQuery, spawnRequest.SpawnObject);
        FixedString64Bytes name = $"Spawned {spawnRequest.Name} {spawnedEntity.Index}";

        ecb.SetName(entityIndexInQuery, spawnedEntity, in name);

        // 스폰된 오브젝트의 위치 설정
        float3 finalPosition;
        if (objectLookup.HasComponent(spawnRequest.SpawnObject))
        {
            var objectComponent = objectLookup[spawnRequest.SpawnObject];

            // RootOffset을 고려한 위치 계산
            finalPosition = new float3(
                spawnRequest.SpawnPosition.x,
                spawnRequest.SpawnPosition.y - objectComponent.RootOffset,
                0f
            );
        }
        else
        {
            finalPosition = new float3(spawnRequest.SpawnPosition.x, spawnRequest.SpawnPosition.y, 0f);
        }

        ecb.SetComponent(entityIndexInQuery, spawnedEntity, LocalTransform.FromPosition(finalPosition));

        // 스폰된 오브젝트에 SpawnedObjectComponent 추가
        ecb.AddComponent(entityIndexInQuery, spawnedEntity, new SpawnedObjectComponent
        {
            Spawner = spawnRequest.Spawner, // 스포너 엔티티 참조 설정
            SpawnTime = currentTime,
            IsManaged = true
        });

        // 스포너의 SpawnedEntityBuffer에 스폰된 Entity 추가
        ecb.AppendToBuffer(entityIndexInQuery, spawnRequest.Spawner, new SpawnedEntityBuffer
        {
            SpawnedEntity = spawnedEntity,
            SpawnTime = currentTime
        });

        // 스폰 요청 제거
        ecb.DestroyEntity(entityIndexInQuery, entity);
    }
}