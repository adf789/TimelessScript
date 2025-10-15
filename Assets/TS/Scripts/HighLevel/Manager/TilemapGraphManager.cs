using System.Collections.Generic;
using UnityEngine;
using TS.LowLevel.Data.Config;
using TS.LowLevel.Data.Runtime;

namespace TS.HighLevel.Manager
{
    /// <summary>
    /// 타일맵 패턴 그래프 관리 시스템
    /// 6방향 멀티 링크드 리스트 구조로 패턴 연결 관리
    /// </summary>
    public class TilemapGraphManager : BaseManager<TilemapGraphManager>
    {
        [Header("References")]
        [SerializeField] private TilemapPatternRegistry patternRegistry;
        [SerializeField] private PatternUnlockSystem unlockSystem;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showDebugGizmos = true;

        // 루트 노드 (시작 패턴)
        private TilemapPatternNode _rootNode;

        // 모든 노드 관리 (PatternID_GridX_GridY → Node)
        private Dictionary<string, TilemapPatternNode> _allNodes = new Dictionary<string, TilemapPatternNode>();

        // 월드 그리드 위치 기반 노드 검색 (GridPosition → Node)
        private Dictionary<Vector2Int, TilemapPatternNode> _gridLookup = new Dictionary<Vector2Int, TilemapPatternNode>();

        private void Start()
        {
            if (patternRegistry == null)
            {
                Debug.LogError("[TilemapGraphManager] PatternRegistry is not assigned!");
                return;
            }

            if (unlockSystem == null)
            {
                unlockSystem = PatternUnlockSystem.Instance;
                if (unlockSystem == null)
                {
                    Debug.LogError("[TilemapGraphManager] PatternUnlockSystem not found!");
                    return;
                }
            }

            patternRegistry.Initialize();

            if (showDebugLogs)
                Debug.Log("[TilemapGraphManager] Initialized");
        }

        /// <summary>
        /// 초기 루트 패턴 설정 (게임 시작 시 1개 패턴)
        /// </summary>
        public void SetRootPattern(string patternID, Vector2Int startGridPosition = default)
        {
            var patternData = patternRegistry.GetPattern(patternID);
            if (patternData == null)
            {
                Debug.LogError($"[TilemapGraphManager] Pattern not found: {patternID}");
                return;
            }

            _rootNode = CreateNode(patternID, startGridPosition, patternData);

            if (showDebugLogs)
                Debug.Log($"[TilemapGraphManager] Root pattern set: {patternID} at {startGridPosition}");
        }

        /// <summary>
        /// 두 패턴을 특정 방향으로 연결
        /// </summary>
        public bool ConnectPatterns(string fromPatternID, Vector2Int fromGrid, string toPatternID, PatternDirection direction)
        {
            // 시작 노드 찾기 또는 생성
            TilemapPatternNode fromNode = GetOrCreateNode(fromPatternID, fromGrid);
            if (fromNode == null)
            {
                Debug.LogError($"[TilemapGraphManager] Failed to get/create node: {fromPatternID} at {fromGrid}");
                return false;
            }

            // 타겟 그리드 위치 계산
            Vector2Int toGrid = CalculateTargetGrid(fromGrid, direction, fromNode.PatternData);

            // 타겟 노드 찾기 또는 생성
            TilemapPatternNode toNode = GetOrCreateNode(toPatternID, toGrid);
            if (toNode == null)
            {
                Debug.LogError($"[TilemapGraphManager] Failed to get/create node: {toPatternID} at {toGrid}");
                return false;
            }

            // 양방향 연결
            fromNode.SetNodeInDirection(direction, toNode);
            PatternDirection reverseDirection = GetReverseDirection(direction);
            toNode.SetNodeInDirection(reverseDirection, fromNode);

            if (showDebugLogs)
                Debug.Log($"[TilemapGraphManager] Connected: {fromPatternID}({fromGrid}) → {toPatternID}({toGrid}) [{direction}]");

            return true;
        }

        /// <summary>
        /// 카메라 뷰 내 보이는 노드 찾기
        /// </summary>
        public List<TilemapPatternNode> FindVisibleNodes(Bounds cameraBounds)
        {
            var visibleNodes = new List<TilemapPatternNode>();

            foreach (var node in _allNodes.Values)
            {
                if (node.PatternData == null) continue;

                Bounds nodeBounds = node.GetWorldBounds();
                if (cameraBounds.Intersects(nodeBounds))
                {
                    visibleNodes.Add(node);
                }
            }

            return visibleNodes;
        }

        /// <summary>
        /// 특정 그리드 위치의 노드 가져오기
        /// </summary>
        public TilemapPatternNode GetNodeAtGrid(Vector2Int gridPosition)
        {
            _gridLookup.TryGetValue(gridPosition, out var node);
            return node;
        }

