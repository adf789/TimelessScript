
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SpriteSheetAnimationComponent : IComponentData
{
    public FixedString64Bytes StartKey;
    public FixedString64Bytes CurrentKey;
    public int CurrentSpriteIndex;
    public int CurrentAnimationIndex;
    public int CurrentAnimationCount;
    public int PassingFrame;
    public bool IsLoop;

    public bool IsLastAnimation => CurrentAnimationIndex == CurrentAnimationCount - 1;

    public SpriteSheetAnimationComponent(FixedString64Bytes startKey, bool isLoop)
    {
        IsLoop = isLoop;
        CurrentSpriteIndex = 0;
        CurrentAnimationIndex = -1;
        CurrentAnimationCount = 0;
        PassingFrame = 0;
        StartKey = startKey;
        CurrentKey = string.Empty;
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