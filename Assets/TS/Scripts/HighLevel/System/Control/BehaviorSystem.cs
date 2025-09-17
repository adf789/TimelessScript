
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionSystem))]
[BurstCompile]
public partial struct BehaviorSystem : ISystem
{
    private Entity target;

    public void OnCreate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<BehaviorComponent>().WithAll<LightweightPhysicsComponent>().Build();

        state.RequireAnyForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (pickedTag, physics, collider, entity) in
        SystemAPI.Query<RefRO<IsPickedTag>,
        RefRW<LightweightPhysicsComponent>,
        RefRW<LightweightColliderComponent>>().WithEntityAccess())
        {
            if (target.Equals(Entity.Null))
            {
                if (!physics.ValueRW.isStatic)
                {
                    target = entity;

                    Debug.Log($"Select {target}");
                }
                break;
            }
            else if (!target.Equals(entity))
            {
                target = default;
                
                float2 position = collider.ValueRW.position + collider.ValueRW.offset;
                float2 touchPosition = pickedTag.ValueRO.TouchPosition;
                float halfHeight = collider.ValueRW.size.y * 0.5f;

                position.x = touchPosition.x;
                position.y += halfHeight;

                Debug.Log($"position: {position}, touchPosition: {touchPosition}");

                break;
            }
        }

        var behaviorJob = new BehaviorJob()
        {
            animationComponentLookup = SystemAPI.GetComponentLookup<SpriteSheetAnimationComponent>(false)
        };

        state.Dependency = behaviorJob.ScheduleParallel(state.Dependency);
    }
}