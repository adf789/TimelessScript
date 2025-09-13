
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

// BehaviorComponent가 아직 없다면, 아래와 같이 정의해야 합니다.
// public struct BehaviorComponent : IComponentData { /* 필요한 데이터 */ }

public partial struct BehaviorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // BehaviorComponent를 가진 부모 엔티티를 찾습니다.
        foreach (var (behavior, parentEntity) 
                 in SystemAPI.Query<RefRO<BehaviorComponent>>().WithEntityAccess())
        {
            // 1. 부모 엔티티의 자식 목록(DynamicBuffer<Child>)을 가져옵니다.
            if (!SystemAPI.HasBuffer<Child>(parentEntity))
            {
                continue; // 자식이 없으면 다음 부모로 넘어갑니다.
            }
            DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(parentEntity);

            // 2. 자식 목록을 순회하여 원하는 컴포넌트를 가진 자식을 찾습니다.
            foreach (var child in children)
            {
                var childEntity = child.Value;

                // 3. 자식 엔티티가 SpriteSheetAnimationComponent와 Authoring을 모두 가지고 있는지 확인합니다.
                if (SystemAPI.HasComponent<SpriteSheetAnimationComponent>(childEntity))
                {
                    // 4. 자식의 컴포넌트들을 가져옵니다.
                    var animComponent = SystemAPI.GetComponent<SpriteSheetAnimationComponent>(childEntity);
                    
                    // --- 여기에 로직을 추가하세요 ---
                    // 예시: 특정 조건일 때 애니메이션을 변경하는 로직
                    // if (behavior.ValueRO.SomeState == DesiredState)
                    // {
                    //     var animationSystem = state.World.GetExistingSystemManaged<SpriteSheetAnimationSystem>();
                    //     if(animationSystem != null)
                    //     {
                    //         animationSystem.SetAnimation(animAuthoring, ref animComponent, new FixedString64Bytes("run"));
                    //         SystemAPI.SetComponent(childEntity, animComponent); // 변경된 컴포넌트 저장
                    //     }
                    // }
                    
                    // 원하는 자식을 찾았으므로, 더 이상 다른 자식을 순회할 필요가 없다면 루프를 종료합니다.
                    break;
                }
            }
        }
    }
}