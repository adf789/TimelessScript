
using Unity.Burst;
using Unity.Collections;
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
        Entities
            .WithoutBurst()
            .ForEach((SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component) =>
            {
                if (!authoring.IsLoaded)
                {
                    authoring.Initialize();
                    authoring.LoadAnimations();
                    return;
                }

                if (!CheckAnimationFrame(authoring, ref component))
                    return;

                authoring.SetAnimationByIndex(component.CurrentKey, component.NextAnimationIndex());
            }).Run();
    }

    private bool CheckAnimationFrame(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        if (component.PassingFrame < authoring.GetFrameDelay(component.CurrentAnimationIndex))
        {
            component.PassingFrame++;
            return false;
        }
        else
        {
            component.PassingFrame = 0;
            return true;
        }
    }

/// <summary>
/// 테스트용
/// </summary>
/// <param name="authoring"></param>
/// <param name="component"></param>
/// <param name="key"></param>
    public void SetAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component, FixedString64Bytes key)
    {
        if (authoring.TryGetSpriteSheetIndex(key, out var index, out var defaultKey))
        {
            component.CurrentKey = key;
            component.CurrentAnimationCount = authoring.GetSpriteSheetCount(key);
        }
        else
        {
            component.CurrentKey = defaultKey;
            component.CurrentAnimationCount = authoring.GetSpriteSheetCount(defaultKey);
        }

        component.CurrentSpriteSheetIndex = index;
        component.CurrentAnimationIndex = -1;

        authoring.SetAnimationByIndex(component.CurrentKey, component.NextAnimationIndex());
    }
}