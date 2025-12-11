using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnJob : IJobEntity
{
    [ReadOnly] public float CurrentTime;

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [EntityIndexInQuery] int entityInQueryIndex,
        Entity spawnerEntity,
        ref SpawnConfigComponent spawnConfig,
        in LocalTransform transform,
        in LocalToWorld worldPosition,
        in ColliderComponent collider)
    {
        // 스폰 쿨다운 체크
        if (CurrentTime < spawnConfig.NextSpawnTime)
            return;

        // 최대 스폰 개수 체크
        if (spawnConfig.ReadySpawnCount >= spawnConfig.MaxSpawnCount)
            return;

        // 스폰 가능한 위치 찾기
        FindValidSpawnPosition(entityInQueryIndex, spawnConfig.PositionYOffset, in collider, out float3 spawnPosition);

        // 스폰 요청 생성
        var spawnRequestEntity = ecb.CreateEntity(entityInQueryIndex);

        var spawnRequest = new SpawnRequestComponent
        {
            SpawnObject = spawnConfig.SpawnObjectPrefab,
            SpawnParent = spawnConfig.SpawnParent,
            Spawner = spawnerEntity, // 스포 Entity 참조 설정
            ObjectType = spawnConfig.ObjectType, // Entity 오브젝트 타입
            Name = spawnConfig.Name,
            SpawnPosition = spawnPosition,
            LayerOffset = spawnConfig.LayerOffset,
            IsActive = true
        };

        ecb.AddComponent(entityInQueryIndex, spawnRequestEntity, spawnRequest);

        // 스폰 카운트 및 다음 스폰 시간 업데이트
        spawnConfig.ReadySpawnCount++;

        // 스폰 성공 여부와 관계없이 다음 스폰 시간 업데이트
        spawnConfig.NextSpawnTime = CurrentTime + spawnConfig.SpawnCooldown;
    }

    private void FindValidSpawnPosition(
        int entityIndex,
        float yOffset,
        in ColliderComponent collider,
        out float3 spawnPosition)
    {
        float halfWidth = collider.Size.x * 0.5f;
        float halfHeight = collider.Size.y * 0.5f;

        uint seed = (uint) (CurrentTime * IntDefine.TIME_MILLISECONDS_ONE) +
                       (uint) entityIndex * 13 + 1;

        var random = new Random(seed);

        spawnPosition = new float3(random.NextFloat(-halfWidth, halfWidth), halfHeight + yOffset, 0);
    }
}