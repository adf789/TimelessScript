
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PhysicsCollisionJob : IJobEntity
{
    public void Execute(
        ref LightweightPhysicsComponent physics,
        ref LocalTransform transform,
        in CollisionInfoComponent collisionInfo,
        in LightweightColliderComponent collider,
        [ReadOnly] DynamicBuffer<CollisionBuffer> collisions)
    {
        if (physics.isStatic)
            return;

        if (collider.isTrigger)
            return;

        // 모든 충돌 처리
        for (int i = 0; i < collisions.Length; i++)
        {
            var collision = collisions[i];
            if (collision.isTrigger)
                continue;
                
            // 위치 보정
            float2 currentPos = transform.Position.xy;
            currentPos += collision.separationVector;
            transform.Position = new float3(currentPos.x, currentPos.y, transform.Position.z);
            
            // 속도 반응 (간단한 반발)
            float2 normal = math.normalize(collision.separationVector);
            if (math.abs(normal.y) > math.abs(normal.x))
            {
                physics.velocity.y = -physics.velocity.y * 0.5f;
            }
            else
            {
                physics.velocity.x = -physics.velocity.x * 0.5f;
            }
        }
        
        // 레이캐스트 기반 지면 감지 (안정적)
        CheckGroundWithRaycast(ref physics, in transform);
    }
    
    [BurstCompile]
    private static void CheckGroundWithRaycast(ref LightweightPhysicsComponent physics, in LocalTransform transform)
    {
        // 캐릭터 아래쪽으로 짧은 레이캐스트
        float2 rayStart = transform.Position.xy;
        float2 rayEnd = rayStart + new float2(0, -0.1f); // 0.1 유닛 아래로
        
        // 간단한 지면 감지 - 속도가 거의 0이고 아래쪽으로 움직이지 않으면 grounded
        bool isMovingDown = physics.velocity.y < -float.Epsilon;
        bool hasLowVerticalVelocity = math.abs(physics.velocity.y) < 0.05f;
        
        if (hasLowVerticalVelocity && !isMovingDown)
        {
            physics.isGrounded = true;
        }
        else if (isMovingDown && math.abs(physics.velocity.y) > 0.2f)
        {
            physics.isGrounded = false;
        }
        // 중간 상태에서는 이전 값 유지
    }
}