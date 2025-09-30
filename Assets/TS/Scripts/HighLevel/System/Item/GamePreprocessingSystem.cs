
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
            actorComponent.LifePassingTime += deltaTime;

            if (actorComponent.LifePassingTime < actorComponent.LifeTime)
                return;

            CollectingItemByInteract(entity, ref actorComponent, in ecb);

            recycle.ValueRW.AddActor(actorComponent);

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

            var getItem = player.Inventory.GetItem(item.Key);

            Debug.Log($"Collect Item: {getItem.ID}, Count: {getItem.Count}");
        }

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
