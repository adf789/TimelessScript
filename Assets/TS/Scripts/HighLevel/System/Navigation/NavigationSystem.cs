using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ControlSystem))]
[BurstCompile]
public partial class NavigationSystem : SystemBase
{
    #region Constants
    private const float SAME_HEIGHT_THRESHOLD = 0.5f;
    private const float MAX_HORIZONTAL_REACH = 10.0f;
    private const float MAX_VERTICAL_REACH = 8.0f;
    private const float WAYPOINT_ARRIVAL_DISTANCE = 0.2f;
    private const float MINIMUM_MOVE_DISTANCE = 0.5f;
    #endregion

    #region Cached Queries
    private EntityQuery _ladderQuery;
    private EntityQuery _groundQuery;
    #endregion

    protected override void OnCreate()
    {
        RequireForUpdate<NavigationComponent>();

        // 성능 최적화를 위한 쿼리 캐싱 (일반 필터링)
        _ladderQuery = GetEntityQuery(
            ComponentType.ReadOnly<TSObjectComponent>(),
            ComponentType.ReadOnly<ColliderComponent>(),
            ComponentType.ReadOnly<LocalTransform>()
        );

        _groundQuery = GetEntityQuery(
            ComponentType.ReadOnly<TSObjectComponent>(),
            ComponentType.ReadOnly<ColliderComponent>(),
            ComponentType.ReadOnly<LocalTransform>()
        );
    }

    protected override void OnUpdate()
    {
        // 활성 상태 필터링으로 성능 최적화
        foreach (var (navigation, waypoints, entity) in
        SystemAPI.Query<RefRW<NavigationComponent>, DynamicBuffer<NavigationWaypoint>>()
                                            .WithAll<NavigationComponent>()
                                            .WithEntityAccess())
        {
            if (!navigation.ValueRO.IsActive)
                continue;

            ProcessNavigationState(ref navigation.ValueRW, waypoints, entity);
        }
    }

    #region Core Navigation Methods

    [BurstCompile]
    private void ProcessNavigationState(ref NavigationComponent navigation, DynamicBuffer<NavigationWaypoint> waypoints, Entity entity)
    {
        switch (navigation.State)
        {
            case NavigationState.PathFinding:
                ExecutePathFinding(ref navigation, waypoints, entity);
                break;

            case NavigationState.MovingToWaypoint:
                ExecuteWaypointMovement(ref navigation, waypoints, entity);
                break;

            case NavigationState.Completed:
            case NavigationState.Failed:
                navigation.IsActive = false;
                break;
        }
    }

    [BurstCompile]
    private void ExecutePathFinding(ref NavigationComponent navigation, DynamicBuffer<NavigationWaypoint> waypoints, Entity entity)
    {
        // 현재 위치 계산 최적화
        var currentPosition = CalculateCurrentPosition(entity);
        var targetPosition = navigation.FinalTargetPosition;
        var targetGround = navigation.FinalTargetGround;

        // 유효성 검사
        if (!SystemAPI.Exists(targetGround))
        {
            SetNavigationFailed(ref navigation, "Target ground entity does not exist");
            return;
        }

        // 경로 계산 및 생성
        waypoints.Clear();
        if (GenerateNavigationPath(currentPosition, targetPosition, targetGround, waypoints))
        {
            navigation.CurrentWaypointIndex = 0;
            navigation.State = NavigationState.MovingToWaypoint;

            #if UNITY_EDITOR
            Debug.Log($"Navigation path generated with {waypoints.Length} waypoints");
            #endif
        }
        else
        {
            SetNavigationFailed(ref navigation, "Failed to find valid path");
        }
    }

    [BurstCompile]
    private void ExecuteWaypointMovement(ref NavigationComponent navigation, DynamicBuffer<NavigationWaypoint> waypoints, Entity entity)
    {
        // 경로 완료 확인
        if (navigation.CurrentWaypointIndex >= waypoints.Length)
        {
            navigation.State = NavigationState.Completed;
            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"Navigation 완료: 모든 웨이포인트 도달");
            #endif
            return;
        }

        var currentWaypoint = waypoints[navigation.CurrentWaypointIndex];
        var objectComponent = SystemAPI.GetComponentRW<TSObjectComponent>(entity);

