using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ControlSystem))]
[UpdateBefore(typeof(BehaviorSystem))]
[BurstCompile]
public partial struct NavigationSystem : ISystem
{
    #region Cached Queries
    private EntityQuery _ladderQuery;
    private EntityQuery _groundQuery;
    #endregion

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NavigationComponent>();

        // 성능 최적화를 위한 쿼리 캐싱 (일반 필터링)
        _ladderQuery = SystemAPI.QueryBuilder()
        .WithAll<TSObjectComponent>()
        .WithAll<ColliderComponent>()
        .WithAll<LocalTransform>().Build();

        _groundQuery = SystemAPI.QueryBuilder()
        .WithAll<TSObjectComponent>()
        .WithAll<ColliderComponent>()
        .WithAll<LocalTransform>().Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 활성 상태 필터링으로 성능 최적화
        foreach (var (navigation, waypoints, entity) in
        SystemAPI.Query<RefRW<NavigationComponent>, DynamicBuffer<NavigationWaypoint>>()
                                            .WithAll<NavigationComponent>()
                                            .WithEntityAccess())
        {
            if (!navigation.ValueRO.IsActive)
                continue;

            ProcessNavigationState(ref navigation.ValueRW, waypoints, entity, ref state);
        }
    }

    #region Core Navigation Methods

    private void ProcessNavigationState(ref NavigationComponent navigation, DynamicBuffer<NavigationWaypoint> waypoints, Entity entity, ref SystemState state)
    {
        switch (navigation.State)
        {
            case NavigationState.PathFinding:
                ExecutePathFinding(ref navigation, waypoints, entity, ref state);
                break;

            case NavigationState.MovingToWaypoint:
                ExecuteWaypointMovement(ref navigation, waypoints, entity, ref state);
                break;

            case NavigationState.Completed:
            case NavigationState.Failed:
                navigation.IsActive = false;
                break;
        }
    }

    private void ExecutePathFinding(ref NavigationComponent navigation, DynamicBuffer<NavigationWaypoint> waypoints, Entity entity, ref SystemState state)
    {
        // 현재 위치 계산 최적화
        var currentPosition = CalculateCurrentPosition(entity, ref state);
        var targetPosition = navigation.FinalTargetPosition;
        var targetGround = navigation.FinalTargetGround;

        // 유효성 검사
        if (targetGround == Entity.Null)
        {
            SetNavigationFailed(ref navigation, "Target ground entity does not exist");
            return;
        }

        // 경로 계산 및 생성
        waypoints.Clear();
        if (GenerateNavigationPath(currentPosition, targetPosition, targetGround, waypoints, ref state))
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

    private void ExecuteWaypointMovement(ref NavigationComponent navigation, DynamicBuffer<NavigationWaypoint> waypoints, Entity entity, ref SystemState state)
    {
        var objectComponent = state.EntityManager.GetComponentData<TSObjectComponent>(entity);

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

#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[NavigationSystem] 웨이포인트 처리 {navigation.CurrentWaypointIndex}/{waypoints.Length}: {currentWaypoint.MoveType} → ({currentWaypoint.Position.x:F2}, {currentWaypoint.Position.y:F2})");
#endif

        // 이동 명령 설정
        SetMovementCommand(ref objectComponent, currentWaypoint);
        state.EntityManager.SetComponentData(entity, objectComponent);

        // 도달 확인
        if (HasReachedWaypoint(entity, currentWaypoint.Position, ref state))
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"웨이포인트 {navigation.CurrentWaypointIndex} 도달! 다음 웨이포인트로 이동");
#endif
            navigation.CurrentWaypointIndex++;
        }
    }

    #endregion

    #region Utility Methods

    private float2 CalculateCurrentPosition(Entity entity, ref SystemState state)
    {
        var transform = state.EntityManager.GetComponentData<LocalTransform>(entity);
        var tsObject = state.EntityManager.GetComponentData<TSObjectComponent>(entity);
        var position = transform.Position.xy;
        position.y += tsObject.RootOffset;
        return position;
    }

    [BurstCompile]
    private void SetMovementCommand(ref TSObjectComponent tsObject, NavigationWaypoint waypoint)
    {
        tsObject.Behavior.Target = waypoint.TargetEntity;
        tsObject.Behavior.MoveState = waypoint.MoveType;
        tsObject.Behavior.MovePosition = waypoint.Position;

#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[NavigationSystem] 이동 명령 설정: {tsObject.Name} → Purpose: {waypoint.MoveType}, Position: ({waypoint.Position.x:F2}, {waypoint.Position.y:F2})");
#endif
    }

    private bool HasReachedWaypoint(Entity entity, float2 waypointPosition, ref SystemState state)
    {
        var currentPosition = CalculateCurrentPosition(entity, ref state);
        float distance = math.distance(currentPosition, waypointPosition);

#if UNITY_EDITOR
        if (distance < StringDefine.AUTO_MOVE_WAYPOINT_ARRIVAL_DISTANCE + 0.1f) // 거의 도착한 경우에만 로그
        {
            UnityEngine.Debug.Log($"웨이포인트 도달 확인: 거리 = {distance:F3}, 임계값 = {StringDefine.AUTO_MOVE_WAYPOINT_ARRIVAL_DISTANCE}");
        }
#endif

        return distance < StringDefine.AUTO_MOVE_WAYPOINT_ARRIVAL_DISTANCE;
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

    private bool GenerateNavigationPath(float2 startPos, float2 targetPos, Entity targetGround, DynamicBuffer<NavigationWaypoint> waypoints, ref SystemState state)
    {
        // 목표 지형의 정확한 표면 높이 계산
        float targetSurfaceY = GetGroundSurfaceHeight(targetGround, ref state);
        var adjustedTargetPos = new float2(targetPos.x, targetSurfaceY);

        // 높이 차이 확인
        if (IsOnSameLevel(startPos.y, targetSurfaceY))
        {
            return CreateDirectPath(adjustedTargetPos, targetGround, waypoints);
        }

        // 복잡한 경로 (사다리 이용) 생성
        return CreateLadderPath(startPos, adjustedTargetPos, targetGround, waypoints, ref state);
    }

    [BurstCompile]
    private bool IsOnSameLevel(float startY, float targetY)
    {
        return math.abs(startY - targetY) <= StringDefine.AUTO_MOVE_SAME_HEIGHT_THRESHOLD;
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

    private bool CreateLadderPath(float2 startPos, float2 targetPos, Entity targetGround, DynamicBuffer<NavigationWaypoint> waypoints, ref SystemState state)
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

        // 다중 사다리 경로 찾기 시도
        var pathFound = FindMultiLadderPath(startPos, targetPos, targetGround, ladders.AsArray(), waypoints, ref state);

        ladders.Dispose();
        return pathFound;
    }

    private struct LadderInfo
    {
        public Entity Entity;
        public float2 Position;
        public Entity TopConnectedGround;
        public Entity BottomConnectedGround;
        public float Distance;
    }

    private struct PathNode
    {
        public float2 Position;
        public Entity GroundEntity;
        public float CostFromStart;
        public float EstimatedCostToTarget;
        public int PreviousNodeIndex;
        public Entity LadderUsed; // 이 노드에 도달하기 위해 사용된 사다리
    }

    /// <summary>
    /// 다중 사다리를 통한 최적 경로를 찾는 함수
    /// </summary>
    private bool FindMultiLadderPath(float2 startPos, float2 targetPos, Entity targetGround, NativeArray<Entity> ladders, DynamicBuffer<NavigationWaypoint> waypoints, ref SystemState state)
    {
        // 시작 지형과 목표 지형이 같은지 확인
        Entity startGround = FindGroundAtPosition(startPos);

#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[FindMultiLadderPath] 시작 지형: {startGround.Index}, 목표 지형: {targetGround.Index}");
#endif

        // 같은 지형이면 직접 이동
        if (startGround == targetGround && startGround != Entity.Null)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[FindMultiLadderPath] 같은 지형이므로 직접 이동");
#endif
            return CreateDirectPath(targetPos, targetGround, waypoints);
        }

        // 다른 지형이므로 사다리 경유 필요
#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[FindMultiLadderPath] 다른 지형이므로 사다리 경유 필요");
#endif

        // 항상 A* 알고리즘을 사용하여 최적 경로 찾기 (단일/다중 사다리 모두 고려)
#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[FindMultiLadderPath] A* 알고리즘으로 최적 경로 찾기 시작");
#endif

        // 사다리 정보 수집
        var ladderInfos = new NativeList<LadderInfo>(Allocator.Temp);
        CollectLadderInfos(ladders, ladderInfos, ref state);

#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[FindMultiLadderPath] 수집된 사다리 개수: {ladderInfos.Length}");
#endif

        if (ladderInfos.Length == 0)
        {
            ladderInfos.Dispose();
            return false;
        }

        // A* 알고리즘으로 다중 사다리 경로 찾기
        var path = FindPathUsingAStar(startPos, startGround, targetPos, targetGround, ladderInfos, ref state);
        ladderInfos.Dispose();

        if (path.Length == 0)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[FindMultiLadderPath] A* 알고리즘으로 경로를 찾지 못함");
#endif
            path.Dispose();
            return false;
        }

