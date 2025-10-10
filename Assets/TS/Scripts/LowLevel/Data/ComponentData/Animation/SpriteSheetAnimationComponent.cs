
using Unity.Entities;

public struct SpriteSheetAnimationComponent : IComponentData
{
    public AnimationState CurrentState;
    public AnimationState NextState;
    public AnimationPhase PrevPhase;
    public AnimationPhase CurrentPhase;
    public int CurrentSpriteIndex;
    public int CurrentAnimationIndex;
    public int CurrentAnimationCount;
    public float PassingTime;
    public bool IsTransitioning;
    public bool IsEndLoopOneTime;
    public bool HasStartAnimation;
    public bool HasEndAnimation;
    public bool ShouldTransitionToEnd;
    public AnimationTransitionType TransitionType;

    // 애니메이션 플래그
    private AnimationFlag startFlag;
    private AnimationFlag completeFlag;
    private AnimationFlag endFlag;

    public bool IsLastAnimation => CurrentAnimationIndex == CurrentAnimationCount - 1;

    public SpriteSheetAnimationComponent(AnimationState currentState)
    {
        IsTransitioning = false;
        CurrentSpriteIndex = 0;
        CurrentAnimationIndex = -1;
        CurrentAnimationCount = 0;
        PassingTime = 0f;
        CurrentState = currentState;
        NextState = AnimationState.None;
        PrevPhase = AnimationPhase.Start;
        CurrentPhase = AnimationPhase.Loop;
        HasStartAnimation = false;
        HasEndAnimation = false;
        ShouldTransitionToEnd = false;
        IsEndLoopOneTime = false;
        TransitionType = AnimationTransitionType.None;

        startFlag = new AnimationFlag();
        completeFlag = new AnimationFlag();
        endFlag = new AnimationFlag();
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

    public void SetFlag(AnimationState state, AnimationFlagType flagType, bool isOn)
    {
        switch (flagType)
        {
            case AnimationFlagType.Start:
                {
                    startFlag.State = state;
                    startFlag.IsOn = isOn;
                }
                break;

            case AnimationFlagType.Complete:
                {
                    completeFlag.State = state;
                    completeFlag.IsOn = isOn;
                }
                break;

            case AnimationFlagType.End:
                {
                    endFlag.State = state;
                    endFlag.IsOn = isOn;
                }
                break;
        }
    }

    public void SetFlagReset(AnimationFlagType flagType)
    {
        switch (flagType)
        {
            case AnimationFlagType.Start:
                {
                    startFlag.State = AnimationState.None;
                    startFlag.IsOn = false;
                }
                break;

            case AnimationFlagType.Complete:
                {
                    completeFlag.State = AnimationState.None;
                    completeFlag.IsOn = false;
                }
                break;

            case AnimationFlagType.End:
                {
                    endFlag.State = AnimationState.None;
                    endFlag.IsOn = false;
                }
                break;
        }
    }

    public AnimationFlag GetFlag(AnimationFlagType flagType)
    {
        return flagType switch
        {
            AnimationFlagType.Start => startFlag,
            AnimationFlagType.Complete => completeFlag,
            AnimationFlagType.End => endFlag,
            _ => default,
        };
    }
}