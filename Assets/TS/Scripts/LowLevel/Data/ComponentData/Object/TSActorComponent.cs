
using Unity.Entities;

public struct TSActorComponent : IComponentData
{
    public float LifeTime;
    public float LifePassingTime;
    public MoveAction Move;
    // public ActionStack ActionStack;
}