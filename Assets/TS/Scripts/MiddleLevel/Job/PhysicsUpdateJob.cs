
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
        ref LightweightPhysicsComponent physics,
        ref LocalTransform transform)
    {
        if (physics.isStatic)
            return;
            
        float2 previousPosition = transform.Position.xy;
        
        // 중력 적용
        if (physics.useGravity && !physics.isGrounded)
        {
            physics.velocity += physics.gravity * deltaTime;
        }
        
        // 드래그 적용
        physics.velocity *= physics.drag;
        
        // 위치 업데이트
        float2 newPosition = previousPosition + physics.velocity * deltaTime;
        transform.Position = new float3(newPosition.x, newPosition.y, transform.Position.z);
    }
}