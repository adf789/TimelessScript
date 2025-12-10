using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnJob : IJobEntity
{
    [ReadOnly] public float CurrentTime;
    [ReadOnly] public ComponentLookup<WorldPositionComponent> WorldPositionLookup;

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [EntityIndexInQuery] int entityInQueryIndex,
        Entity spawnerEntity,
        ref SpawnConfigComponent spawnConfig,
        in LocalTransform transform,
        in ColliderComponent collider)
    {
        // 스폰 쿨다운 체크
        if (CurrentTime < spawnConfig.NextSpawnTime)
            return;

        // 최대 스폰 개수 체크
        if (spawnConfig.ReadySpawnCount >= spawnConfig.MaxSpawnCount)
            return;

        // 스폰 가능한 위치 찾기
        FindValidSpawnPosition(entityInQueryIndex, spawnConfig.PositionYOffset, in transform, in collider, out float3 spawnPosition);

        // 부모 위치 가져옴
        float3 parentPosition = float3.zero;

        if (WorldPositionLookup.TryGetComponent(spawnConfig.SpawnParent, out var worldPositionComponent))
        {
            parentPosition += worldPositionComponent.WorldOffset;
        }

        parentPosition += transform.Position;

        // 스폰 요청 생성
        var spawnRequestEntity = ecb.CreateEntity(entityInQueryIndex);
        ecb.AddComponent(entityInQueryIndex, spawnRequestEntity, new SpawnRequestComponent
        {
            SpawnObject = spawnConfig.SpawnObjectPrefab,
            SpawnParent = spawnConfig.SpawnParent,
            Spawner = spawnerEntity, // 스포너 Entity 참조 설정
            ObjectType = spawnConfig.ObjectType, // Entity 오브젝트 타입
            Name = spawnConfig.Name,
            SpawnPosition = spawnPosition,
            ParentPosition = parentPosition,
            LayerOffset = spawnConfig.LayerOffset,
            IsActive = true
        });

        // 스폰 카운트 및 다음 스폰 시간 업데이트
        spawnConfig.ReadySpawnCount++;

        // 스폰 성공 여부와 관계없이 다음 스폰 시간 업데이트
        spawnConfig.NextSpawnTime = CurrentTime + spawnConfig.SpawnCooldown;
    }

    private void FindValidSpawnPosition(
        int entityIndex,
        float yOffset,
        in LocalTransform transform,
        in ColliderComponent collider,
        out float3 spawnPosition)
    {
        float halfWidth = collider.Size.x * 0.5f;
        float halfHeight = collider.Size.y * 0.5f;

        uint seed = (uint) (CurrentTime * IntDefine.TIME_MILLISECONDS_ONE) +
                       (uint) (transform.Position.x * 10) +
                       (uint) (transform.Position.y * 100) +
                       (uint) entityIndex * 13 + 1;

        var random = new Random(seed);
        float3 randomOffset = new float3(random.NextFloat(-halfWidth, halfWidth), halfHeight, 0);
        float3 candidatePosition = randomOffset;

        candidatePosition.y += yOffset;

        spawnPosition = candidatePosition;
    }
}