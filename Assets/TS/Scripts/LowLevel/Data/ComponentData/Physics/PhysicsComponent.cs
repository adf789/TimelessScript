using Unity.Entities;
using Unity.Mathematics;
using System.Runtime.InteropServices;

public struct PhysicsComponent : IComponentData
{
    public Entity Entity;
    public float2 Velocity;
    public float2 Gravity;
    public float Mass;
    public float Drag;
    [MarshalAs(UnmanagedType.U1)]
    public bool UseGravity;
    [MarshalAs(UnmanagedType.U1)]
    public bool IsGrounded;
    [MarshalAs(UnmanagedType.U1)]
    public bool IsPrevGrounded;
    [MarshalAs(UnmanagedType.U1)]
    public bool IsRandingAnimation;
    [MarshalAs(UnmanagedType.U1)]
    public bool IsStatic;
    
}
