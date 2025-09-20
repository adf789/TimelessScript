
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct TriggerHandlingJob : IJobEntity
{
    public void Execute(
        ref PhysicsEventsComponent events,
        DynamicBuffer<TriggerBuffer> triggerBuffer,
        in ColliderComponent collider,
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