#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[FindMultiLadderPath] A* 알고리즘으로 {path.Length}개 노드 경로 발견");
#endif

        // 경로를 웨이포인트로 변환
        GenerateWaypointsFromPath(path, waypoints, ref state);

        // 최종 목표 지점 추가 (A* 경로의 마지막 지점이 실제 터치된 목표 위치와 다를 수 있음)
        if (waypoints.Length > 0)
        {
            var lastWaypoint = waypoints[waypoints.Length - 1];
            float finalDistance = math.distance(lastWaypoint.Position, targetPos);

            // 마지막 웨이포인트와 최종 목표 사이의 거리가 있으면 최종 목표 웨이포인트 추가
            if (finalDistance > StringDefine.AUTO_MOVE_MINIMUM_DISTANCE)
            {
                AddWaypoint(waypoints, targetPos, targetGround, TSObjectType.Ground, MoveState.Move);

#if UNITY_EDITOR
                UnityEngine.Debug.Log($"A* 최종 목표 웨이포인트 추가: ({targetPos.x:F2}, {targetPos.y:F2}) - Move");
#endif
            }
        }

        path.Dispose();
        return true;
    }

    /// <summary>
    /// 모든 사다리의 상세 정보를 수집 (ConnectedGround 기반)
    /// </summary>
    private void CollectLadderInfos(NativeArray<Entity> ladders, NativeList<LadderInfo> ladderInfos, ref SystemState state)
    {
        for (int i = 0; i < ladders.Length; i++)
        {
            var ladder = ladders[i];
            if (!state.EntityManager.HasComponent<LadderComponent>(ladder)) continue;

            var ladderComponent = state.EntityManager.GetComponentData<LadderComponent>(ladder);
            var transform = state.EntityManager.GetComponentData<LocalTransform>(ladder);

            var ladderInfo = new LadderInfo
            {
                Entity = ladder,
                Position = transform.Position.xy,
                TopConnectedGround = ladderComponent.TopConnectedGround,
                BottomConnectedGround = ladderComponent.BottomConnectedGround,
                Distance = 0
            };

            ladderInfos.Add(ladderInfo);
        }
    }

    /// <summary>
    /// A* 알고리즘을 사용한 다중 사다리 경로 찾기
    /// </summary>
    private NativeList<PathNode> FindPathUsingAStar(float2 startPos, Entity startGround, float2 targetPos, Entity targetGround, NativeList<LadderInfo> ladderInfos, ref SystemState state)
    {
        var openList = new NativeList<PathNode>(Allocator.Temp);
        var closedList = new NativeList<PathNode>(Allocator.Temp);
        var path = new NativeList<PathNode>(Allocator.Temp);

        // 시작 노드 추가
        var startNode = new PathNode
        {
            Position = startPos,
            GroundEntity = startGround,
            CostFromStart = 0,
            EstimatedCostToTarget = math.distance(startPos, targetPos),
            PreviousNodeIndex = -1,
            LadderUsed = Entity.Null
        };
        openList.Add(startNode);

        int maxIterations = 100; // 무한 루프 방지
        int iterations = 0;

        while (openList.Length > 0 && iterations < maxIterations)
        {
            iterations++;

            // 가장 낮은 F 비용을 가진 노드 찾기
            int currentIndex = FindLowestFCostNode(openList);
            var currentNode = openList[currentIndex];

            // 목표에 도달했는지 확인 - 지형 표면 기준으로 판단
            if (IsReachedTargetGround(currentNode, targetPos, targetGround, ref state))
            {
                // 경로 재구성
                ReconstructPath(currentNode, openList, closedList, path);
                break;
            }

            // 현재 노드를 닫힌 목록으로 이동
            openList.RemoveAtSwapBack(currentIndex);
            closedList.Add(currentNode);

            // 인접 노드들 탐색
            ExploreNeighborNodes(currentNode, targetPos, targetGround, ladderInfos, openList, closedList, ref state);
        }

        openList.Dispose();
        closedList.Dispose();

        return path;
    }

    /// <summary>
    /// 인접 노드들을 탐색하여 openList에 추가
    /// </summary>
    private void ExploreNeighborNodes(PathNode currentNode, float2 targetPos, Entity targetGround, NativeList<LadderInfo> ladderInfos, NativeList<PathNode> openList, NativeList<PathNode> closedList, ref SystemState state)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log($"[ExploreNeighborNodes] 현재 노드: 지형 {currentNode.GroundEntity.Index}, 목표 지형: {targetGround.Index}");
