
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

        var target = actor.RestoreMove.Target;

        switch (actor.RestoreMove.TargetType)
        {
            case TSObjectType.Gimmick:
                {
                    actor.Move.Target = target;
                    actor.Move.TargetDataID = actor.RestoreMove.TargetDataID;
                    actor.Move.TargetType = actor.RestoreMove.TargetType;

                    // Gimmick의 위치와 반지름 정보 가져오기
                    var gimmickCollider = colliderLookup.GetRefRO(target);
                    var gimmick = gimmickLookup.GetRefRO(target);
                    var transform = transformLookup.GetRefRO(target);
                    var gimmickPosition = transform.ValueRO.Position.xy + gimmickCollider.ValueRO.Offset;
                    float gimmickRadius = gimmick.ValueRO.Radius;

                    // 원형의 중심 아래에 접하는 지형 찾기
                    var groundResult = FindGroundBelowCircle(gimmickPosition, gimmickRadius);

                    if (groundResult.GroundEntity != Entity.Null)
                    {
                        // Navigation 시스템으로 이동
                        navigation.IsActive = true;
                        navigation.FinalTargetPosition = groundResult.ContactPoint;
                        navigation.FinalTargetGround = groundResult.GroundEntity;
                        navigation.CurrentWaypointIndex = 0;
                        navigation.State = NavigationState.PathFinding;

                        Debug.Log($"Moving to ground contact point below gimmick circle. Position: {groundResult.ContactPoint}, Radius: {gimmickRadius}, Gimmick Center: {gimmickPosition}");
                    }
                }
                break;
        }

        ecb.RemoveComponent<MoveRestoreFlagComponent>(entityIndexInQuery, entity);
    }

    /// <summary>
    /// 원형의 중심 아래에 접하는 지형을 찾고 접촉점을 계산하는 메서드
    /// </summary>
    public GroundContactResult FindGroundBelowCircle(float2 circleCenter, float circleRadius)
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
                float2 contactPoint = CalculateCircleRectangleContact(circleCenter, circleRadius, groundMin, groundMax);

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

    /// <summary>
    /// 원과 사각형(지형) 사이의 접촉점을 계산하는 메서드
    /// 원이 지형 상단면과 접촉하는 실제 지점들을 모두 찾아서 가장 적절한 점을 반환
    /// </summary>
    private float2 CalculateCircleRectangleContact(float2 circleCenter, float circleRadius, float2 rectMin, float2 rectMax)
    {
        float groundTopY = rectMax.y;

        // 원과 지형 상단면(수평선)의 교점들을 찾기
        var intersections = FindCircleLineIntersections(circleCenter, circleRadius, groundTopY, rectMin.x, rectMax.x);

        if (intersections.Length > 0)
        {
            // 교점이 있으면 원의 중심에서 가장 가까운 교점 반환
            float2 bestPoint = intersections[0];
            float shortestDist = math.distance(circleCenter, bestPoint);

            for (int i = 1; i < intersections.Length; i++)
            {
                float dist = math.distance(circleCenter, intersections[i]);
                if (dist < shortestDist)
                {
                    shortestDist = dist;
                    bestPoint = intersections[i];
                }
            }

            Debug.Log($"Circle-Ground intersection found at: {bestPoint}, Total intersections: {intersections.Length}");
            return bestPoint;
        }

        // 교점이 없으면 모서리와의 접촉 확인
        float2 topLeftCorner = new float2(rectMin.x, rectMax.y);
        float2 topRightCorner = new float2(rectMax.x, rectMax.y);

        float distToTopLeft = math.distance(circleCenter, topLeftCorner);
        float distToTopRight = math.distance(circleCenter, topRightCorner);

        if (distToTopLeft <= circleRadius)
        {
            float2 direction = math.normalize(topLeftCorner - circleCenter);
            float2 contactPoint = circleCenter + direction * circleRadius;
            Debug.Log($"Circle-Corner contact (left): {contactPoint}");
            return contactPoint;
        }

        if (distToTopRight <= circleRadius)
        {
            float2 direction = math.normalize(topRightCorner - circleCenter);
            float2 contactPoint = circleCenter + direction * circleRadius;
            Debug.Log($"Circle-Corner contact (right): {contactPoint}");
            return contactPoint;
        }

        // 접촉하지 않는 경우
        Debug.Log("No contact found between circle and ground");
        return new float2(float.NaN, float.NaN);
    }

    /// <summary>
    /// 원과 수평선의 교점들을 찾는 메서드
    /// </summary>
    private NativeList<float2> FindCircleLineIntersections(float2 circleCenter, float circleRadius, float lineY, float lineMinX, float lineMaxX)
    {
        var intersections = new NativeList<float2>(2, Unity.Collections.Allocator.Temp);

        // 원의 방정식: (x - cx)² + (y - cy)² = r²
        // 수평선: y = lineY
        // 교점을 구하기 위해 y = lineY를 원의 방정식에 대입

        float dy = lineY - circleCenter.y;
        float discriminant = circleRadius * circleRadius - dy * dy;

        // 판별식이 음수면 교점 없음
        if (discriminant < 0)
        {
            return intersections;
        }

        // 교점의 X 좌표들 계산
        float sqrtDiscriminant = math.sqrt(discriminant);
        float x1 = circleCenter.x - sqrtDiscriminant;
        float x2 = circleCenter.x + sqrtDiscriminant;

        // 교점이 지형의 X 범위 내에 있는지 확인
        if (x1 >= lineMinX && x1 <= lineMaxX)
        {
            intersections.Add(new float2(x1, lineY));
        }

        if (x2 >= lineMinX && x2 <= lineMaxX && math.abs(x2 - x1) > 0.001f)
        {
            intersections.Add(new float2(x2, lineY));
        }

        return intersections;
    }
}
