
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PickedSystem))]
[BurstCompile]
public partial struct ControlSystem : ISystem
{
    private Entity target;

    public void OnCreate(ref SystemState state)
       => state.RequireForUpdate<TSObjectComponent>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var targetHolder = SystemAPI.GetSingleton<TargetHolderComponent>();

        if (targetHolder.Target.IsNull)
            return;

        if (target == Entity.Null)
        {
            if (targetHolder.Target.ObjectType == TSObjectType.Actor)
            {
                target = targetHolder.Target.Self;

                Debug.Log($"Select {target}");
            }
        }
        else if (target != targetHolder.Target.Self)
        {
            var objectInfo = SystemAPI.GetComponentRW<TSObjectComponent>(target);
            target = default;

            switch (targetHolder.Target.ObjectType)
            {
                case TSObjectType.Actor:
                    {
                        target = targetHolder.Target.Self;
                    }
                    break;
                case TSObjectType.Ground:
                    {
                        var collider = SystemAPI.GetComponent<LightweightColliderComponent>(targetHolder.Target.Self);
                        float2 position = collider.position + collider.offset;
                        float2 touchPosition = targetHolder.TouchPosition;
                        float halfHeight = collider.size.y * 0.5f;

                        position.x = touchPosition.x;
                        position.y += halfHeight;

                        objectInfo.ValueRW.Behavior.Purpose = MoveState.Move;
                        objectInfo.ValueRW.Behavior.MovePosition = position;

                        Debug.Log($"position: {position}, touchPosition: {touchPosition}");
                    }
                    break;
            }
        }
    }
}