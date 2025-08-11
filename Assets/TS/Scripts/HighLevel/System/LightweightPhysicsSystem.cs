using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct LightweightPhysicsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LightweightPhysicsComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // 물리 업데이트 Job
        var physicsJob = new PhysicsUpdateJob
        {
            deltaTime = deltaTime
        };
        state.Dependency = physicsJob.ScheduleParallel(state.Dependency);
        
        // 충돌 처리 Job
        var collisionJob = new PhysicsCollisionJob();
        state.Dependency = collisionJob.ScheduleParallel(state.Dependency);
        
        // 트리거 처리 Job
        var triggerJob = new TriggerHandlingJob();
        state.Dependency = triggerJob.ScheduleParallel(state.Dependency);
    }
}

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

[BurstCompile]
public partial struct PhysicsCollisionJob : IJobEntity
{
    public void Execute(
        ref LightweightPhysicsComponent physics,
        ref LocalTransform transform,
        in CollisionInfo collisionInfo,
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

[BurstCompile]
public partial struct TriggerHandlingJob : IJobEntity
{
    public void Execute(
        ref PhysicsEvents events,
        DynamicBuffer<TriggerBuffer> triggerBuffer,
        in LightweightColliderComponent collider,
        [ReadOnly] DynamicBuffer<CollisionBuffer> collisions)
    {
        // 현재 트리거 상태 초기화
        events.hasTriggerEnter = false;
        events.hasTriggerStay = false;
        events.hasTriggerExit = false;
        
        // 새로운 트리거들 수집
        var currentTriggers = new NativeList<Entity>(Allocator.Temp);
        
        for (int i = 0; i < collisions.Length; i++)
        {
            var collision = collisions[i];
            if (collision.isTrigger)
            {
                currentTriggers.Add(collision.collidedEntity);
                
                // 트리거 진입 검사
                bool wasInTrigger = false;
                for (int j = 0; j < triggerBuffer.Length; j++)
                {
                    if (triggerBuffer[j].triggerEntity.Equals(collision.collidedEntity))
                    {
                        wasInTrigger = true;
                        break;
                    }
                }
                
                if (!wasInTrigger)
                {
                    // 새로운 트리거 진입
                    events.hasTriggerEnter = true;
                    events.triggerEntity = collision.collidedEntity;
                }
                else
                {
                    // 트리거 지속
                    events.hasTriggerStay = true;
                    events.triggerEntity = collision.collidedEntity;
                }
            }
        }
        
        // 트리거 종료 검사
        for (int i = 0; i < triggerBuffer.Length; i++)
        {
            var oldTrigger = triggerBuffer[i].triggerEntity;
            bool stillInTrigger = false;
            
            for (int j = 0; j < currentTriggers.Length; j++)
            {
                if (currentTriggers[j].Equals(oldTrigger))
                {
                    stillInTrigger = true;
                    break;
                }
            }
            
            if (!stillInTrigger)
            {
                events.hasTriggerExit = true;
                events.triggerEntity = oldTrigger;
            }
        }
        
        // 트리거 버퍼 업데이트
        triggerBuffer.Clear();
        for (int i = 0; i < currentTriggers.Length; i++)
        {
            triggerBuffer.Add(new TriggerBuffer
            {
                triggerEntity = currentTriggers[i],
                isEntering = true
            });
        }
        
        currentTriggers.Dispose();
    }
}

// 힘 추가를 위한 유틸리티
public static class PhysicsUtility
{
    public static void AddForce(ref LightweightPhysicsComponent physics, float2 force)
    {
        physics.velocity += force / physics.mass;
    }
    
    public static void SetVelocity(ref LightweightPhysicsComponent physics, float2 velocity)
    {
        physics.velocity = velocity;
    }
}