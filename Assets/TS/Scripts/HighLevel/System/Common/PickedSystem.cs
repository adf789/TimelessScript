// Assets/TS/Scripts/HighLevel/System/Common/PickedSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionSystem))]
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
        if (!CheckTouchDown())
            return;

        float2 touchPosition = GetTouchPosition();

        // Get the singleton for read-write access.
        var targetHolder = SystemAPI.GetSingletonRW<TargetHolderComponent>();

        // 1. Reset the target in the singleton.
        targetHolder.ValueRW.Target = default;

        var currentlyPickedEntity = Entity.Null;

        // 2. Find the highest priority entity at the touch position.
        int maxOrder = int.MinValue;
        foreach (var (picked, bounds, entity) in SystemAPI.Query<RefRO<PickedComponent>, RefRO<ColliderBoundsComponent>>().WithEntityAccess())
        {
            var boundsValue = new Rect(bounds.ValueRO.min, bounds.ValueRO.max - bounds.ValueRO.min);
            if (boundsValue.Contains(touchPosition.xy))
            {
                if (picked.ValueRO.Order > maxOrder)
                {
                    maxOrder = picked.ValueRO.Order;
                    currentlyPickedEntity = entity;
                }
            }
        }

        // 3. If an entity is picked, update the singleton with its physics component.
        if (currentlyPickedEntity != Entity.Null)
        {
            if (SystemAPI.HasComponent<TSObjectInfoComponent>(currentlyPickedEntity))
            {
                targetHolder.ValueRW.Target = SystemAPI.GetComponent<TSObjectInfoComponent>(currentlyPickedEntity);
                targetHolder.ValueRW.TouchPosition = touchPosition;
            }
        }
    }

    private bool CheckTouchDown()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private float2 GetTouchPosition()
    {
        if (Mouse.current == null) return float2.zero;
        if (Camera.main == null) return float2.zero;

        var screenPos = Mouse.current.position.ReadValue();
        var worldPos = Camera.main.ScreenToWorldPoint(new float3(screenPos.x, screenPos.y, 0));
        return new float2(worldPos.x, worldPos.y);
    }
}
