
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
        var targetHolder = SystemAPI.GetSingletonRW<TargetHolderComponent>();

        if (targetHolder.ValueRW.Target.IsNull)
            return;

        // 선택된 오브젝트 해제
        var selectTarget = targetHolder.ValueRW.Target;
        var touchPosition = targetHolder.ValueRW.TouchPosition;
        targetHolder.ValueRW.Release();

        if (target == Entity.Null)
        {
            if (selectTarget.ObjectType == TSObjectType.Actor)
            {
                if (SystemAPI.HasComponent<LightweightPhysicsComponent>(selectTarget.Self))
                {
                    var physics = SystemAPI.GetComponentRO<LightweightPhysicsComponent>(selectTarget.Self);

                    if (!physics.ValueRO.isGrounded)
                        return;
                }
                else if (SystemAPI.HasComponent<SpriteSheetAnimationComponent>(selectTarget.Self))
                {
                    var animation = SystemAPI.GetComponentRO<SpriteSheetAnimationComponent>(selectTarget.Self);

                    if (animation.ValueRO.CurrentState != AnimationState.Idle)
                        return;
                }

                target = selectTarget.Self;

                Debug.Log($"Select {selectTarget.Name}");
            }
            return;
        }

        if (target == selectTarget.Self)
        {
            target = Entity.Null;

            Debug.Log($"Unselect {selectTarget.Name}");
            return;
        }
        else
        {
            if (selectTarget.ObjectType == TSObjectType.Actor)
            {
                target = selectTarget.Self;
                return;
            }

            var objectInfo = SystemAPI.GetComponentRW<TSObjectComponent>(target);

            switch (selectTarget.ObjectType)
            {
                case TSObjectType.Ground:
                    {
                        var collider = SystemAPI.GetComponent<LightweightColliderComponent>(selectTarget.Self);
                        float2 position = collider.position + collider.offset;
                        float halfHeight = collider.size.y * 0.5f;

                        position.x = touchPosition.x;
                        position.y += halfHeight;

                        // Navigation 컴포넌트가 있으면 Navigation 시스템 사용
                        if (SystemAPI.HasComponent<NavigationComponent>(target))
                        {
                            var navigation = SystemAPI.GetComponentRW<NavigationComponent>(target);
                            navigation.ValueRW.IsActive = true;
                            navigation.ValueRW.FinalTargetPosition = position;
                            navigation.ValueRW.FinalTargetGround = selectTarget.Self;
                            navigation.ValueRW.CurrentWaypointIndex = 0;
                            navigation.ValueRW.State = NavigationState.PathFinding;
                        }
                        else
                        {
                            // 기존 방식 - 직접 이동
                            objectInfo.ValueRW.Behavior.Target = selectTarget.Self;
                            objectInfo.ValueRW.Behavior.Purpose = MoveState.Move;
                            objectInfo.ValueRW.Behavior.MovePosition = position;
                        }

                        Debug.Log($"position: {position}, touchPosition: {touchPosition}");
                    }
                    break;
            }
        }
    }
}