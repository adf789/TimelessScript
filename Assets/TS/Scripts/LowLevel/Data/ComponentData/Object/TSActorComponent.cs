
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct TSActorComponent : IComponentData
{
    public float LifeTime;
    public float LifePassingTime;
    public TSObjectBehavior Behavior;
}