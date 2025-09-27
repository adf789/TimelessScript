using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct SpawnCleanupJob : IJobEntity
{
    [ReadOnly] public float currentTime;
    [ReadOnly] public ComponentLookup<SpawnedObjectComponent> spawnedObjectLookup;

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [EntityIndexInQuery] int entityInQueryIndex,
        Entity entity,
        ref SpawnConfigComponent spawnConfig)
    {
        // 스폰된 오브젝트들의 상태를 확인하고 카운트 업데이트
        int validSpawnCount = 0;

        // 이 부분은 실제로는 스폰된 오브젝트들을 추적하는
        // 별도의 DynamicBuffer나 컬렉션을 사용해야 합니다.
        // 현재는 간단한 구현으로 대체합니다.

        spawnConfig.CurrentSpawnCount = validSpawnCount;
    }
}