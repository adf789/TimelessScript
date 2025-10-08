
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(ControlSystem))]
public partial class GamePreprocessingSystem : SystemBase
{
    private Dictionary<uint, Item> collectItems = new Dictionary<uint, Item>();

    protected override void OnCreate()
    {
        if (!SystemAPI.HasSingleton<RecycleComponent>())
        {
            var entity = EntityManager.CreateEntity(typeof(RecycleComponent));

            EntityManager.SetComponentData(entity, new RecycleComponent
            {
                RemoveActors = new NativeQueue<TSActorComponent>(Allocator.Persistent)
            });
        }

        RequireForUpdate<RecycleComponent>();
    }

    protected override void OnUpdate()
    {
        OnUpdateActorLifeCycle();

        OnUpdateColllectItems();
    }

    private void OnUpdateActorLifeCycle()
    {
        var recycle = SystemAPI.GetSingletonRW<RecycleComponent>();
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        float deltaTime = World.Time.DeltaTime;

        Entities
        .WithAll<TSActorComponent>()
        .WithoutBurst()
        .ForEach((Entity entity, ref TSActorComponent actorComponent) =>
        {
            // 액터 생명시간 체크
            actorComponent.LifePassingTime += deltaTime;

            if (actorComponent.LifePassingTime < actorComponent.LifeTime)
                return;

            // 시간이 모두 지나면 얻은 아이템들 인벤토리로 획득
            CollectingItemByInteract(entity, ref actorComponent, in ecb);

            // 컴포넌트 값 재사용
            recycle.ValueRW.AddActor(actorComponent);

            // 엔티티 삭제
            ecb.DestroyEntity(entity);
        }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void OnUpdateColllectItems()
    {
        var player = PlayerSubManager.Instance;
        foreach (var item in collectItems)
        {
            // 아이템 획득 로직 추가
            player.Inventory.Add(item.Key, item.Value.Count);
        }

        if (collectItems.Count > 0)
            ObserverSubManager.Instance.NotifyObserver(new ShowCurrencyParam());

        collectItems.Clear();
    }

    private void CollectingItemByInteract(Entity entity, ref TSActorComponent actorComponent, in EntityCommandBuffer ecb)
    {
        if (!SystemAPI.HasBuffer<InteractBuffer>(entity))
            return;

        var transform = SystemAPI.GetComponent<LocalTransform>(entity);
        var interactBuffer = SystemAPI.GetBuffer<InteractBuffer>(entity);
        long totalCount = 0;

        foreach (var interact in interactBuffer)
        {
            switch (interact.DataType)
            {
                case TableDataType.Gimmick:
                    {
                        var gimmickTable = TableSubManager.Instance.Get<GimmickTable>();
                        var gimmickData = gimmickTable.Get(interact.DataID);

                        if (gimmickData == null)
                            continue;

                        long count = gimmickData.AcquireCount;
                        var itemData = TableSubManager.Instance.Get<ItemTable>().Get(gimmickData.AcquireItem);

                        if (itemData == null)
                            continue;

                        if (collectItems.TryGetValue(itemData.ID, out var item))
                            collectItems[itemData.ID] = item.AddCount(count);
                        else
                            collectItems[itemData.ID] = new Item(itemData, count);

                        totalCount += count;
                    }
                    break;
            }
        }

        ObserverSubManager.Instance.NotifyObserver(new RewardEffectParam()
        {
            Position = transform.Position.xy,
            RewardCount = (int) totalCount
        });

        interactBuffer.Clear();
    }
}
