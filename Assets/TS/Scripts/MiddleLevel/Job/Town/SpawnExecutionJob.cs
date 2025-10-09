using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct SpawnExecutionJob : IJobEntity
{
    [ReadOnly] public float currentTime;
    public EntityCommandBuffer ecb;
    [NativeDisableUnsafePtrRestriction]
    public RefRW<RecycleComponent> recycleComponent;
    [ReadOnly] public BufferLookup<LinkedEntityGroup> linkedEntityGroupLookup;
    public BufferLookup<AvailableLayerBuffer> availableLayerLookup;
    public ComponentLookup<SpawnConfigComponent> spawnConfigLookup;

    public void Execute(
        Entity entity,
        ref SpawnRequestComponent spawnRequest)
    {
        if (!spawnRequest.IsActive)
            return;

        // 스포터 옵션 가져옴
        var spawnConfig = spawnConfigLookup.GetRefRW(spawnRequest.Spawner);

        if (!spawnConfig.IsValid)
            return;

        // 스폰 오브젝트 인스턴스 생성 (하위 오브젝트들도 함께 생성됨)
        var spawnedEntity = ecb.Instantiate(spawnRequest.SpawnObject);
        FixedString64Bytes name = $"Spawned {spawnRequest.Name} {spawnedEntity.Index}";

        ecb.SetName(spawnedEntity, in name);

        // 스폰된 오브젝트의 위치 설정
        ecb.SetComponent(spawnedEntity, LocalTransform.FromPosition(spawnRequest.SpawnPosition));

        switch (spawnRequest.ObjectType)
        {
            case TSObjectType.Actor:
                {
                    if (recycleComponent.ValueRW.GetActorCount() > 0)
                    {
                        var actorComponent = recycleComponent.ValueRW.GetActor();

                        actorComponent.LifePassingTime = 0;

                        ecb.AddComponent<TSActorComponent>(spawnedEntity, actorComponent);
                        ecb.AddComponent<MoveRestoreFlagComponent>(spawnedEntity);
                    }
                }
                break;
        }

        // 스폰된 오브젝트에 SpawnedObjectComponent 추가
        ecb.AddComponent(spawnedEntity, new SpawnedObjectComponent
        {
            Spawner = spawnRequest.Spawner, // 스포너 엔티티 참조 설정
            SpawnTime = currentTime,
            IsManaged = true
        });

        // 스포너의 SpawnedEntityBuffer에 스폰된 Entity 추가
        ecb.AppendToBuffer(spawnRequest.Spawner, new SpawnedEntityBuffer
        {
            SpawnedEntity = spawnedEntity,
            SpawnTime = currentTime
        });

        // 스폰된 오브젝트의 애니메이션 컴포넌트 연결
        int layer = -1;

        // 1. 재사용 가능한 Layer 큐에서 가져오기
        if (availableLayerLookup.HasBuffer(spawnRequest.Spawner))
        {
            var availableLayers = availableLayerLookup[spawnRequest.Spawner];

            if (availableLayers.Length > 0)
            {
                // 큐에서 첫 번째 layer 가져오고 제거
                layer = availableLayers[0].Layer;
                availableLayers.RemoveAt(0);
            }
        }

        // 설정된 레이어가 없으면 스폰된 엔티티 수로 결정
        if (layer == -1)
        {
            layer = spawnConfig.ValueRW.CurrentSpawnCount + spawnConfig.ValueRW.LayerOffset;
        }

        ecb.AddComponent(spawnedEntity, new AnimationLinkerFlagComponent()
        {
            Layer = layer
        });

        // 스폰된 오브젝트 수 추가
        spawnConfig.ValueRW.CurrentSpawnCount++;

        // 스폰 요청 제거
        ecb.DestroyEntity(entity);
    }
}