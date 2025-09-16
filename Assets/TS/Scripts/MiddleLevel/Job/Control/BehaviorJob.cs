
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct BehaviorJob : IJobEntity
{
    [NativeDisableParallelForRestriction]
    public ComponentLookup<SpriteSheetAnimationComponent> animationComponentLookup;

    public void Execute(Entity entity,
    ref BehaviorComponent behaviorComponent,
    ref LightweightPhysicsComponent physicsComponent,
    [ReadOnly] DynamicBuffer<Child> children)
    {
        bool isGrounded = physicsComponent.isGrounded;

        // 2. 자식 목록을 순회하여 원하는 컴포넌트를 가진 자식을 찾습니다.
        foreach (var child in children)
        {
            var childEntity = child.Value;

            // 3. 자식 엔티티가 SpriteSheetAnimationComponent와 Authoring을 모두 가지고 있는지 확인합니다.
            if (animationComponentLookup.HasComponent(child.Value))
            {
                // 4. 자식의 컴포넌트들을 가져옵니다.
                var animComponent = animationComponentLookup.GetRefRW(child.Value);

                if (isGrounded && animComponent.ValueRW.CurrentKey == "Jump_Idle")
                {
                    if (animComponent.ValueRW.StartKey != "Jump_Land")
                    {
                        animComponent.ValueRW.StartKey = "Jump_Land";
                        animComponent.ValueRW.IsLoop = false;
                    }
                }
                else if (!isGrounded && animComponent.ValueRW.CurrentKey != "Jump_Idle")
                {
                    if (animComponent.ValueRW.StartKey != "Jump_Idle")
                    {
                        animComponent.ValueRW.StartKey = "Jump_Idle";
                    }
                }
            }
        }
    }
}