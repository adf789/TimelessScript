using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnExecutionJob : IJobEntity
{
    [ReadOnly] public float currentTime;
    public EntityCommandBuffer ecb;
    public BufferLookup<AvailableActorBuffer> availableActorLookup;
    public ComponentLookup<SpawnConfigComponent> spawnConfigLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> localToWorldLookup;

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

        // 스폰 오브젝트 인스턴스 생성 (하위 오브 젝트들도 함께 생성됨)
        var spawnedEntity = ecb.Instantiate(spawnRequest.SpawnObject);
        FixedString64Bytes name = $"{spawnRequest.Name} {spawnedEntity.Index}";
        int layer = -1;

        ecb.SetName(spawnedEntity, in name);

        // 스폰된 오브젝트의 위치 설정
        ecb.AddComponent(spawnedEntity, new Parent { Value = spawnRequest.SpawnParent });

        // 월드 좌표 설정
        ecb.AddComponent(spawnedEntity, new WorldPositionComponent()
        {
            WorldOffset = spawnRequest.ParentPosition
        });

        // World 좌표를 Parent의 Local 좌표로 변환
        var parentLocalToWorld = localToWorldLookup[spawnRequest.SpawnParent];
        var localPosition = math.transform(math.inverse(parentLocalToWorld.Value), spawnRequest.SpawnPosition);
        ecb.SetComponent(spawnedEntity, LocalTransform.FromPosition(localPosition));

        switch (spawnRequest.ObjectType)
        {
            case TSObjectType.Actor:
                {
                    var availableActors = availableActorLookup[spawnRequest.Spawner];
                    if (availableActors.Length > 0)
                    {
                        // 재사용 가능한 액터의 정보를 가져옴
                        var actorComponent = availableActors[0].Actor;
                        layer = availableActors[0].Layer;

                        // 가져온 액터 제거
                        availableActors.RemoveAt(0);

                        actorComponent.LifePassingTime = 0;

                        ecb.AddComponent(spawnedEntity, actorComponent);
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