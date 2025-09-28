using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct SpawnCleanupJob : IJobEntity
{
    [ReadOnly] public float currentTime;
    [ReadOnly] public EntityStorageInfoLookup entityLookup;
    [ReadOnly] public ComponentLookup<SpawnedObjectComponent> spawnedObjectLookup;

    public void Execute(
        [EntityIndexInQuery] int entityInQueryIndex,
        Entity entity,
        ref SpawnConfigComponent spawnConfig,
        ref DynamicBuffer<SpawnedEntityBuffer> spawnedObjects)
    {
        // 스폰된 오브젝트들의 상태를 확인하고 카운트 업데이트
        int validSpawnCount = 0;

        // 역순으로 순회하여 삭제 시 인덱스 문제 방지
        for (int i = spawnedObjects.Length - 1; i >= 0; i--)
        {
            var spawnedEntity = spawnedObjects[i].SpawnedEntity;

            // Entity가 존재하고 SpawnedObjectComponent를 가지고 있는지 확인
            if (entityLookup.Exists(spawnedEntity) &&
                spawnedObjectLookup.HasComponent(spawnedEntity))
            {
                var spawnedObj = spawnedObjectLookup[spawnedEntity];

                // 이 스포너에 의해 생성된 오브젝트인지 확인
                if (spawnedObj.Spawner == entity)
                {
                    validSpawnCount++;
                }
                else
                {
                    // 다른 스포너의 오브젝트이면 버퍼에서 제거
                    spawnedObjects.RemoveAt(i);
                }
            }
            else
            {
                // 존재하지 않는 Entity이면 버퍼에서 제거
                spawnedObjects.RemoveAt(i);
            }
        }

        spawnConfig.CurrentSpawnCount = validSpawnCount;
    }
}