#endif

        for (int i = 0; i < ladderInfos.Length; i++)
        {
            var ladder = ladderInfos[i];

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[ExploreNeighborNodes] 사다리 {ladder.Entity.Index} 검사: 상단({ladder.TopConnectedGround.Index}) - 하단({ladder.BottomConnectedGround.Index})");
#endif

            // 현재 지형에서 이 사다리에 도달할 수 있는지 확인
            bool canReachBottom = CanReachLadderFromGround(currentNode, ladder.Entity, ladder.BottomConnectedGround, ref state);
            bool canReachTop = CanReachLadderFromGround(currentNode, ladder.Entity, ladder.TopConnectedGround, ref state);

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[ExploreNeighborNodes] 사다리 {ladder.Entity.Index} 도달 가능성: 하단 {canReachBottom}, 상단 {canReachTop}");
#endif

            // 하단에 도달 가능하면 상단으로 이동 가능
            if (canReachBottom)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"[ExploreNeighborNodes] 사다리 {ladder.Entity.Index} 하단에서 상단(지형 {ladder.TopConnectedGround.Index})으로 이동 시도");
#endif
                AddLadderNode(currentNode, ladder, ladder.TopConnectedGround, targetPos, targetGround, openList, closedList, ref state);
            }

            // 상단에 도달 가능하면 하단으로 이동 가능
            if (canReachTop)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"[ExploreNeighborNodes] 사다리 {ladder.Entity.Index} 상단에서 하단(지형 {ladder.BottomConnectedGround.Index})으로 이동 시도");
