
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SpriteSheetAnimationComponent : IComponentData
{
    public AnimationState StartState;
    public AnimationState CurrentState;
    public int CurrentSpriteIndex;
    public int CurrentAnimationIndex;
    public int CurrentAnimationCount;
    public int PassingFrame;
    public bool IsLoop;
    public bool IsFlip;

    public bool IsLastAnimation => CurrentAnimationIndex == CurrentAnimationCount - 1;

    public SpriteSheetAnimationComponent(AnimationState startState, bool isLoop)
    {
        IsLoop = isLoop;
        IsFlip = false;
        CurrentSpriteIndex = 0;
        CurrentAnimationIndex = -1;
        CurrentAnimationCount = 0;
        PassingFrame = 0;
        StartState = startState;
        CurrentState = AnimationState.Idle;
    }

    public int NextAnimationIndex()
    {
        CurrentAnimationIndex++;

        if (CurrentAnimationIndex >= CurrentAnimationCount)
        {
            if (IsLoop)
                CurrentAnimationIndex = 0;
            else
                CurrentAnimationIndex = CurrentAnimationCount - 1;
        }

        return CurrentAnimationIndex;
    }
}