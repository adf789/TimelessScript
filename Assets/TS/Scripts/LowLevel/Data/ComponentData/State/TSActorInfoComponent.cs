
using Unity.Entities;
using Unity.Mathematics;

public struct TSObjectInfoComponent : IComponentData
{
    public Entity Target;
    public BehaviorType Behavior;
    public MoveState State;

    public bool IsNull => Target == Entity.Null;
}