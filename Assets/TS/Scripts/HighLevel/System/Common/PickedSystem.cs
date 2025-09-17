// Assets/TS/Scripts/HighLevel/System/Common/PickedSystem.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[BurstCompile]
public partial class PickedSystem : SystemBase
{
    private Camera _camera;
    private float3 _touchPosition;
    private bool _isTouchDown;
    private EntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        RequireForUpdate<PickedComponent>();
        _ecbSystem = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        CheckTouchDown();
        UpdateTouchPosition();

        if (!_isTouchDown)
        {
            return;
        }

        var ecb = _ecbSystem.CreateCommandBuffer();
        var currentlyPickedEntity = Entity.Null;
        
        // 1. 이전에 선택된 엔티티의 태그를 제거합니다.
        foreach (var (tag, entity) in SystemAPI.Query<IsPickedTag>().WithEntityAccess())
        {
            ecb.RemoveComponent<IsPickedTag>(entity);
        }

        // 2. 클릭된 위치에 있는 엔티티 중 가장 우선순위가 높은 엔티티를 찾습니다.
        int maxOrder = int.MinValue;
        Entity nextPickedEntity = Entity.Null;

        foreach (var (picked, bounds, entity) in SystemAPI.Query<RefRO<PickedComponent>, RefRO<ColliderBoundsComponent>>().WithEntityAccess())
        {
            var boundsValue = new Rect(bounds.ValueRO.min, bounds.ValueRO.max - bounds.ValueRO.min);
            if (boundsValue.Contains(_touchPosition.xy))
            {
                if (picked.ValueRO.Order > maxOrder)
                {
                    maxOrder = picked.ValueRO.Order;
                    nextPickedEntity = entity;
                }
            }
        }

        // 3. 새로 선택된 엔티티에 태그를 추가합니다.
        if (nextPickedEntity != Entity.Null)
        {
            ecb.AddComponent(nextPickedEntity, new IsPickedTag
            {
                TouchPosition = _touchPosition.xy
            });
        }
        
        _ecbSystem.AddJobHandleForProducer(Dependency);
    }

    private void CheckTouchDown()
    {
        _isTouchDown = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void UpdateTouchPosition()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) return;
        }

        if (Mouse.current == null) return;
        
        var screenPos = Mouse.current.position.ReadValue();
        var worldPos = _camera.ScreenToWorldPoint(new float3(screenPos.x, screenPos.y, 0));
        _touchPosition = new float3(worldPos.x, worldPos.y, 0);
    }
}