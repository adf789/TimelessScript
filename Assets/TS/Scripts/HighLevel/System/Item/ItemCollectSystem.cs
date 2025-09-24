
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(AnimationCallbackHandlerSystem))]
public partial class ItemCollectSystem : SystemBase
{
    protected override void OnCreate()
    {
        // 먼저 싱글톤 생성
        CreateCollectorSingleton();

        // 이제 RequireForUpdate 설정
        RequireForUpdate<CollectorComponent>();
    }

    protected override void OnUpdate()
    {
        var collector = SystemAPI.GetSingletonRW<CollectorComponent>();

        if (!collector.ValueRW.InteractCollector.IsCreated)
            return;

        for (int i = 0; i < collector.ValueRW.InteractCollector.Length; i++)
            {
                Debug.Log($"Get Interaction: ({collector.ValueRW.InteractCollector[i].DataID},{collector.ValueRW.InteractCollector[i].DataType.ToString()})");
            }

        if(!collector.ValueRW.InteractCollector.IsEmpty)
            collector.ValueRW.InteractCollector.Clear();
    }

    protected override void OnDestroy()
    {
        // 메모리 정리
        if (SystemAPI.HasSingleton<CollectorComponent>())
        {
            var collector = SystemAPI.GetSingleton<CollectorComponent>();
            if (collector.InteractCollector.IsCreated)
            {
                collector.InteractCollector.Dispose();
            }
        }
    }

    private void CreateCollectorSingleton()
    {
        var entity = World.EntityManager.CreateEntity();
        World.EntityManager.AddComponentData(entity, new CollectorComponent()
        {
            InteractCollector = new NativeList<InteractComponent>(Allocator.Persistent)
        });
    }
}
