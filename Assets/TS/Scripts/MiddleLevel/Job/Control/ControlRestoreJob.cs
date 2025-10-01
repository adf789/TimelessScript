
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct ControlRestoreJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;

    [ReadOnly] public NativeArray<Entity> groundEntities;
    [ReadOnly] public ComponentLookup<ColliderComponent> colliderLookup;
    [ReadOnly] public ComponentLookup<TSGroundComponent> groundLookup;
    [ReadOnly] public ComponentLookup<TSGimmickComponent> gimmickLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;

    public void Execute(
        [EntityIndexInQuery] int entityIndexInQuery,
        Entity entity,
    in MoveRestoreFlagComponent moveRestoreFlag,
    in PhysicsComponent physicsComponent,
    ref TSActorComponent actor,
    ref NavigationComponent navigation)
    {
        if (!physicsComponent.IsGrounded)
            return;

        // Actor 위치 가져오기
        var transform = transformLookup[entity];
        var actorPosition = transform.Position.xy;

        // 타겟 정보 가져오기
        var target = actor.RestoreMove.Target;

        switch (actor.RestoreMove.TargetType)
        {
            case TSObjectType.Gimmick:
                RestoreMoveForGimmick(target, actorPosition, ref actor, ref navigation);
                break;
        }

        ecb.RemoveComponent<MoveRestoreFlagComponent>(entityIndexInQuery, entity);
    }

    private void RestoreMoveForGimmick(
        Entity target,
        float2 actorPosition,
        ref TSActorComponent actor,
        ref NavigationComponent navigation)
    {
        actor.Move.Target = target;
        actor.Move.TargetDataID = actor.RestoreMove.TargetDataID;
        actor.Move.TargetType = actor.RestoreMove.TargetType;

        // Gimmick의 위치와 반지름 정보 가져오기
        var gimmickCollider = colliderLookup.GetRefRO(target);
        var gimmick = gimmickLookup.GetRefRO(target);
        var gimmickTransform = transformLookup.GetRefRO(target);
        var gimmickPosition = gimmickTransform.ValueRO.Position.xy + gimmickCollider.ValueRO.Offset;
        float gimmickRadius = gimmick.ValueRO.Radius;

        // 원형의 중심 아래에 접하는 지형 찾기
        var groundResult = FindGroundBelowCircle(actorPosition, gimmickPosition, gimmickRadius);

        if (groundResult.GroundEntity == Entity.Null)
            return;

        // Navigation 시스템으로 이동
        navigation.IsActive = true;
        navigation.FinalTargetPosition = groundResult.ContactPoint;
        navigation.FinalTargetGround = groundResult.GroundEntity;
        navigation.CurrentWaypointIndex = 0;
        navigation.State = NavigationState.PathFinding;

        Debug.Log($"Moving to ground contact point below gimmick circle. Position: {groundResult.ContactPoint}, Radius: {gimmickRadius}, Gimmick Center: {gimmickPosition}");
    }

    /// <summary>
    /// 원형의 중심 아래에 접하는 지형을 찾고 접촉점을 계산하는 메서드
    /// </summary>
    private GroundContactResult FindGroundBelowCircle(float2 basePosition, float2 circleCenter, float circleRadius)
    {
        Entity bestGround = Entity.Null;
        float2 bestContactPoint = float2.zero;
        float shortestDistance = float.MaxValue;

        // 원의 하단 점에서 아래쪽으로 검색할 범위
        float2 searchStart = circleCenter + new float2(0, -circleRadius);
        float searchRange = circleRadius * 2f; // 원의 지름만큼 아래까지 검색

        for (int i = 0; i < groundEntities.Length; i++)
        {
            var groundEntity = groundEntities[i];

            if (!colliderLookup.HasComponent(groundEntity) ||
                !transformLookup.HasComponent(groundEntity) ||
                !groundLookup.HasComponent(groundEntity))
                continue;

            var collider = colliderLookup[groundEntity];
            var transform = transformLookup[groundEntity];

            float2 groundCenter = transform.Position.xy + collider.Offset;
            float2 groundSize = collider.Size;

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
                        bestGround = groundEntity;
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
}