        /// <summary>
        /// 노드 가져오기 또는 생성
        /// </summary>
        private TilemapPatternNode GetOrCreateNode(string patternID, Vector2Int gridPosition)
        {
            // 기존 노드 확인
            if (_gridLookup.TryGetValue(gridPosition, out var existingNode))
            {
                return existingNode;
            }

            // 패턴 데이터 가져오기
            var patternData = patternRegistry.GetPattern(patternID);
            if (patternData == null)
            {
                Debug.LogError($"[TilemapGraphManager] Pattern not found: {patternID}");
                return null;
            }

            // 언락 확인
            if (!unlockSystem.IsPatternUnlocked(patternID))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[TilemapGraphManager] Pattern not unlocked: {patternID}");
                return null;
            }

            // 새 노드 생성
            return CreateNode(patternID, gridPosition, patternData);
        }

        /// <summary>
        /// 노드 생성 및 등록
        /// </summary>
        private TilemapPatternNode CreateNode(string patternID, Vector2Int gridPosition, TilemapPatternData patternData)
        {
            string nodeKey = GetNodeKey(patternID, gridPosition);

            var node = new TilemapPatternNode(patternID, gridPosition, patternData);

            _allNodes[nodeKey] = node;
            _gridLookup[gridPosition] = node;

            return node;
        }

        /// <summary>
        /// 방향에 따른 타겟 그리드 위치 계산
        /// </summary>
        private Vector2Int CalculateTargetGrid(Vector2Int currentGrid, PatternDirection direction, TilemapPatternData patternData)
        {
            return direction switch
            {
                PatternDirection.TopLeft => currentGrid + new Vector2Int(-1, 1),
                PatternDirection.TopRight => currentGrid + new Vector2Int(1, 1),
                PatternDirection.Left => currentGrid + new Vector2Int(-1, 0),
                PatternDirection.Right => currentGrid + new Vector2Int(1, 0),
                PatternDirection.BottomLeft => currentGrid + new Vector2Int(-1, -1),
                PatternDirection.BottomRight => currentGrid + new Vector2Int(1, -1),
                _ => currentGrid
            };
        }

        /// <summary>
        /// 역방향 가져오기
        /// </summary>
        private PatternDirection GetReverseDirection(PatternDirection direction)
        {
            return direction switch
            {
                PatternDirection.TopLeft => PatternDirection.BottomRight,
                PatternDirection.TopRight => PatternDirection.BottomLeft,
                PatternDirection.Left => PatternDirection.Right,
                PatternDirection.Right => PatternDirection.Left,
                PatternDirection.BottomLeft => PatternDirection.TopRight,
                PatternDirection.BottomRight => PatternDirection.TopLeft,
                _ => direction
            };
        }

        /// <summary>
        /// 노드 키 생성
        /// </summary>
        private string GetNodeKey(string patternID, Vector2Int gridPosition)
        {
            return $"{patternID}_{gridPosition.x}_{gridPosition.y}";
        }

        /// <summary>
        /// 모든 노드 개수
        /// </summary>
        public int TotalNodeCount => _allNodes.Count;

        /// <summary>
        /// 루트 노드
        /// </summary>
        public TilemapPatternNode RootNode => _rootNode;

        /// <summary>
        /// 모든 노드 목록
        /// </summary>
        public IEnumerable<TilemapPatternNode> AllNodes => _allNodes.Values;

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || _allNodes == null || _allNodes.Count == 0) return;

            // 모든 노드 시각화
            foreach (var node in _allNodes.Values)
            {
                if (node.PatternData == null) continue;

                Bounds bounds = node.GetWorldBounds();

                // 루트 노드는 녹색, 일반 노드는 파란색, 로드된 노드는 노란색
                if (node == _rootNode)
                    Gizmos.color = Color.green;
                else if (node.IsLoaded)
                    Gizmos.color = Color.yellow;
                else
                    Gizmos.color = Color.blue;

                Gizmos.DrawWireCube(bounds.center, bounds.size);

                // 연결 시각화
                DrawConnections(node);
            }
        }

        private void DrawConnections(TilemapPatternNode node)
        {
            Gizmos.color = Color.cyan;

            Vector3 nodeCenter = node.GetWorldPosition();

            // 6방향 연결 선 그리기
            if (node.TopLeft != null)
                Gizmos.DrawLine(nodeCenter, node.TopLeft.GetWorldPosition());
            if (node.TopRight != null)
                Gizmos.DrawLine(nodeCenter, node.TopRight.GetWorldPosition());
            if (node.Left != null)
                Gizmos.DrawLine(nodeCenter, node.Left.GetWorldPosition());
            if (node.Right != null)
                Gizmos.DrawLine(nodeCenter, node.Right.GetWorldPosition());
            if (node.BottomLeft != null)
                Gizmos.DrawLine(nodeCenter, node.BottomLeft.GetWorldPosition());
            if (node.BottomRight != null)
                Gizmos.DrawLine(nodeCenter, node.BottomRight.GetWorldPosition());
        }

        #endregion
    }
}
