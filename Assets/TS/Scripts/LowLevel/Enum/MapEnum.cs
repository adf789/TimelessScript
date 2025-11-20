public enum FourDirection
{
    Up,
    Down,
    Left,
    Right,
}

public enum MapLoadState
{
    None,           // 로드 전 상태
    OnlyPhysics,    // 충돌체만 로드 상태
    All             // 모두 로드된 상태
}