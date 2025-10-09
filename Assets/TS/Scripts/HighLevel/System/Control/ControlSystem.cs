
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PickedSystem))]
[BurstCompile]
public partial struct ControlSystem : ISystem
{
    private Entity controlTarget;
    private EntityStorageInfoLookup entityLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TSActorComponent>();

        entityLookup = state.GetEntityStorageInfoLookup();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 리스폰 된 액터 컨트롤 복구
        OnUpdateRestoreControl(ref state);

        // 엔티티가 참조된 후 삭제되었다면 타겟 캐싱 취소
        CheckValidControlTarget(ref state);

        // 터치된 타겟 가져옴
        if (!TryGetPickedTarget(ref state, out var pickedTarget, out float2 touchPosition))
            return;

        // 컨트롤 타겟 설정
        if (controlTarget == Entity.Null
        || pickedTarget.ObjectType == TSObjectType.Actor)
        {
            // 선택 가능한 타겟인지 확인
            if (!CheckPossibleControlTarget(ref state, in pickedTarget))
                return;

            controlTarget = pickedTarget.Self;

            Debug.Log($"Select {pickedTarget.Name}");

            return;
        }

        // 목표 타겟 설정
        if (!SetTarget(ref state, pickedTarget))
            return;

        // 이동에 필요한 값 설정
        SetMoveStatus(ref state, in pickedTarget, in touchPosition);
    }

    private void OnUpdateRestoreControl(ref SystemState state)
    {
        // Ground entities를 수집
        var groundQuery = SystemAPI.QueryBuilder()
            .WithAll<TSGroundComponent, ColliderComponent, LocalTransform>()
            .Build();
        var groundEntities = groundQuery.ToEntityArray(Allocator.TempJob);

        // 리스폰 후 컨트롤 상태를 복구함
        var controlRestoreJob = new ControlRestoreJob
        {
            ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            groundEntities = groundEntities,
            colliderLookup = SystemAPI.GetComponentLookup<ColliderComponent>(true),
            groundLookup = SystemAPI.GetComponentLookup<TSGroundComponent>(true),
            gimmickLookup = SystemAPI.GetComponentLookup<TSGimmickComponent>(true),
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
        };

        state.Dependency = controlRestoreJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        // 임시로 할당된 NativeArray 해제
        groundEntities.Dispose();
    }

    private bool TryGetPickedTarget(ref SystemState state,
    out TSObjectComponent pickedTarget,
    out float2 touchPosition)
    {
        pickedTarget = default;
        touchPosition = float2.zero;

        // 선택된 타겟 가져옴
        var targetHolder = SystemAPI.GetSingletonRW<TargetHolderComponent>();

        if (targetHolder.ValueRW.Target.IsNull)
            return false;

        pickedTarget = targetHolder.ValueRW.Target;
        touchPosition = targetHolder.ValueRW.TouchPosition;

        // 선택된 타겟 해제
        targetHolder.ValueRW.Release();

        return true;
    }

    private void CheckValidControlTarget(ref SystemState state)
    {
        entityLookup.Update(ref state);

        if (!entityLookup.Exists(controlTarget))
            controlTarget = Entity.Null;
    }

    /// <summary>
    /// 현재 선택 가능한 타겟인지 확인
    /// </summary>
    private bool CheckPossibleControlTarget(ref SystemState state, in TSObjectComponent selectTarget)
    {
        if (selectTarget.IsNull)
            return false;

        if (selectTarget.Self == controlTarget)
            return false;

        if (selectTarget.ObjectType != TSObjectType.Actor)
            return false;

        if (SystemAPI.HasComponent<PhysicsComponent>(selectTarget.Self))
        {
            var physics = SystemAPI.GetComponent<PhysicsComponent>(selectTarget.Self);

            if (!physics.IsGrounded)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 목표 타겟을 설정함
    /// </summary>
    private bool SetTarget(ref SystemState state, in TSObjectComponent target)
    {
        if (controlTarget == Entity.Null)
            return false;

        if (!SystemAPI.HasComponent<TSActorComponent>(controlTarget))
            return false;

        var actorObject = SystemAPI.GetComponentRW<TSActorComponent>(controlTarget);

        actorObject.ValueRW.Move.Target = target.Self;
        actorObject.ValueRW.Move.TargetDataID = target.DataID;
        actorObject.ValueRW.Move.TargetType = target.ObjectType;

        return true;
    }

    private void SetMoveStatus(ref SystemState state,
    in TSObjectComponent target,
    in float2 touchPosition)
    {
        // Navigation 컴포넌트를 가져옴
        if (!SystemAPI.HasComponent<NavigationComponent>(controlTarget))
            return;

        // 이동에 필요한 컴포넌트를 가져옴
        var navigation = SystemAPI.GetComponentRW<NavigationComponent>(controlTarget);
        var controlTransform = SystemAPI.GetComponent<LocalTransform>(controlTarget);
        var targetTransform = SystemAPI.GetComponent<LocalTransform>(target.Self);
        var actorObject = SystemAPI.GetComponentRW<TSActorComponent>(controlTarget);

        // 시작 지점과 도착 지점
        float2 selfPosition = controlTransform.Position.xy;
        float2 targetPosition = targetTransform.Position.xy;

        // 현재 타겟 저장(엔티티 재생성 시 복구용)
        SaveMoveTarget(ref actorObject.ValueRW, touchPosition);

        switch (target.ObjectType)
        {
            case TSObjectType.Ground:
                {
                    var collider = SystemAPI.GetComponent<ColliderComponent>(target.Self);
                    float2 position = targetPosition + collider.Offset;
                    float halfHeight = collider.Size.y * 0.5f;

                    position.x = touchPosition.x;
                    position.y += halfHeight;

                    // Navigation 시스템으로 이동
                    navigation.ValueRW.IsActive = true;
                    navigation.ValueRW.FinalTargetPosition = position;
                    navigation.ValueRW.FinalTargetGround = target.Self;
                    navigation.ValueRW.CurrentWaypointIndex = 0;
                    navigation.ValueRW.State = NavigationState.PathFinding;
                }
                break;

            case TSObjectType.Gimmick:
                {
                    // Gimmick의 위치와 반지름 정보 가져오기
                    var gimmickCollider = SystemAPI.GetComponent<ColliderComponent>(target.Self);
                    var gimmick = SystemAPI.GetComponent<TSGimmickComponent>(target.Self);
                    float gimmickRadius = gimmick.Radius;

                    // 원형의 중심 아래에 접하는 지형 찾기
                    var groundResult = FindGroundBelowCircle(ref state, selfPosition, targetPosition, gimmickRadius);

                    if (groundResult.GroundEntity != Entity.Null)
                    {
                        // Navigation 시스템으로 이동
                        navigation.ValueRW.IsActive = true;
                        navigation.ValueRW.FinalTargetPosition = groundResult.ContactPoint;
                        navigation.ValueRW.FinalTargetGround = groundResult.GroundEntity;
                        navigation.ValueRW.CurrentWaypointIndex = 0;
                        navigation.ValueRW.State = NavigationState.PathFinding;

                        Debug.Log($"Moving to ground contact point below gimmick circle. Position: {groundResult.ContactPoint}, Radius: {gimmickRadius}, Gimmick Center: {targetPosition}");
                    }
                    else
                    {
                        Debug.Log("No ground found below the gimmick circle");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 원형의 중심 아래에 접하는 지형을 찾고 접촉점을 계산하는 메서드
    /// </summary>
    private GroundContactResult FindGroundBelowCircle(ref SystemState state, float2 basePosition, float2 circleCenter, float circleRadius)
    {
        Entity bestGround = Entity.Null;
        float2 bestContactPoint = float2.zero;
        float shortestDistance = float.MaxValue;

        // 원의 하단 점에서 아래쪽으로 검색할 범위
        float2 searchStart = circleCenter + new float2(0, -circleRadius);
        float searchRange = circleRadius * 2f; // 원의 지름만큼 아래까지 검색

        foreach (var (collider, groundComp, transform, entity) in
                 SystemAPI.Query<RefRO<ColliderComponent>,
                 RefRO<TSGroundComponent>,
                 RefRO<LocalTransform>>().WithEntityAccess())
        {
            float2 groundCenter = transform.ValueRO.Position.xy + collider.ValueRO.Offset;
            float2 groundSize = collider.ValueRO.Size;

            // 지형의 경계 계산
            float2 groundMin = groundCenter - groundSize * 0.5f;
            float2 groundMax = groundCenter + groundSize * 0.5f;

            // 지형이 원의 중심보다 아래에 있는지 확인
            float groundTopY = groundMax.y;
            if (groundTopY < circleCenter.y)
            {
                // 원과 지형 사각형의 접촉점 계산
                float2 contactPoint = Utility.Mathematic.CalculateCircleRectangleContact(basePosition, circleCenter, circleRadius, groundMin, groundMax);

                // 접촉점이 유효한지 확인 (NaN이 아님)
                if (!math.isnan(contactPoint.x) && !math.isnan(contactPoint.y))
                {
                    // 원의 중심에서 접촉점까지의 거리 계산
                    float distance = math.distance(circleCenter, contactPoint);

                    Debug.Log($"Ground found: Center({circleCenter}), Radius({circleRadius}), Ground({groundMin}-{groundMax}), ContactPoint({contactPoint}), Distance({distance})");

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        bestGround = entity;
                        bestContactPoint = contactPoint;
                    }
                }
            }
        }

        return new GroundContactResult
        {
            GroundEntity = bestGround,
            ContactPoint = bestContactPoint,
            Distance = shortestDistance
        };
    }

    private void SaveMoveTarget(ref TSActorComponent actorComponent, float2 touchPosition)
    {
        actorComponent.RestoreMove.Target = actorComponent.Move.Target;
        actorComponent.RestoreMove.TargetDataID = actorComponent.Move.TargetDataID;
        actorComponent.RestoreMove.TargetType = actorComponent.Move.TargetType; actorComponent.RestoreMove.Position = touchPosition;
    }
}