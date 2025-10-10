using Unity.Collections;

public enum AnimationState
{
    None = 0,
    Idle,
    Interact,
    Walking,
    Fall,
    Ladder_ClimbDown,
    Ladder_ClimbUp,
}

/// <summary>
/// 다음 애니메이션으로 트랜지션
/// </summary>
public enum AnimationTransitionType
{
    None,               // 정상진행
    SkipAllPhase,       // 다음 애니메이션으로 즉시 변경
    SkipCurrentPhase    // 현재 상태만 패스
}

public enum AnimationPhase
{
    Start,
    Loop,
    End
}

public enum AnimationFlagType
{
    Start = 0,
    Complete,
    End,

    Max
}

public static partial class ToString
{
    public static FixedString64Bytes ToFixedString(this AnimationState state)
    {
        return state switch
        {
            AnimationState.None => "None",
            AnimationState.Idle => "Idle",
            AnimationState.Interact => "Interact",
            AnimationState.Walking => "Walking",
            AnimationState.Fall => "Fall",
            AnimationState.Ladder_ClimbDown => "Ladder_ClimbDown",
            AnimationState.Ladder_ClimbUp => "Ladder_ClimbUp",
            _ => "",
        };
    }

    public static FixedString64Bytes ToFixedString(this AnimationTransitionType type)
    {
        return type switch
        {
            AnimationTransitionType.None => "None",
            AnimationTransitionType.SkipAllPhase => "SkipAllPhase",
            AnimationTransitionType.SkipCurrentPhase => "SkipCurrentPhase",
            _ => "",
        };
    }
}