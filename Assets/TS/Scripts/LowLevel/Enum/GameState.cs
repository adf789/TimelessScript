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
}

public enum MoveState
{
    None,
    Move,
    ClimbUp,
    ClimbDown,
    Interact
}