using Unity.Entities;
using Unity.Mathematics;

public struct LightweightPhysicsComponent : IComponentData
{
    public float2 velocity;
    public float2 gravity;
    public float mass;
    public float drag;
    public bool useGravity;
    public bool isGrounded;
    public bool isStatic;
}

public struct PhysicsEvents : IComponentData
{
    public bool hasTriggerEnter;
    public bool hasTriggerStay;
    public bool hasTriggerExit;
    public Entity triggerEntity;
}

public struct TriggerBuffer : IBufferElementData
{
    public Entity triggerEntity;
    public bool isEntering; // true면 Enter, false면 Exit
}