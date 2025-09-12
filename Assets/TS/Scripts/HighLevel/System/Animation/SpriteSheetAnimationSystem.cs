
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SpriteSheetAnimationSystem : SystemBase
{
    protected override void OnCreate()
    {
        // 이 시스템은 SpriteSheetAnimationComponent가 있는 엔티티가 하나라도 있을 때만 업데이트됩니다.
        RequireForUpdate<SpriteSheetAnimationComponent>();
    }

    protected override void OnUpdate()
    {
        // 관리형 컴포넌트(MonoBehaviour)를 다룰 때는 Entities.ForEach를 사용하는 것이 가장 확실합니다.
        // WithoutBurst()를 통해 이 코드가 메인 스레드에서 실행되어야 함을 명시합니다.
        Entities
            .WithoutBurst()
            .ForEach((SpriteSheetAnimationAuthoring authoringComponent, ref SpriteSheetAnimationComponent animComponent) =>
            {
                // 이제 authoringComponent 변수를 통해 SpriteSheetAnimationAuthoring의
                // public 메서드나 프로퍼티에 직접 접근할 수 있습니다.
                // 예시: authoringComponent.SetAnimation("idle", true);
                // 예시: authoringComponent.SetFlip(true, false);
                Debug.LogError("Test: " + authoringComponent.name);
            }).Run();
    }
}