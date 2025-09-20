
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

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
    public bool HasStartAnimation;
    public bool HasEndAnimation;
    public bool ShouldTransitionToEnd;
    public bool SkipStartAnimation;

    public bool IsLastAnimation => CurrentAnimationIndex == CurrentAnimationCount - 1;

    public SpriteSheetAnimationComponent(AnimationState currentState, bool isLoop)
    {
        IsFlip = false;
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
        SkipStartAnimation = false;
    }

    public int NextAnimationIndex()
    {
        CurrentAnimationIndex++;

        if (CurrentAnimationIndex >= CurrentAnimationCount)
        {
            if (CurrentPhase == AnimationPhase.Loop)
                CurrentAnimationIndex = 0;
            else
                CurrentAnimationIndex = CurrentAnimationCount - 1;
        }

        return CurrentAnimationIndex;
    }

    public void RequestTransition(AnimationState nextState, bool skipStart = false)
    {
        if (CurrentState == nextState)
            return;
            
        NextState = nextState;
        SkipStartAnimation = skipStart;
        ShouldTransitionToEnd = true;
    }

    public bool ShouldEndCurrentAnimation()
    {
        if (CurrentPhase == AnimationPhase.Loop)
            return ShouldTransitionToEnd;
        else
            return IsLastAnimation;
    }
}