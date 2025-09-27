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

    public EntityCommandBuffer.ParallelWriter ecb;

    public void Execute(
        [EntityIndexInQuery] int entityInQueryIndex,
        Entity entity,
        ref SpawnRequestComponent spawnRequest)
    {
        if (!spawnRequest.IsActive)
            return;

        // 스폰 오브젝트 인스턴스 생성 (하위 오브젝트들도 함께 생성됨)
        var spawnedEntity = ecb.Instantiate(entityInQueryIndex, spawnRequest.SpawnObject);
        FixedString64Bytes name = "Spawned Gimmick";

        ecb.SetName(entityInQueryIndex, spawnedEntity, in name);

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

        ecb.SetComponent(entityInQueryIndex, spawnedEntity, LocalTransform.FromPosition(finalPosition));

        // 스폰된 오브젝트에 SpawnedObjectComponent 추가
        ecb.AddComponent(entityInQueryIndex, spawnedEntity, new SpawnedObjectComponent
        {
            Spawner = Entity.Null, // 스포너 엔티티 참조는 SpawnJob에서 설정
            SpawnTime = currentTime,
            IsManaged = true
        });

        // 하위 오브젝트들에게도 기본적인 설정 적용
        // Instantiate 메서드는 기본적으로 프리팹의 모든 하위 오브젝트들을 포함하여 생성합니다.
        // 추가적인 하위 오브젝트 설정이 필요한 경우 여기에 로직을 추가할 수 있습니다.

        // 스폰 요청 제거
        ecb.DestroyEntity(entityInQueryIndex, entity);
    }
}