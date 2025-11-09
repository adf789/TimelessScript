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

    public static PhysicsComponent GetStaticPhysic(Entity entity)
    {
        return new PhysicsComponent()
        {
            Entity = entity,
            Velocity = float2.zero,
            Gravity = float2.zero,
            Mass = 0,
            Drag = 0,
            UseGravity = false,
            IsPrevGrounded = false,
            IsRandingAnimation = false,
            IsGrounded = false,
            IsStatic = true
        };
    }
}
