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
    Jump_Idle,
    Jump_Land,
    PickUp,
    Ladder_ClimbDownStart,
    Ladder_ClimbDownIdle,
    Ladder_ClimbUpStart,
    Ladder_ClimbUpIdle,
}

public enum MoveState
{
    None,
    Move,
    ClimbUp,
    ClimbDown,
    Interact
}