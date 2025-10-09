
using Unity.Entities;

public struct TSActorComponent : IComponentData
{
    public float LifePassingTime;
    public MoveAction Move;
    public RestoreMoveAction RestoreMove;
}