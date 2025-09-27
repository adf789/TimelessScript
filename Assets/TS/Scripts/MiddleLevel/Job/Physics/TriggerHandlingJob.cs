
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
        events.HasTriggerEnter = false;
        events.HasTriggerStay = false;
        events.HasTriggerExit = false;

        // 새로운 트리거들 수집
        var currentTriggers = new NativeList<Entity>(Allocator.Temp);

        for (int i = 0; i < collisions.Length; i++)
        {
            var collision = collisions[i];
            if (collision.IsTrigger)
            {
                currentTriggers.Add(collision.CollidedEntity);

                // 트리거 진입 검사
                bool wasInTrigger = false;
                for (int j = 0; j < triggerBuffer.Length; j++)
                {
                    if (triggerBuffer[j].TriggerEntity.Equals(collision.CollidedEntity))
                    {
                        wasInTrigger = true;
                        break;
                    }
                }

                if (!wasInTrigger)
                {
                    // 새로운 트리거 진입
                    events.HasTriggerEnter = true;
                    events.TriggerEntity = collision.CollidedEntity;
                }
                else
                {
                    // 트리거 지속
                    events.HasTriggerStay = true;
                    events.TriggerEntity = collision.CollidedEntity;
                }
            }
        }

        // 트리거 종료 검사
        for (int i = 0; i < triggerBuffer.Length; i++)
        {
            var oldTrigger = triggerBuffer[i].TriggerEntity;
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
                events.HasTriggerExit = true;
                events.TriggerEntity = oldTrigger;
            }
        }

        // 트리거 버퍼 업데이트
        triggerBuffer.Clear();
        for (int i = 0; i < currentTriggers.Length; i++)
        {
            triggerBuffer.Add(new TriggerBuffer
            {
                TriggerEntity = currentTriggers[i],
                IsEntering = true
            });
        }

        currentTriggers.Dispose();
    }
}