
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionSystem))]
[BurstCompile]
public partial struct BehaviorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<BehaviorComponent>().WithAll<LightweightPhysicsComponent>().Build();

        state.RequireAnyForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // BehaviorComponent를 가진 부모 엔티티를 찾습니다.
        foreach (var (behavior, physics, parentEntity)
                 in SystemAPI.Query<RefRO<BehaviorComponent>, RefRO<LightweightPhysicsComponent>>().WithEntityAccess())
        {
            // 1. 부모 엔티티의 자식 목록(DynamicBuffer<Child>)을 가져옵니다.
            if (!SystemAPI.HasBuffer<Child>(parentEntity))
            {
                continue; // 자식이 없으면 다음 부모로 넘어갑니다.
            }
            DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(parentEntity);

            bool isGrounded = physics.ValueRO.isGrounded;

            // 2. 자식 목록을 순회하여 원하는 컴포넌트를 가진 자식을 찾습니다.
            foreach (var child in children)
            {
                var childEntity = child.Value;

                // 3. 자식 엔티티가 SpriteSheetAnimationComponent와 Authoring을 모두 가지고 있는지 확인합니다.
                if (SystemAPI.HasComponent<SpriteSheetAnimationComponent>(childEntity))
                {
                    // 4. 자식의 컴포넌트들을 가져옵니다.
                    var animComponent = SystemAPI.GetComponentRW<SpriteSheetAnimationComponent>(childEntity);

                    if (isGrounded && animComponent.ValueRW.CurrentKey == "Jump_Idle")
                    {
                        if(animComponent.ValueRW.StartKey != "Jump_Land")
                            animComponent.ValueRW.StartKey = "Jump_Land";
                    }
                    else if (!isGrounded && animComponent.ValueRW.CurrentKey != "Jump_Idle")
                    {
                        if (animComponent.ValueRW.StartKey != "Jump_Idle")
                        {
                            animComponent.ValueRW.StartKey = "Jump_Idle";
                            animComponent.ValueRW.IsLoop = false;
                        }
                    }
                }
            }
        }
    }
}