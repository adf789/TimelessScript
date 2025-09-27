
using Unity.Entities;
using Unity.Mathematics;

public struct PhysicsEventsComponent : IComponentData
{
    public bool HasTriggerEnter;
    public bool HasTriggerStay;
    public bool HasTriggerExit;
    public Entity TriggerEntity;
}