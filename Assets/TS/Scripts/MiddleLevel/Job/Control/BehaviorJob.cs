
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct BehaviorJob : IJobEntity
{
    [NativeDisableParallelForRestriction]
    public ComponentLookup<SpriteSheetAnimationComponent> AnimationComponentLookup;
    public float Speed;
    public float DeltaTime;

    public void Execute(Entity entity,
    ref LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref LightweightPhysicsComponent physicsComponent,
    [ReadOnly] DynamicBuffer<Child> children)
    {
        bool isGrounded = physicsComponent.isGrounded;
        RefRW<SpriteSheetAnimationComponent> animComponent = default;

        // 2. 자식 목록을 순회하여 원하는 컴포넌트를 가진 자식을 찾습니다.
        foreach (var child in children)
        {
            var childEntity = child.Value;

            // 3. 자식 엔티티가 SpriteSheetAnimationComponent와 Authoring을 모두 가지고 있는지 확인합니다.
            if (AnimationComponentLookup.HasComponent(child.Value))
            {
                // 4. 자식의 컴포넌트들을 가져옵니다.
                animComponent = AnimationComponentLookup.GetRefRW(child.Value);
                break;
            }
        }

        if (!animComponent.IsValid)
            return;

        if (objectComponent.Behavior.Purpose == MoveState.Move)
        {
            if (animComponent.ValueRW.CurrentKey != "Walking" && animComponent.ValueRW.StartKey != "Walking")
            {
                animComponent.ValueRW.StartKey = "Walking";
            }

            // 1. 현재 '루트(발)'의 위치를 계산합니다. (피벗 위치 + 오프셋)
            float2 currentRootPosition = transform.Position.xy;
            currentRootPosition.y += objectComponent.RootOffset;

            // 2. 목표 '루트' 위치를 가져옵니다.
            float2 targetRootPosition = objectComponent.Behavior.MovePosition;

            if (animComponent.ValueRW.IsFlip = targetRootPosition.x < currentRootPosition.x);

            // 3. 한 프레임에 이동할 최대 거리를 계산합니다.
            float maxDistanceDelta = Speed * DeltaTime;

            // 4. MoveTowards를 사용해 다음 '루트' 위치를 계산합니다.
            float2 newRootPosition = Utility.Geometry.MoveTowards(currentRootPosition, targetRootPosition, maxDistanceDelta);

            // 5. 새로 계산된 '루트' 위치를 엔티티의 '피벗' 위치로 다시 변환합니다. (루트 위치 - 오프셋)
            float2 newTransformPosition = newRootPosition;
            newTransformPosition.y -= objectComponent.RootOffset;

            // 6. 최종 '피벗' 위치를 LocalTransform에 적용합니다. Z축 값은 유지합니다.
            transform.Position = new float3(newTransformPosition.x, newTransformPosition.y, transform.Position.z);
            
            // 도착지 위치 비교
            currentRootPosition = transform.Position.xy;
            currentRootPosition.y += objectComponent.RootOffset;

            if (Utility.Geometry.CheckEqualsPosition(currentRootPosition, targetRootPosition))
            {
                animComponent.ValueRW.StartKey = "Idle";
                objectComponent.Behavior.Purpose = MoveState.None;
            }
        }
        else if (isGrounded && animComponent.ValueRW.CurrentKey == "Jump_Idle")
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