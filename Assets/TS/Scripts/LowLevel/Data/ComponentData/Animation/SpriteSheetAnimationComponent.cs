
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpriteSheetAnimationComponent : IComponentData
{
    public AnimationState CurrentState;
    public AnimationState NextState;
    public AnimationPhase CurrentPhase;
    public int CurrentSpriteIndex;
    public int CurrentAnimationIndex;
    public int CurrentAnimationCount;
    public int PassingFrame;
    public bool IsFlip;
    public bool IsTransitioning;
    public bool IsEndLoopOneTime;
    public bool HasStartAnimation;
    public bool HasEndAnimation;
    public bool ShouldTransitionToEnd;
    public AnimationTransitionType TransitionType;
    public bool AnimationCompleted;
    public AnimationState CompletedAnimationState;

    public bool IsLastAnimation => CurrentAnimationIndex == CurrentAnimationCount - 1;

    public SpriteSheetAnimationComponent(AnimationState currentState)
    {
        IsFlip = false;
        IsTransitioning = false;
        CurrentSpriteIndex = 0;
        CurrentAnimationIndex = -1;
        CurrentAnimationCount = 0;
        PassingFrame = 0;
        CurrentState = currentState;
        NextState = AnimationState.None;
        CurrentPhase = AnimationPhase.Loop;
        HasStartAnimation = false;
        HasEndAnimation = false;
        ShouldTransitionToEnd = false;
        IsEndLoopOneTime = false;
        TransitionType = AnimationTransitionType.None;
        AnimationCompleted = false;
        CompletedAnimationState = AnimationState.None;
    }

    public int NextAnimationIndex()
    {
        CurrentAnimationIndex++;

        if (CurrentAnimationIndex >= CurrentAnimationCount)
        {
            if (CurrentPhase == AnimationPhase.Loop
            && !IsEndLoopOneTime)
                CurrentAnimationIndex = 0;
            else
                CurrentAnimationIndex = CurrentAnimationCount - 1;
        }

        return CurrentAnimationIndex;
    }

    public void RequestTransition(AnimationState nextState, AnimationTransitionType transitionType = AnimationTransitionType.None)
    {
        Debug.Log($"Request Animation Current: {CurrentState.ToFixedString()}, Next: {nextState.ToFixedString()}");

        // 현재 애니메이션과 다음 변경하려는 애니메이션이 같은 경우
        if (CurrentState == nextState)
        {
            // 이전에 변경하려는 애니메이션과 지금 변경하는 애니메이션이 다른 경우
            // 애니메이션 변경 취소
            if (NextState != nextState)
            {
                NextState = AnimationState.None;
                TransitionType = AnimationTransitionType.None;
                ShouldTransitionToEnd = false;
                IsEndLoopOneTime = false;
            }

            Debug.Log($"Request Animation Pass");

            return;
        }

        NextState = nextState;
        TransitionType = transitionType;
        IsEndLoopOneTime = TransitionType == AnimationTransitionType.None;
        ShouldTransitionToEnd = true;
        IsTransitioning = false;
    }

    public bool ShouldEndCurrentAnimation()
    {
        if (CurrentPhase == AnimationPhase.Loop)
        {
            if (!IsEndLoopOneTime)
                return ShouldTransitionToEnd;
            else
                return IsLastAnimation;
        }
        else
        {
            return IsLastAnimation;
        }
    }
}