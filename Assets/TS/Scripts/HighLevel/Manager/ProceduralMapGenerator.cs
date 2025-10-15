using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TS.LowLevel.Data.Config;

namespace TS.HighLevel.Manager
{
    /// <summary>
    /// 프로시저럴 맵 생성 시스템
    /// 카메라 위치 기반으로 패턴을 동적으로 확장하여 무한 맵 생성
    /// </summary>
    public class ProceduralMapGenerator : BaseManager<ProceduralMapGenerator>
    {
        [Header("References")]
        [SerializeField] private TilemapPatternRegistry patternRegistry;
        [SerializeField] private TilemapStreamingManager streamingManager;
        [SerializeField] private Camera targetCamera; // 추적할 카메라

        [Header("Generation Settings")]
        [SerializeField] private bool enableAutoExpansion = true;
        [SerializeField] private float expansionDistance = 75f; // 패턴 경계로부터 이 거리에 카메라가 오면 확장
        [SerializeField] private int maxGeneratedPatterns = 50; // 최대 생성 패턴 수
        [SerializeField] private float checkInterval = 1f; // 확장 체크 주기 (초)

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showDebugGizmos = true;

        // 생성된 그리드 추적 (gridOffset -> patternID)
        private Dictionary<Vector2Int, string> _generatedGrids = new Dictionary<Vector2Int, string>();

        // 시드 패턴 (프로시저럴 생성의 시작점)
        private List<Vector2Int> _seedGrids = new List<Vector2Int>();

        // 자동 확장 체크 타이머
        private float _lastCheckTime;

        // 카메라 위치 추적
        private Vector3 _cameraPosition;

        private void Start()
        {
            if (patternRegistry == null)
            {
                Debug.LogError("[ProceduralMapGenerator] PatternRegistry is not assigned!");
                return;
            }

            if (streamingManager == null)
            {
                streamingManager = TilemapStreamingManager.Instance;
                if (streamingManager == null)
                {
                    Debug.LogError("[ProceduralMapGenerator] TilemapStreamingManager not found!");
                    return;
                }
            }

            // 카메라 참조 설정
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogWarning("[ProceduralMapGenerator] No camera found. Auto-expansion disabled.");
                    enableAutoExpansion = false;
                }
            }

            patternRegistry.Initialize();

