
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct SpriteSheetAnimationComponent : IComponentData
{
    public FixedString64Bytes CurrentKey;
    public int CurrentSpriteSheetIndex;
    public int CurrentAnimationIndex;
    public int CurrentAnimationCount;
    public int PassingFrame;
    public bool IsLoop;

    public SpriteSheetAnimationComponent(bool isLoop)
    {
        IsLoop = isLoop;
        CurrentAnimationIndex = -1;
        CurrentAnimationCount = 0;
        PassingFrame = 0;
        CurrentKey = string.Empty;
        CurrentSpriteSheetIndex = 0;
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