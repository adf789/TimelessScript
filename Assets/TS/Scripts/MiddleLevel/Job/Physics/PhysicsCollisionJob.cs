
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PhysicsCollisionJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<TSObjectComponent> TSObjectLookup;
    [ReadOnly] public ComponentLookup<LightweightColliderComponent> ColliderLookup;

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

        // 현재 충돌 목록에서 사다리와 겹치고 있는지 확인
        bool isInLadderArea = IsCollidingWithLadder(collisions);

        // 모든 충돌 처리
        for (int i = 0; i < collisions.Length; i++)
        {
            var collision = collisions[i];
            if (collision.isTrigger)
                continue;

            // 사다리 영역에서의 Ground 충돌 처리
            if (isInLadderArea && collision.isGroundCollision)
            {
                // 사다리 영역에서는 Ground와의 충돌을 무시 (지면을 뚫고 지나갈 수 있음)
                #if UNITY_EDITOR
                UnityEngine.Debug.Log($"[PhysicsCollisionJob] 사다리 영역에서 Ground 충돌 무시");
                #endif
                continue;
            }

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
    private bool IsCollidingWithLadder(DynamicBuffer<CollisionBuffer> collisions)
    {
        // 충돌 목록에서 사다리와의 충돌(Trigger)이 있는지 확인
        for (int i = 0; i < collisions.Length; i++)
        {
            var collision = collisions[i];

            // 트리거 충돌이고 사다리인지 확인
            if (collision.isTrigger && TSObjectLookup.HasComponent(collision.collidedEntity))
            {
                var tsObject = TSObjectLookup[collision.collidedEntity];
                if (tsObject.ObjectType == TSObjectType.Ladder)
                {
                    #if UNITY_EDITOR
                    UnityEngine.Debug.Log($"[PhysicsCollisionJob] 사다리와 충돌 감지: {tsObject.Name}");
                    #endif
                    return true;
                }
            }
        }
        return false;
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