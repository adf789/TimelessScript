public enum GameState
{
    Intro,
    Loading,
    Home
}

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

public enum MoveState
{
    None,
    Move,
    ClimbUp,
    ClimbDown,
    Interact
}