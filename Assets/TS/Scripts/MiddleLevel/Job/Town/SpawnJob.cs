using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnJob : IJobEntity
{
    [ReadOnly] public float currentTime;
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [EntityIndexInQuery] int entityInQueryIndex,
        Entity spawnerEntity,
        ref SpawnConfigComponent spawnConfig,
        in LocalTransform transform,
        in ColliderComponent collider)
    {
        // 스폰 쿨다운 체크
        if (currentTime < spawnConfig.NextSpawnTime)
            return;

        // 최대 스폰 개수 체크
        if (spawnConfig.ReadySpawnCount >= spawnConfig.MaxSpawnCount)
            return;

        // 스폰 가능한 위치 찾기
        FindValidSpawnPosition(entityInQueryIndex, spawnConfig.PositionYOffset, in transform, in collider, out float2 spawnPosition);

        // 스폰 요청 생성
        var spawnRequestEntity = ecb.CreateEntity(entityInQueryIndex);
        ecb.AddComponent(entityInQueryIndex, spawnRequestEntity, new SpawnRequestComponent
        {
            SpawnObject = spawnConfig.SpawnObjectPrefab,
            Spawner = spawnerEntity, // 스포너 Entity 참조 설정
            ObjectType = spawnConfig.ObjectType, // Entity 오브젝트 타입
            Name = spawnConfig.Name,
            SpawnPosition = spawnPosition,
            IsActive = true
        });

        // 스폰 카운트 및 다음 스폰 시간 업데이트
        spawnConfig.ReadySpawnCount++;

        // 스폰 성공 여부와 관계없이 다음 스폰 시간 업데이트
        spawnConfig.NextSpawnTime = currentTime + spawnConfig.SpawnCooldown;
    }

    private void FindValidSpawnPosition(
        int entityIndex,
        float yOffset,
        in LocalTransform transform,
        in ColliderComponent collider,
        out float2 spawnPosition)
    {
        float halfWidth = collider.Size.x * 0.5f;
        float halfHeight = collider.Size.y * 0.5f;

        uint seed = (uint) (currentTime * IntDefine.TIME_MILLISECONDS_ONE) +
                       (uint) (transform.Position.x * 10) +
                       (uint) (transform.Position.y * 100) +
                       (uint) entityIndex * 13 + 1;

        var random = new Unity.Mathematics.Random(seed);
        float2 randomOffset = new float2(random.NextFloat(-halfWidth, halfWidth), halfHeight);
        float2 candidatePosition = transform.Position.xy + randomOffset;

        candidatePosition.y += yOffset;

        spawnPosition = candidatePosition;
    }
}