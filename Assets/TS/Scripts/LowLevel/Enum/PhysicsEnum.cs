using Unity.Collections;

public enum GroundType
{
    Normal,
    Bouncy,
    Slippery,
    Sticky
}

public enum ColliderLayer
{
    None,
    Actor,
    Ground,
    Ladder,
    Gimmick,
}

public static partial class ToString
{
    public static FixedString64Bytes ToFixedString(this ColliderLayer state)
    {
        return state switch
        {
            ColliderLayer.None => "None",
            ColliderLayer.Actor => "Actor",
            ColliderLayer.Ground => "Ground",
            ColliderLayer.Ladder => "Ladder",
            ColliderLayer.Gimmick => "Gimmick",
            _ => "",
        };
    }
}