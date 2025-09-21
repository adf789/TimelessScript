
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum AnimationPhase
{
    Start,
    Loop,
    End
}

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
    public bool HasStartAnimation;
    public bool HasEndAnimation;
    public bool ShouldTransitionToEnd;
    public bool ShouldTransitionToEndOneTime;
    public AnimationTransitionType TransitionType;

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
        ShouldTransitionToEndOneTime = false;
        TransitionType = AnimationTransitionType.None;
    }

    public int NextAnimationIndex()
    {
        CurrentAnimationIndex++;

        if (CurrentAnimationIndex >= CurrentAnimationCount)
        {
            if (CurrentPhase == AnimationPhase.Loop
            && !ShouldTransitionToEndOneTime)
                CurrentAnimationIndex = 0;
            else
                CurrentAnimationIndex = CurrentAnimationCount - 1;
        }

        return CurrentAnimationIndex;
    }

    public void RequestTransition(AnimationState nextState, AnimationTransitionType transitionType = AnimationTransitionType.None)
    {
        if (CurrentState == nextState)
        {
            if (NextState != nextState)
                NextState = AnimationState.None;

            return;
        }

        Debug.Log($"Request Animation: {nextState.ToFixedString()}\n{StackTraceUtility.ExtractStackTrace()}");

        NextState = nextState;
        TransitionType = transitionType;
        ShouldTransitionToEndOneTime = TransitionType == AnimationTransitionType.None;
        ShouldTransitionToEnd = true;
        IsTransitioning = false;
    }

    public bool ShouldEndCurrentAnimation()
    {
        if (CurrentPhase == AnimationPhase.Loop)
        {
            if (!ShouldTransitionToEndOneTime)
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