        #if UNITY_EDITOR
        UnityEngine.Debug.Log($"[NavigationSystem] 웨이포인트 처리 {navigation.CurrentWaypointIndex}/{waypoints.Length}: {currentWaypoint.MoveType} → ({currentWaypoint.Position.x:F2}, {currentWaypoint.Position.y:F2})");
        #endif

        // 이동 명령 설정
        SetMovementCommand(ref objectComponent.ValueRW, currentWaypoint);

        // 도달 확인
        if (HasReachedWaypoint(entity, currentWaypoint.Position))
        {
            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"웨이포인트 {navigation.CurrentWaypointIndex} 도달! 다음 웨이포인트로 이동");
            #endif
            navigation.CurrentWaypointIndex++;
        }
    }

    #endregion

    #region Utility Methods

    [BurstCompile]
    private float2 CalculateCurrentPosition(Entity entity)
    {
        var transform = SystemAPI.GetComponent<LocalTransform>(entity);
        var tsObject = SystemAPI.GetComponent<TSObjectComponent>(entity);
        var position = transform.Position.xy;
        position.y += tsObject.RootOffset;
        return position;
    }

    [BurstCompile]
    private void SetMovementCommand(ref TSObjectComponent tsObject, NavigationWaypoint waypoint)
    {
        tsObject.Behavior.Target = waypoint.TargetEntity;
        tsObject.Behavior.Purpose = waypoint.MoveType;
        tsObject.Behavior.MovePosition = waypoint.Position;

        #if UNITY_EDITOR
        UnityEngine.Debug.Log($"[NavigationSystem] 이동 명령 설정: {tsObject.Name} → Purpose: {waypoint.MoveType}, Position: ({waypoint.Position.x:F2}, {waypoint.Position.y:F2})");
        #endif
    }

    [BurstCompile]
    private bool HasReachedWaypoint(Entity entity, float2 waypointPosition)
    {
        var currentPosition = CalculateCurrentPosition(entity);
        float distance = math.distance(currentPosition, waypointPosition);

        #if UNITY_EDITOR
        if (distance < WAYPOINT_ARRIVAL_DISTANCE + 0.1f) // 거의 도착한 경우에만 로그
        {
            UnityEngine.Debug.Log($"웨이포인트 도달 확인: 거리 = {distance:F3}, 임계값 = {WAYPOINT_ARRIVAL_DISTANCE}");
        }
        #endif

        return distance < WAYPOINT_ARRIVAL_DISTANCE;
    }

    private void SetNavigationFailed(ref NavigationComponent navigation, string reason)
    {
        navigation.State = NavigationState.Failed;
        #if UNITY_EDITOR
        Debug.LogWarning($"Navigation failed: {reason}");
        #endif
    }

    #endregion

    #region Path Finding

    [BurstCompile]
    private bool GenerateNavigationPath(float2 startPos, float2 targetPos, Entity targetGround, DynamicBuffer<NavigationWaypoint> waypoints)
    {
        // 목표 지형의 정확한 표면 높이 계산
        float targetSurfaceY = CalculateGroundSurfaceHeight(targetGround, targetPos.x);
        var adjustedTargetPos = new float2(targetPos.x, targetSurfaceY);

        // 높이 차이 확인
        if (IsOnSameLevel(startPos.y, targetSurfaceY))
        {
            return CreateDirectPath(adjustedTargetPos, targetGround, waypoints);
        }

        // 복잡한 경로 (사다리 이용) 생성
        return CreateLadderPath(startPos, adjustedTargetPos, targetGround, waypoints);
    }

    [BurstCompile]
    private bool IsOnSameLevel(float startY, float targetY)
    {
        return math.abs(startY - targetY) <= SAME_HEIGHT_THRESHOLD;
    }

    [BurstCompile]
    private bool CreateDirectPath(float2 targetPos, Entity targetGround, DynamicBuffer<NavigationWaypoint> waypoints)
    {
        waypoints.Add(new NavigationWaypoint
        {
            Position = targetPos,
            TargetEntity = targetGround,
            ObjectType = TSObjectType.Ground,
            MoveType = MoveState.Move
        });
        return true;
    }

    [BurstCompile]
    private bool CreateLadderPath(float2 startPos, float2 targetPos, Entity targetGround, DynamicBuffer<NavigationWaypoint> waypoints)
    {
        // 캐시된 쿼리 사용 후 런타임 필터링으로 성능 최적화
        var entities = _ladderQuery.ToEntityArray(Allocator.Temp);
        var tsObjects = _ladderQuery.ToComponentDataArray<TSObjectComponent>(Allocator.Temp);

        var ladders = new NativeList<Entity>(Allocator.Temp);

        // TSObjectType.Ladder 필터링
        for (int i = 0; i < entities.Length; i++)
        {
            if (tsObjects[i].ObjectType == TSObjectType.Ladder)
            {
                ladders.Add(entities[i]);
            }
        }

        entities.Dispose();
        tsObjects.Dispose();

        if (ladders.Length == 0)
        {
            ladders.Dispose();
            return false;
        }

        var bestLadderInfo = FindOptimalLadder(startPos, targetPos, ladders.AsArray());
        ladders.Dispose();

        if (bestLadderInfo.Entity == Entity.Null)
        {
            return false;
        }

        return GenerateLadderWaypoints(startPos, targetPos, targetGround, bestLadderInfo, waypoints);
    }

    private struct LadderInfo
    {
        public Entity Entity;
        public float2 Position;
        public float Distance;
    }

    [BurstCompile]
    private LadderInfo FindOptimalLadder(float2 startPos, float2 targetPos, NativeArray<Entity> ladders)
    {
        var bestLadder = new LadderInfo
        {
            Entity = Entity.Null,
            Distance = float.MaxValue
        };

        for (int i = 0; i < ladders.Length; i++)
        {
            var ladder = ladders[i];
            var ladderPosition = CalculateLadderPosition(ladder);

            // 도달 가능성 및 효율성 검사
            if (IsLadderUsable(startPos, targetPos, ladderPosition))
            {
                float totalDistance = CalculateTotalPathDistance(startPos, targetPos, ladderPosition);

                if (totalDistance < bestLadder.Distance)
                {
                    bestLadder = new LadderInfo
                    {
                        Entity = ladder,
                        Position = ladderPosition,
                        Distance = totalDistance
                    };
                }
            }
        }

        return bestLadder;
    }

    [BurstCompile]
    private float2 CalculateLadderPosition(Entity ladder)
    {
        var collider = SystemAPI.GetComponent<ColliderComponent>(ladder);
        var transform = SystemAPI.GetComponent<LocalTransform>(ladder);
        return transform.Position.xy + collider.offset;
    }

    [BurstCompile]
    private bool IsLadderUsable(float2 startPos, float2 targetPos, float2 ladderPos)
    {
        // 수평 거리 검사
        float distanceToLadder = math.abs(startPos.x - ladderPos.x);
        float distanceFromLadder = math.abs(ladderPos.x - targetPos.x);

        return distanceToLadder <= MAX_HORIZONTAL_REACH &&
               distanceFromLadder <= MAX_HORIZONTAL_REACH;
    }

    [BurstCompile]
    private float CalculateTotalPathDistance(float2 startPos, float2 targetPos, float2 ladderPos)
    {
        return math.distance(startPos, ladderPos) + math.distance(ladderPos, targetPos);
    }

    [BurstCompile]
    private bool GenerateLadderWaypoints(float2 startPos, float2 targetPos, Entity targetGround, LadderInfo ladderInfo, DynamicBuffer<NavigationWaypoint> waypoints)
    {
        var ladderPos = ladderInfo.Position;

        #if UNITY_EDITOR
        UnityEngine.Debug.Log($"사다리 경로 생성: Start({startPos.x:F2}, {startPos.y:F2}) → Ladder({ladderPos.x:F2}, {ladderPos.y:F2}) → Target({targetPos.x:F2}, {targetPos.y:F2})");
        #endif

        // 1. 사다리 X 위치로 수평 이동 (필요한 경우)
        float horizontalDistance = math.abs(startPos.x - ladderPos.x);
        if (horizontalDistance > MINIMUM_MOVE_DISTANCE)
        {
            var moveToLadderPos = new float2(ladderPos.x, startPos.y);
            AddWaypoint(waypoints, moveToLadderPos, ladderInfo.Entity, TSObjectType.Ground, MoveState.Move);

            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"웨이포인트 1 - 사다리로 이동: ({moveToLadderPos.x:F2}, {moveToLadderPos.y:F2})");
            #endif
        }

        // 2. 사다리 이용 (수직 이동)
        bool isClimbingUp = startPos.y < targetPos.y;
        var climbType = isClimbingUp ? MoveState.ClimbUp : MoveState.ClimbDown;
        var climbTargetPos = new float2(ladderPos.x, targetPos.y);

        AddWaypoint(waypoints, climbTargetPos, ladderInfo.Entity, TSObjectType.Ladder, climbType);

        #if UNITY_EDITOR
        UnityEngine.Debug.Log($"웨이포인트 2 - 사다리 {(isClimbingUp ? "올라가기" : "내려가기")}: ({climbTargetPos.x:F2}, {climbTargetPos.y:F2})");
        #endif

        // 3. 목표 지점으로 수평 이동 (필요한 경우)
        float finalHorizontalDistance = math.abs(ladderPos.x - targetPos.x);
        if (finalHorizontalDistance > MINIMUM_MOVE_DISTANCE)
        {
            AddWaypoint(waypoints, targetPos, targetGround, TSObjectType.Ground, MoveState.Move);

            #if UNITY_EDITOR
            UnityEngine.Debug.Log($"웨이포인트 3 - 최종 목표로 이동: ({targetPos.x:F2}, {targetPos.y:F2})");
            #endif
        }

        return true;
    }

    [BurstCompile]
    private void AddWaypoint(DynamicBuffer<NavigationWaypoint> waypoints, float2 position, Entity target, TSObjectType objectType, MoveState moveType)
    {
        waypoints.Add(new NavigationWaypoint
        {
            Position = position,
            TargetEntity = target,
            ObjectType = objectType,
            MoveType = moveType
        });
    }

    #endregion

    #region Ground Surface Calculation

    [BurstCompile]
    private float CalculateGroundSurfaceHeight(Entity ground, float xPosition)
    {
        var collider = SystemAPI.GetComponent<ColliderComponent>(ground);
        var transform = SystemAPI.GetComponent<LocalTransform>(ground);

        // 지형 중심점 계산
        float groundCenterX = transform.Position.x + collider.offset.x;
        float groundCenterY = transform.Position.y + collider.offset.y;
        float halfHeight = collider.size.y * 0.5f;
        float halfWidth = collider.size.x * 0.5f;

        // X 위치 범위 검사
        float leftEdge = groundCenterX - halfWidth;
        float rightEdge = groundCenterX + halfWidth;

        // 다각형 지형 대응: 범위 내외 관계없이 상단 표면 높이 반환
        return groundCenterY + halfHeight;
    }

    [BurstCompile]
    private Entity FindGroundAtPosition(float2 position)
    {
        // 캐시된 쿼리 사용 후 런타임 필터링으로 성능 최적화
        var entities = _groundQuery.ToEntityArray(Allocator.Temp);
        var tsObjects = _groundQuery.ToComponentDataArray<TSObjectComponent>(Allocator.Temp);
        var colliders = _groundQuery.ToComponentDataArray<ColliderComponent>(Allocator.Temp);
        var transforms = _groundQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        Entity foundGround = Entity.Null;

        for (int i = 0; i < entities.Length; i++)
        {
            // TSObjectType.Ground 필터링
            if (tsObjects[i].ObjectType == TSObjectType.Ground)
            {
                var collider = colliders[i];
                var transform = transforms[i];

                if (IsPositionOnGround(position, collider, transform))
                {
                    foundGround = entities[i];
                    break;
                }
            }
        }

        entities.Dispose();
        tsObjects.Dispose();
        colliders.Dispose();
        transforms.Dispose();

        return foundGround;
    }

    [BurstCompile]
    private bool IsPositionOnGround(float2 position, ColliderComponent collider, LocalTransform transform)
    {
        float groundCenterX = transform.Position.x + collider.offset.x;
        float groundCenterY = transform.Position.y + collider.offset.y;
        float halfWidth = collider.size.x * 0.5f;
        float halfHeight = collider.size.y * 0.5f;

        // 수평 범위 확인
        bool withinHorizontalRange = position.x >= groundCenterX - halfWidth &&
                                    position.x <= groundCenterX + halfWidth;

        // 수직 범위 확인 (표면에서 일정 범위 내)
        float surfaceY = groundCenterY + halfHeight;
        bool withinVerticalRange = math.abs(position.y - surfaceY) <= SAME_HEIGHT_THRESHOLD * 2;

        return withinHorizontalRange && withinVerticalRange;
    }

    #endregion
}