#endif
                AddLadderNode(currentNode, ladder, ladder.BottomConnectedGround, targetPos, targetGround, openList, closedList, ref state);
            }
        }
    }

    /// <summary>
    /// 사다리 노드를 openList에 추가
    /// </summary>
    private void AddLadderNode(PathNode currentNode, LadderInfo ladder, Entity connectedGround, float2 targetPos, Entity targetGround, NativeList<PathNode> openList, NativeList<PathNode> closedList, ref SystemState state)
    {
        // 연결된 지형이 없으면 무시
        if (connectedGround == Entity.Null) return;

        // 현재 지형과 같은 지형으로는 이동하지 않음 (이미 그 지형에 있음)
        if (currentNode.GroundEntity == connectedGround)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[AddLadderNode] 스킵: 현재 지형({currentNode.GroundEntity.Index})과 연결 지형({connectedGround.Index})이 같음");
#endif
            return;
        }

        // 사다리 위치에서 연결된 지형의 표면 높이 계산
        var ladderTransform = state.EntityManager.GetComponentData<LocalTransform>(ladder.Entity);
        float surfaceY = GetGroundSurfaceHeight(connectedGround, ref state);
        var surfacePosition = new float2(ladderTransform.Position.x, surfaceY);

        // 이미 닫힌 목록에 있는지 확인
        if (IsInClosedList(surfacePosition, closedList)) return;

        // 현재 노드가 지형에 있다면 지형 표면 기준으로 비용 계산
        float2 currentSurfacePos = currentNode.Position;
        if (currentNode.GroundEntity != Entity.Null)
        {
            float currentSurfaceY = GetGroundSurfaceHeight(currentNode.GroundEntity, ref state);
            currentSurfacePos = new float2(currentNode.Position.x, currentSurfaceY);
        }

        float newCost = currentNode.CostFromStart + math.distance(currentSurfacePos, surfacePosition);

        // 이미 openList에 있는지 확인
        int existingIndex = FindInOpenList(surfacePosition, openList);
        if (existingIndex >= 0)
        {
            // 더 나은 경로인지 확인
            if (newCost < openList[existingIndex].CostFromStart)
            {
                var existingNode = openList[existingIndex];
                existingNode.CostFromStart = newCost;
                existingNode.PreviousNodeIndex = FindNodeIndex(currentNode, openList, closedList);
                existingNode.LadderUsed = ladder.Entity;
                openList[existingIndex] = existingNode;
            }
        }
        else
        {
            // 새 노드 추가 - 지형 표면 위치 사용
            var newNode = new PathNode
            {
                Position = surfacePosition,
                GroundEntity = connectedGround,
                CostFromStart = newCost,
                EstimatedCostToTarget = math.distance(surfacePosition, targetPos),
                PreviousNodeIndex = FindNodeIndex(currentNode, openList, closedList),
                LadderUsed = ladder.Entity
            };
            openList.Add(newNode);

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"A* 노드 추가: 지형 {connectedGround.Index} 표면 ({surfacePosition.x:F2}, {surfacePosition.y:F2}) - 사다리 {ladder.Entity.Index} (현재: {currentNode.GroundEntity.Index})");
#endif
        }
    }

    private LadderInfo FindOptimalLadder(float2 startPos, float2 targetPos, NativeArray<Entity> ladders, ref SystemState state)
    {
        var bestLadder = new LadderInfo
        {
            Entity = Entity.Null,
            Distance = float.MaxValue
        };

        for (int i = 0; i < ladders.Length; i++)
        {
            var ladder = ladders[i];
            var ladderPosition = CalculateLadderPosition(ladder, ref state);

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

    private float2 CalculateLadderPosition(Entity ladder, ref SystemState state)
    {
        var collider = state.EntityManager.GetComponentData<ColliderComponent>(ladder);
        var transform = state.EntityManager.GetComponentData<LocalTransform>(ladder);
        return transform.Position.xy + collider.offset;
    }

    [BurstCompile]
    private bool IsLadderUsable(float2 startPos, float2 targetPos, float2 ladderPos)
    {
        // 수평 거리 검사
        float distanceToLadder = math.abs(startPos.x - ladderPos.x);
        float distanceFromLadder = math.abs(ladderPos.x - targetPos.x);

        return distanceToLadder <= StringDefine.AUTO_MOVE_MAX_HORIZONTAL_REACH &&
               distanceFromLadder <= StringDefine.AUTO_MOVE_MAX_HORIZONTAL_REACH;
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
        if (horizontalDistance > StringDefine.AUTO_MOVE_MINIMUM_DISTANCE)
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
        if (finalHorizontalDistance > StringDefine.AUTO_MOVE_MINIMUM_DISTANCE)
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

    private float GetGroundSurfaceHeight(Entity ground, ref SystemState state)
    {
        var collider = state.EntityManager.GetComponentData<ColliderComponent>(ground);
        var transform = state.EntityManager.GetComponentData<LocalTransform>(ground);

        // 지형 중심점 계산
        float groundCenterY = transform.Position.y + collider.offset.y;
        float halfHeight = collider.size.y * 0.5f;

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
        bool withinVerticalRange = math.abs(position.y - surfaceY) <= StringDefine.AUTO_MOVE_SAME_HEIGHT_THRESHOLD * 2;

        return withinHorizontalRange && withinVerticalRange;
    }

    #endregion

    #region A* Algorithm Helper Functions

    /// <summary>
    /// 현재 지형에서 사다리에 도달할 수 있는지 확인 (ConnectedGround 기준)
    /// </summary>
    [BurstCompile]
    private bool CanReachLadderFromGround(PathNode currentNode, Entity ladderEntity, Entity ladderConnectedGround, ref SystemState state)
    {
        // 같은 지형에 있거나 연결된 지형인 경우만 도달 가능
        if (currentNode.GroundEntity == ladderConnectedGround && ladderConnectedGround != Entity.Null)
        {
            var ladderTransform = state.EntityManager.GetComponentData<LocalTransform>(ladderEntity);
            float horizontalDistance = math.abs(currentNode.Position.x - ladderTransform.Position.x);
            return horizontalDistance <= StringDefine.AUTO_MOVE_MAX_HORIZONTAL_REACH;
        }

        return false;
    }

    /// <summary>
    /// 목표 지형에 도달했는지 확인
    /// </summary>
    [BurstCompile]
    private bool IsReachedTargetGround(PathNode currentNode, float2 targetPos, Entity targetGround, ref SystemState state)
    {
        // 목표 지형에 있는지 확인
        if (currentNode.GroundEntity == targetGround && targetGround != Entity.Null)
        {
            // 지형 표면 높이 기준으로 도달 여부 판단
            float currentSurfaceY = GetGroundSurfaceHeight(currentNode.GroundEntity, ref state);
            float heightDiff = math.abs(currentNode.Position.y - currentSurfaceY);

            // 목표 지형에 있고 표면 높이가 적절하면 도달 (걸어서 이동 가능)
            return heightDiff <= StringDefine.AUTO_MOVE_SAME_HEIGHT_THRESHOLD;
        }

        return false;
    }

    /// <summary>
    /// 가장 낮은 F 비용을 가진 노드의 인덱스를 찾기
    /// </summary>
    [BurstCompile]
    private int FindLowestFCostNode(NativeList<PathNode> openList)
    {
        float lowestCost = float.MaxValue;
        int lowestIndex = 0;

        for (int i = 0; i < openList.Length; i++)
        {
            float fCost = openList[i].CostFromStart + openList[i].EstimatedCostToTarget;
            if (fCost < lowestCost)
            {
                lowestCost = fCost;
                lowestIndex = i;
            }
        }

        return lowestIndex;
    }

    /// <summary>
    /// 닫힌 목록에 해당 위치가 있는지 확인
    /// </summary>
    [BurstCompile]
    private bool IsInClosedList(float2 position, NativeList<PathNode> closedList)
    {
        for (int i = 0; i < closedList.Length; i++)
        {
            if (math.distance(closedList[i].Position, position) < 0.1f)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 열린 목록에서 해당 위치의 인덱스를 찾기
    /// </summary>
    [BurstCompile]
    private int FindInOpenList(float2 position, NativeList<PathNode> openList)
    {
        for (int i = 0; i < openList.Length; i++)
        {
            if (math.distance(openList[i].Position, position) < 0.1f)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 노드의 인덱스를 찾기 (openList + closedList에서)
    /// </summary>
    [BurstCompile]
    private int FindNodeIndex(PathNode node, NativeList<PathNode> openList, NativeList<PathNode> closedList)
    {
        // 먼저 closedList에서 찾기
        for (int i = 0; i < closedList.Length; i++)
        {
            if (math.distance(closedList[i].Position, node.Position) < 0.1f)
                return i + 1000; // closedList 인덱스는 1000을 더해서 구분
        }

        // openList에서 찾기
        for (int i = 0; i < openList.Length; i++)
        {
            if (math.distance(openList[i].Position, node.Position) < 0.1f)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 경로 재구성
    /// </summary>
    private void ReconstructPath(PathNode targetNode, NativeList<PathNode> openList, NativeList<PathNode> closedList, NativeList<PathNode> path)
    {
        var currentNode = targetNode;
        var tempPath = new NativeList<PathNode>(Allocator.Temp);

        // 역순으로 경로 구성
        while (currentNode.PreviousNodeIndex >= 0)
        {
            tempPath.Add(currentNode);

            // 이전 노드 찾기
            if (currentNode.PreviousNodeIndex >= 1000)
            {
                // closedList에서 찾기
                int index = currentNode.PreviousNodeIndex - 1000;
                if (index < closedList.Length)
                    currentNode = closedList[index];
                else
                    break;
            }
            else
            {
                // openList에서 찾기
                if (currentNode.PreviousNodeIndex < openList.Length)
                    currentNode = openList[currentNode.PreviousNodeIndex];
                else
                    break;
            }
        }
        
        // 시작 위치 추가
        path.Add(currentNode);

        // 경로를 올바른 순서로 복사
        for (int i = tempPath.Length - 1; i >= 0; i--)
        {
            path.Add(tempPath[i]);
        }

        tempPath.Dispose();
    }

    /// <summary>
    /// A* 경로를 웨이포인트로 변환
    /// </summary>
    private void GenerateWaypointsFromPath(NativeList<PathNode> path, DynamicBuffer<NavigationWaypoint> waypoints, ref SystemState state)
    {
        // 현재 위치부터 시작
        for (int i = 1; i < path.Length; i++)
        {
            var node = path[i];
            var moveType = MoveState.Move;
            var targetEntity = node.GroundEntity;
            var objectType = TSObjectType.Ground;

            // 사다리를 사용하는 경우
            if (node.LadderUsed != Entity.Null)
            {
                if (state.EntityManager.HasComponent<LadderComponent>(node.LadderUsed))
                {
                    var ladderComponent = state.EntityManager.GetComponentData<LadderComponent>(node.LadderUsed);
                    var ladderTransform = state.EntityManager.GetComponentData<LocalTransform>(node.LadderUsed);

                    // 이전 노드 정보 가져오기
                    var prevNode = path[i - 1];

                    // 1. 사다리 시작점으로 수평 이동 (사다리 밑부분 또는 윗부분)
                    if (prevNode.GroundEntity != Entity.Null)
                    {
                        float startSurfaceY = GetGroundSurfaceHeight(prevNode.GroundEntity, ref state);
                        var moveToLadderStartPosition = new float2(ladderTransform.Position.x, startSurfaceY);

                        // 거리가 충분히 떨어져 있으면 수평 이동 웨이포인트 추가
                        float horizontalDistance = math.abs(prevNode.Position.x - ladderTransform.Position.x);
                        if (horizontalDistance > StringDefine.AUTO_MOVE_MINIMUM_DISTANCE)
                        {
                            AddWaypoint(waypoints, moveToLadderStartPosition, prevNode.GroundEntity , TSObjectType.Ground, MoveState.Move);
#if UNITY_EDITOR
                            UnityEngine.Debug.Log($"A* 사다리 접근 웨이포인트: ({moveToLadderStartPosition.x:F2}, {moveToLadderStartPosition.y:F2}) - Move");
#endif
                        }
                    }

                    // 사다리 방향 결정 (올라가는지 내려가는지)
                    bool isClimbingUp = false;
                    Entity endGround = Entity.Null;

                    if (i > 0)
                    {
                        if (node.Position.y > prevNode.Position.y)
                        {
                            isClimbingUp = true;
                            endGround = ladderComponent.TopConnectedGround;
                            moveType = MoveState.ClimbUp;
                        }
                        else
                        {
                            isClimbingUp = false;
                            endGround = ladderComponent.BottomConnectedGround;
                            moveType = MoveState.ClimbDown;
                        }
                    }

                    // 2. 사다리를 타고 반대편으로 이동 (사다리 윗부분 또는 밑부분)
                    if (endGround != Entity.Null)
                    {
                        float endSurfaceY = GetGroundSurfaceHeight(endGround, ref state);
                        var ladderEndPosition = new float2(ladderTransform.Position.x, endSurfaceY);

                        AddWaypoint(waypoints, ladderEndPosition, node.LadderUsed, TSObjectType.Ladder, moveType);

#if UNITY_EDITOR
                        UnityEngine.Debug.Log($"A* 사다리 {(isClimbingUp ? "올라가기" : "내려가기")} 웨이포인트: ({ladderEndPosition.x:F2}, {ladderEndPosition.y:F2}) - {moveType} to {(isClimbingUp ? "Top" : "Bottom")}");
#endif
                    }

                    continue;
                }
            }

            // 일반 지형 이동
            AddWaypoint(waypoints, node.Position, targetEntity, objectType, moveType);

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"A* 웨이포인트 {i}: ({node.Position.x:F2}, {node.Position.y:F2}) - {moveType}");
#endif
        }
    }

    #endregion
}