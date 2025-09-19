
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
        foreach (var (authoring, component) in
        SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<SpriteSheetAnimationAuthoring>,
        RefRW<SpriteSheetAnimationComponent>>())
        {
            if (!authoring.Value.IsLoaded)
            {
                authoring.Value.Initialize();
                authoring.Value.LoadAnimations();
                continue;
            }

            SetAnimation(authoring.Value, ref component.ValueRW);
        }
    }

    private bool CheckAnimationFrame(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        if (component.PassingFrame < authoring.GetFrameDelay(component.CurrentSpriteIndex))
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
    public void SetAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        authoring.SetFlip(component.IsFlip);

        if (component.StartState != AnimationState.None)
        {
            authoring.TryGetSpriteNode(component.StartState, out var findNode, out int findIndex);

            ApplyComponent(ref component, findNode, findIndex, component.IsLoop);
        }
        else if (!component.IsLoop && component.IsLastAnimation)
        {
            var defaultNode = authoring.GetDefaultSpriteNode(out int index);

            ApplyComponent(ref component, defaultNode, index, true);
        }
        else if (!CheckAnimationFrame(authoring, ref component))
            return;

        authoring.SetAnimationByIndex(component.CurrentState, component.NextAnimationIndex());
    }

    private void ApplyComponent(ref SpriteSheetAnimationComponent component, SpriteSheetAnimationAuthoring.Node node, int index, bool isLoop)
    {
        component.CurrentState = node.State;
        component.CurrentAnimationCount = node.SpriteCount;
        component.CurrentSpriteIndex = index;
        component.CurrentAnimationIndex = -1;
        component.StartState = AnimationState.None;
        component.IsLoop = isLoop;
    }
}