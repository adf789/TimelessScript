
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
        int frameDelay = component.CurrentPhase switch
        {
            AnimationPhase.Start => authoring.GetStartFrameDelay(component.CurrentSpriteIndex, component.CurrentAnimationIndex),
            AnimationPhase.Loop => authoring.GetFrameDelay(component.CurrentSpriteIndex, component.CurrentAnimationIndex),
            AnimationPhase.End => authoring.GetEndFrameDelay(component.CurrentSpriteIndex, component.CurrentAnimationIndex),
            _ => authoring.GetFrameDelay(component.CurrentSpriteIndex, component.CurrentAnimationIndex)
        };

        if (component.PassingFrame < frameDelay)
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

    public void SetAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        // 현재 애니메이션 진행
        if (!CheckAnimationFrame(authoring, ref component))
            return;

        authoring.SetFlip(component.IsFlip);

        // 애니메이션 전환 요청 처리
        if (component.NextState != AnimationState.None)
        {
            if (component.NextState != component.CurrentState)
                ProcessAnimationTransition(authoring, ref component);
            else
                component.NextState = AnimationState.None;
        }

        ProcessCurrentAnimation(authoring, ref component);
    }

    private void InitializeNewAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component, AnimationState targetState)
    {
        if (!authoring.TryGetSpriteNode(targetState, out var node, out int nodeIndex))
        {
            Debug.LogWarning($"Animation state {targetState} not found, using default");
            node = authoring.GetDefaultSpriteNode(out nodeIndex);
        }

        component.CurrentState = targetState;
        component.CurrentSpriteIndex = nodeIndex;
        component.NextState = AnimationState.None;
        component.ShouldTransitionToEnd = false;
        component.SkipStartAnimation = false;
        component.HasStartAnimation = authoring.HasStartAnimation(targetState);
        component.HasEndAnimation = authoring.HasEndAnimation(targetState);

        if (!component.HasStartAnimation)
        {
            SetupPhaseAnimation(AnimationPhase.Loop, authoring, ref component);
        }
        else
        {
            SetupPhaseAnimation(AnimationPhase.Start, authoring, ref component);
        }
    }

    private void ProcessAnimationTransition(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        // 현재 애니메이션의 End Phase로 전환
        if (!component.SkipStartAnimation
        && component.HasEndAnimation)
        {
            SetupPhaseAnimation(AnimationPhase.End, authoring, ref component);
        }
        else
        {
            // End 애니메이션이 없거나 이미 End Phase라면 바로 다음 애니메이션으로
            StartNextAnimation(authoring, ref component);
        }
    }

    private void ProcessCurrentAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        int nextIndex = component.NextAnimationIndex();
        bool isPhaseComplete = false;

        // 현재 Phase의 애니메이션이 끝났는지 확인
        switch (component.CurrentPhase)
        {
            case AnimationPhase.Start:
                {
                    authoring.SetStartAnimationByIndex(component.CurrentState, nextIndex);

                    if (component.ShouldEndCurrentAnimation())
                        SetupPhaseAnimation(AnimationPhase.Loop, authoring, ref component);
                }
                break;

            case AnimationPhase.Loop:
                {
                    if (!component.ShouldEndCurrentAnimation())
                    {
                        authoring.SetAnimationByIndex(component.CurrentState, nextIndex);
                        return;
                    }

                    // 전환 요청이 있고 마지막 프레임이라면 End Phase로 이동
                    if (component.HasEndAnimation)
                        SetupPhaseAnimation(AnimationPhase.End, authoring, ref component);
                    else // End 애니메이션이 없으면 바로 다음 애니메이션으로
                        StartNextAnimation(authoring, ref component);
                }
                break;

            case AnimationPhase.End:
                {
                    authoring.SetEndAnimationByIndex(component.CurrentState, nextIndex);

                    if (component.ShouldEndCurrentAnimation())
                        StartNextAnimation(authoring, ref component);
                }
                break;
        }
    }

    /// <summary>
    /// 다음 애니메이션 상태로 설정함
    /// </summary>
    private void StartNextAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        AnimationState nextState = component.NextState;
        component.NextState = AnimationState.None;
        component.ShouldTransitionToEnd = false;

        InitializeNewAnimation(authoring, ref component, nextState);
    }

    private void SetupPhaseAnimation(AnimationPhase phase, SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        component.CurrentPhase = phase;
        component.CurrentAnimationIndex = -1;
        component.PassingFrame = 0;

        switch (phase)
        {
            case AnimationPhase.Start:
                component.CurrentAnimationCount = authoring.GetStartAnimationCount(component.CurrentState);
                break;
            case AnimationPhase.Loop:
                component.CurrentAnimationCount = authoring.GetSpriteSheetCount(component.CurrentState);
                break;
            case AnimationPhase.End:
                component.CurrentAnimationCount = authoring.GetEndAnimationCount(component.CurrentState);
                break;
        }
    }
}