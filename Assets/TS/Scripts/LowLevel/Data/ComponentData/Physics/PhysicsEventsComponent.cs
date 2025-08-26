
using Unity.Entities;
using Unity.Mathematics;

public struct PhysicsEventsComponent : IComponentData
{
    public bool hasTriggerEnter;
    public bool hasTriggerStay;
    public bool hasTriggerExit;
    public Entity triggerEntity;
}