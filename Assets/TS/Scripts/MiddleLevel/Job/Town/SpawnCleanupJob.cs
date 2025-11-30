using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public partial struct SpawnCleanupJob : IJobEntity
{
    [ReadOnly] public EntityStorageInfoLookup entityLookup;

    public void Execute(
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
            if (entityLookup.Exists(spawnedEntity))
            {
                validSpawnCount++;
            }
            else
            {
                spawnedObjects.RemoveAt(i);
            }
        }

        spawnConfig.ReadySpawnCount = spawnConfig.CurrentSpawnCount = validSpawnCount;
    }
}