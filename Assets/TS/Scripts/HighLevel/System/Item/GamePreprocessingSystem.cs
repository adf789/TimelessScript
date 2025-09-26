
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(ControlSystem))]
public partial class GamePreprocessingSystem : SystemBase
{
    private Dictionary<uint, Item> collectItems = new Dictionary<uint, Item>();

    protected override void OnCreate()
    {

    }

    protected override void OnUpdate()
    {
        OnUpdateActorLifeCycle();

        OnUpdateColllectItems();
    }

    private void OnUpdateActorLifeCycle()
    {
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

            var gimmickTable = TableSubManager.Instance.Get<GimmickTable>();

            // for (int i = 0; i < actorComponent.ActionStack.InteractCount; i++)
            // {
            //     var interact = actorComponent.ActionStack.RemoveInteract();

            //     switch (interact.DataType)
            //     {
            //         case TableDataType.Gimmick:
            //             {
            //                 var gimmickData = gimmickTable.Get(interact.DataID);

            //                 if (gimmickData == null)
            //                     continue;

            //                 long count = gimmickData.AcquireCount;
            //                 var itemData = TableSubManager.Instance.Get<ItemTable>().Get(gimmickData.AcquireItem);

            //                 if (itemData == null)
            //                     continue;

            //                 Debug.Log($"Acquire Item: {itemData.Name}, Count: {count}");

            //                 if (collectItems.TryGetValue(itemData.ID, out var item))
            //                     collectItems[itemData.ID] = item.AddCount(count);
            //                 else
            //                     collectItems[itemData.ID] = new Item(itemData, count);
            //             }
            //             break;
            //     }
            // }

            ecb.DestroyEntity(entity);
        }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private void OnUpdateColllectItems()
    {
        foreach (var item in collectItems)
        {
            // 아이템 획득 로직 추가
        }

        collectItems.Clear();
    }
}
