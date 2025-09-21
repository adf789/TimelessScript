using Unity.Entities;
using Unity.Mathematics;
using System.Runtime.InteropServices;

public struct PhysicsComponent : IComponentData
{
    public Entity entity;
    public float2 velocity;
    public float2 gravity;
    public float mass;
    public float drag;
    [MarshalAs(UnmanagedType.U1)]
    public bool useGravity;
    [MarshalAs(UnmanagedType.U1)]
    public bool isGrounded;
    [MarshalAs(UnmanagedType.U1)]
    public bool isPrevGrounded;
    [MarshalAs(UnmanagedType.U1)]
    public bool isStatic;
    
}