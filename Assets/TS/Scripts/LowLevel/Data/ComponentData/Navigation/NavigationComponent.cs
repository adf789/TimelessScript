using Unity.Entities;
using Unity.Mathematics;

public struct NavigationComponent : IComponentData
{
    public bool IsActive;
    public float2 FinalTargetPosition;
    public Entity FinalTargetGround;
    public int CurrentWaypointIndex;
    public NavigationState State;
}

public enum NavigationState
{
    None,
    PathFinding,
    MovingToWaypoint,
    ClimbingLadder,
    Dropping,
    Completed,
    Failed
}