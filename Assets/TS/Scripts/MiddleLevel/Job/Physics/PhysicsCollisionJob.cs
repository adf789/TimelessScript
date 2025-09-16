
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

                if (collision.isGroundCollision)
                {
                    // 지상 충돌 시 처리. 예: 마찰력, 반발 계수 조정
                    physics.isGrounded = true;
                }
            }
            else
            {
                physics.velocity.x = -physics.velocity.x * 0.5f;
            }
        }
        
        // CheckGround(ref physics);
    }
    
    [BurstCompile]
    private static void CheckGround(ref LightweightPhysicsComponent physics)
    {
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