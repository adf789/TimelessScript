using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnExecutionJob : IJobEntity
{
    [ReadOnly] public float currentTime;
    public EntityCommandBuffer ecb;
    [NativeDisableUnsafePtrRestriction]
    public RefRW<RecycleComponent> recycleComponent;

    public void Execute(
        Entity entity,
        ref SpawnRequestComponent spawnRequest)
    {
        if (!spawnRequest.IsActive)
            return;

        // 스폰 오브젝트 인스턴스 생성 (하위 오브젝트들도 함께 생성됨)
        var spawnedEntity = ecb.Instantiate(spawnRequest.SpawnObject);
        FixedString64Bytes name = $"Spawned {spawnRequest.Name} {spawnedEntity.Index}";

        ecb.SetName(spawnedEntity, in name);

        // 스폰된 오브젝트의 위치 설정
        float3 finalPosition = new float3(spawnRequest.SpawnPosition.x, spawnRequest.SpawnPosition.y, 0f);

        ecb.SetComponent(spawnedEntity, LocalTransform.FromPosition(finalPosition));

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

        // 스폰 요청 제거
        ecb.DestroyEntity(entity);
    }
}