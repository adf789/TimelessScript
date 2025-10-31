// Assets/TS/Scripts/HighLevel/System/Common/PickedSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial class PickedSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PickedComponent>();

        // Ensure the singleton for holding the target exists.
        if (!SystemAPI.HasSingleton<TargetHolderComponent>())
        {
            EntityManager.CreateEntity(typeof(TargetHolderComponent));
        }
    }


    protected override void OnUpdate()
    {
        if (!TouchSubManager.Instance.CheckTouchDown())
            return;

        float2 touchPosition = GetTouchPosition();

        // Get the singleton for read-write access.
        var targetHolder = SystemAPI.GetSingletonRW<TargetHolderComponent>();

        // 1. Reset the target in the singleton and deselect all previously selected entities.
        targetHolder.ValueRW.Target = default;
        var currentlyPickedEntity = Entity.Null;

        // 2. Find the highest priority entity at the touch position.
        int maxOrder = int.MinValue;
        foreach (var (picked, bounds, entity) in SystemAPI.Query<RefRO<PickedComponent>, RefRO<ColliderBoundsComponent>>().WithEntityAccess())
        {
            var boundsValue = new Rect(bounds.ValueRO.Min, bounds.ValueRO.Max - bounds.ValueRO.Min);
            if (boundsValue.Contains(touchPosition.xy))
            {
                if (picked.ValueRO.Order > maxOrder)
                {
                    maxOrder = picked.ValueRO.Order;
                    currentlyPickedEntity = entity;
                }
            }
        }

        // 3. If an entity is picked, update the singleton and mark it as selected.
        if (currentlyPickedEntity != Entity.Null)
        {
            if (SystemAPI.HasComponent<TSObjectComponent>(currentlyPickedEntity))
            {
                targetHolder.ValueRW.Target = SystemAPI.GetComponent<TSObjectComponent>(currentlyPickedEntity);
                targetHolder.ValueRW.TouchPosition = touchPosition;
            }
        }
    }

    private float2 GetTouchPosition()
    {
        var position = TouchSubManager.Instance.GetScreenTouchPosition(Camera.main);

        return new float2(position.x, position.y);
    }
}
