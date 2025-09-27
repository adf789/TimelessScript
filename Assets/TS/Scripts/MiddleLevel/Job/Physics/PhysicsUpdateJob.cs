
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PhysicsUpdateJob : IJobEntity
{
    [ReadOnly] public float deltaTime;
    
    public void Execute(
        ref PhysicsComponent physics,
        ref LocalTransform transform)
    {
        if (physics.IsStatic)
            return;
            
        float2 previousPosition = transform.Position.xy;
        
        // 중력 적용
        if (physics.UseGravity && !physics.IsGrounded)
        {
            physics.Velocity += physics.Gravity * deltaTime;
        }
        
        // 드래그 적용
        physics.Velocity *= physics.Drag;
        
        // 위치 업데이트
        float2 newPosition = previousPosition + physics.Velocity * deltaTime;
        transform.Position = new float3(newPosition.x, newPosition.y, transform.Position.z);
    }
}