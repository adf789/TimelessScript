
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
       => state.RequireForUpdate<TSObjectInfoComponent>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var targetHolder = SystemAPI.GetSingleton<TargetHolderComponent>();

        if (targetHolder.Target.IsNull)
            return;

        if (target == Entity.Null)
        {
            if (targetHolder.Target.Behavior == BehaviorType.Actor)
            {
                target = targetHolder.Target.Target;

                Debug.Log($"Select {target}");
            }
        }
        else if (target != targetHolder.Target.Target)
        {
            var objectInfo = SystemAPI.GetComponentRW<TSObjectInfoComponent>(target);

            switch (targetHolder.Target.Behavior)
            {
                case BehaviorType.Actor:
                    {
                        target = targetHolder.Target.Target;
                    }
                    break;
                case BehaviorType.Ground:
                    {
                        objectInfo.ValueRW.State = MoveState.Move;

                        var collider = SystemAPI.GetComponent<LightweightColliderComponent>(targetHolder.Target.Target);
                        float2 position = collider.position + collider.offset;
                        float2 touchPosition = targetHolder.TouchPosition;
                        float halfHeight = collider.size.y * 0.5f;

                        position.x = touchPosition.x;
                        position.y += halfHeight;

                        Debug.Log($"position: {position}, touchPosition: {touchPosition}");
                    }
                    break;
            }
        }
    }
}