            if (showDebugLogs)
                Debug.Log($"[ProceduralMapGenerator] Initialized. Camera: {targetCamera?.name}");
        }

        private void Update()
        {
            if (!enableAutoExpansion || targetCamera == null) return;

            // 카메라 위치 업데이트
            _cameraPosition = targetCamera.transform.position;

            if (Time.time - _lastCheckTime >= checkInterval)
            {
                _lastCheckTime = Time.time;
                CheckAndExpandAroundCamera(_cameraPosition);
            }
        }

        /// <summary>
        /// 카메라 참조 설정
        /// </summary>
        public void SetCamera(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogError("[ProceduralMapGenerator] Cannot set null camera!");
                return;
            }

            targetCamera = camera;
            _cameraPosition = camera.transform.position;

            if (showDebugLogs)
                Debug.Log($"[ProceduralMapGenerator] Camera set: {camera.name}");
        }

        /// <summary>
        /// 시드 패턴 등록 (프로시저럴 생성의 시작점)
        /// </summary>
        public void RegisterSeedPattern(string patternID, Vector2Int gridOffset)
        {
            if (!_generatedGrids.ContainsKey(gridOffset))
            {
                _generatedGrids[gridOffset] = patternID;
                _seedGrids.Add(gridOffset);

                if (showDebugLogs)
                    Debug.Log($"[ProceduralMapGenerator] Seed pattern registered: {patternID} at {gridOffset}");
            }
        }

        /// <summary>
        /// 초기 시드 패턴 자동 등록 (로드된 패턴 기반)
        /// </summary>
        public void RegisterLoadedPatternsAsSeed()
        {
            if (streamingManager == null) return;

            var loadedKeys = streamingManager.GetLoadedPatternKeys();
            foreach (var key in loadedKeys)
            {
                if (TryParsePatternKey(key, out string patternID, out Vector2Int gridOffset))
                {
                    RegisterSeedPattern(patternID, gridOffset);
                }
            }

            if (showDebugLogs)
                Debug.Log($"[ProceduralMapGenerator] Registered {_seedGrids.Count} seed patterns from loaded patterns");
        }

        /// <summary>
        /// 특정 방향으로 맵 확장
        /// </summary>
        public async UniTask<bool> ExpandToDirection(Vector2Int currentGrid, Direction direction)
        {
            // 최대 생성 수 확인
            if (_generatedGrids.Count >= maxGeneratedPatterns)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[ProceduralMapGenerator] Max pattern limit reached ({maxGeneratedPatterns})");
                return false;
            }

            // 현재 그리드의 패턴 가져오기
            if (!_generatedGrids.TryGetValue(currentGrid, out string currentPatternID))
            {
                Debug.LogWarning($"[ProceduralMapGenerator] Current grid {currentGrid} not found in generated grids");
                return false;
            }

            // 다음 그리드 위치 계산
            Vector2Int nextGrid = GetNextGridOffset(currentGrid, direction);

            // 이미 생성된 그리드인지 확인
            if (_generatedGrids.ContainsKey(nextGrid))
            {
                if (showDebugLogs)
                    Debug.Log($"[ProceduralMapGenerator] Grid {nextGrid} already exists, skipping");
                return false;
            }

            // 연결 가능한 다음 패턴 선택
            string nextPatternID = GetValidNextPattern(currentPatternID, direction);
            if (string.IsNullOrEmpty(nextPatternID))
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[ProceduralMapGenerator] No valid next pattern for {currentPatternID} in direction {direction}");
                return false;
            }

            // 패턴 로드
            try
            {
                var instance = await streamingManager.LoadPattern(nextPatternID, nextGrid);
                if (instance != null)
                {
                    _generatedGrids[nextGrid] = nextPatternID;

                    if (showDebugLogs)
                        Debug.Log($"[ProceduralMapGenerator] Expanded to {direction}: {nextPatternID} at {nextGrid}");

                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProceduralMapGenerator] Failed to expand: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 카메라 주변 자동 확장
        /// </summary>
        private async void CheckAndExpandAroundCamera(Vector3 cameraPosition)
        {
            if (_generatedGrids.Count == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[ProceduralMapGenerator] No generated grids to expand from");
                return;
            }

            // 카메라와 가까운 그리드들 찾기
            var nearbyGrids = FindNearbyGrids(cameraPosition, expansionDistance * 2f);

            foreach (var grid in nearbyGrids)
            {
                // 각 방향으로 확장 필요성 확인
                foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
                {
                    Vector2Int nextGrid = GetNextGridOffset(grid, direction);

                    // 이미 존재하면 스킵
                    if (_generatedGrids.ContainsKey(nextGrid))
                        continue;

                    // 카메라가 경계에 가까운지 확인
                    if (IsCameraNearBoundary(cameraPosition, grid, direction, expansionDistance))
                    {
                        // 확장 시도
                        await ExpandToDirection(grid, direction);
                    }
                }
            }
        }

        /// <summary>
        /// 연결 규칙 기반 다음 패턴 선택
        /// </summary>
        private string GetValidNextPattern(string currentPatternID, Direction direction)
        {
            var currentPattern = patternRegistry.GetPattern(currentPatternID);
            if (currentPattern == null)
            {
                Debug.LogWarning($"[ProceduralMapGenerator] Pattern not found: {currentPatternID}");
                return null;
            }

            // 해당 방향의 연결 지점 찾기 (FindIndex 사용)
            int connectionIndex = currentPattern.Connections.FindIndex(c => c.Direction == direction && c.IsActive);

            // 연결 지점이 없거나 ValidNextPatterns가 비어있으면 같은 타입의 랜덤 패턴
            if (connectionIndex < 0)
            {
                if (showDebugLogs)
                    Debug.Log($"[ProceduralMapGenerator] No active connection for {direction}, using random pattern of same type");

                var randomPattern = patternRegistry.GetRandomPattern(currentPattern.Type);
                return randomPattern?.PatternID;
            }

            var connection = currentPattern.Connections[connectionIndex];

            // ValidNextPatterns가 null이거나 비어있으면 같은 타입의 랜덤 패턴
            if (connection.ValidNextPatterns == null || connection.ValidNextPatterns.Count == 0)
            {
                if (showDebugLogs)
                    Debug.Log($"[ProceduralMapGenerator] No valid next patterns for {direction}, using random pattern of same type");

                var randomPattern = patternRegistry.GetRandomPattern(currentPattern.Type);
                return randomPattern?.PatternID;
            }

            // 연결 가능한 패턴 중 랜덤 선택
            int randomIndex = Random.Range(0, connection.ValidNextPatterns.Count);
            return connection.ValidNextPatterns[randomIndex];
        }

        /// <summary>
        /// 다음 그리드 오프셋 계산
        /// </summary>
        private Vector2Int GetNextGridOffset(Vector2Int currentGrid, Direction direction)
        {
            return direction switch
            {
                Direction.North => currentGrid + Vector2Int.up,
                Direction.South => currentGrid + Vector2Int.down,
                Direction.East => currentGrid + Vector2Int.right,
                Direction.West => currentGrid + Vector2Int.left,
                _ => currentGrid
            };
        }

        /// <summary>
        /// 플레이어 근처 그리드 찾기
        /// </summary>
        private List<Vector2Int> FindNearbyGrids(Vector3 playerPosition, float distance)
        {
            var nearbyGrids = new List<Vector2Int>();

            foreach (var grid in _generatedGrids.Keys)
            {
                var pattern = patternRegistry.GetPattern(_generatedGrids[grid]);
                if (pattern == null) continue;

                // 그리드 중심 위치 계산
                Vector3 gridCenter = new Vector3(
                    grid.x * pattern.WorldSize.x + pattern.WorldSize.x * 0.5f,
                    grid.y * pattern.WorldSize.y + pattern.WorldSize.y * 0.5f,
                    0
                );

                // 거리 확인
                if (Vector3.Distance(playerPosition, gridCenter) <= distance)
                {
                    nearbyGrids.Add(grid);
                }
            }

            return nearbyGrids;
        }

        /// <summary>
        /// 카메라가 그리드 경계에 가까운지 확인
        /// </summary>
        private bool IsCameraNearBoundary(Vector3 cameraPosition, Vector2Int grid, Direction direction, float threshold)
        {
            var pattern = patternRegistry.GetPattern(_generatedGrids[grid]);
            if (pattern == null) return false;

            // 그리드의 월드 위치
            Vector3 gridWorldPos = new Vector3(
                grid.x * pattern.WorldSize.x,
                grid.y * pattern.WorldSize.y,
                0
            );

            // 방향별 경계 확인
            switch (direction)
            {
                case Direction.North:
                    float northBoundary = gridWorldPos.y + pattern.WorldSize.y;
                    return cameraPosition.y > northBoundary - threshold;

                case Direction.South:
                    float southBoundary = gridWorldPos.y;
                    return cameraPosition.y < southBoundary + threshold;

                case Direction.East:
                    float eastBoundary = gridWorldPos.x + pattern.WorldSize.x;
                    return cameraPosition.x > eastBoundary - threshold;

                case Direction.West:
                    float westBoundary = gridWorldPos.x;
                    return cameraPosition.x < westBoundary + threshold;
            }

            return false;
        }

        /// <summary>
        /// 패턴 키 파싱 (TilemapStreamingManager와 동일한 형식)
        /// </summary>
        private bool TryParsePatternKey(string key, out string patternID, out Vector2Int gridOffset)
        {
            patternID = null;
            gridOffset = Vector2Int.zero;

            var parts = key.Split('_');
            if (parts.Length < 3) return false;

            // 마지막 두 부분이 그리드 오프셋
            if (!int.TryParse(parts[parts.Length - 2], out int x)) return false;
            if (!int.TryParse(parts[parts.Length - 1], out int y)) return false;

            gridOffset = new Vector2Int(x, y);

            // 나머지는 PatternID
            patternID = string.Join("_", parts.Take(parts.Length - 2));
            return true;
        }

        /// <summary>
        /// 생성된 모든 패턴 클리어
        /// </summary>
        public void ClearGeneratedPatterns()
        {
            _generatedGrids.Clear();
            _seedGrids.Clear();

            if (showDebugLogs)
                Debug.Log("[ProceduralMapGenerator] All generated patterns cleared");
        }

        /// <summary>
        /// 생성된 패턴 수
        /// </summary>
        public int GeneratedPatternCount => _generatedGrids.Count;

        /// <summary>
        /// 특정 그리드가 생성되었는지 확인
        /// </summary>
        public bool IsGridGenerated(Vector2Int gridOffset)
        {
            return _generatedGrids.ContainsKey(gridOffset);
        }

        /// <summary>
        /// 생성된 모든 그리드 목록
        /// </summary>
        public List<Vector2Int> GetAllGeneratedGrids()
        {
            return new List<Vector2Int>(_generatedGrids.Keys);
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || patternRegistry == null) return;

            // 생성된 그리드 시각화
            foreach (var kvp in _generatedGrids)
            {
                var pattern = patternRegistry.GetPattern(kvp.Value);
                if (pattern == null) continue;

                Vector3 worldPos = new Vector3(
                    kvp.Key.x * pattern.WorldSize.x,
                    kvp.Key.y * pattern.WorldSize.y,
                    0
                );

                Vector3 size = new Vector3(pattern.WorldSize.x, pattern.WorldSize.y, 0);

                // 시드 패턴은 녹색, 생성된 패턴은 파란색
                Gizmos.color = _seedGrids.Contains(kvp.Key) ? Color.green : Color.blue;
                Gizmos.DrawWireCube(worldPos + size * 0.5f, size);

                // 그리드 좌표 표시
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    worldPos + size * 0.5f,
                    $"{kvp.Key}\n{kvp.Value}",
                    new GUIStyle { normal = { textColor = Color.white } }
                );
#endif
            }

            // 카메라 위치 표시
            if (targetCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_cameraPosition, 2f);

                // 확장 거리 표시
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(_cameraPosition, expansionDistance);
            }
        }

        #endregion
    }
}
