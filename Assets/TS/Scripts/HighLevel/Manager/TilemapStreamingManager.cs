using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TS.LowLevel.Data.Config;

namespace TS.HighLevel.Manager
{
    /// <summary>
    /// 타일맵 패턴 스트리밍 매니저
    /// Addressables 기반 동적 로딩/언로딩 시스템 (카메라 기반)
    /// </summary>
    public class TilemapStreamingManager : BaseManager<TilemapStreamingManager>
    {
        [Header("Registry")]
        [SerializeField] private TilemapPatternRegistry patternRegistry;

        [Header("Camera Reference")]
        [SerializeField] private Camera targetCamera; // 추적할 카메라 (Orthographic)

        [Header("Streaming Settings")]
        [SerializeField] private int maxLoadedPatterns = 9; // 3x3 그리드 최대
        [SerializeField] private float updateInterval = 0.5f; // 업데이트 주기 (초)
        [SerializeField] private bool enableAutoStreaming = true; // 자동 스트리밍 활성화
        [SerializeField] private float loadBufferSize = 20f; // 카메라 가시 영역 외곽 버퍼 (로드 여유 공간)

        [Header("Performance")]
        [SerializeField] private int maxConcurrentLoads = 3; // 동시 로딩 최대 수
        [SerializeField] private bool usePriority = true; // 우선순위 기반 로딩

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private Color debugColor = Color.green;
        [SerializeField] private bool showCameraBounds = true; // 카메라 영역 표시

        // 로드된 패턴 캐시
        private Dictionary<string, LoadedPattern> _loadedPatterns = new Dictionary<string, LoadedPattern>();

        // 로딩 대기열
        private Queue<LoadRequest> _loadQueue = new Queue<LoadRequest>();
        private HashSet<string> _loadingKeys = new HashSet<string>(); // 현재 로딩 중인 키

        // 카메라 추적
        private Vector3 _cameraPosition;
        private float _lastCameraSize; // 이전 카메라 크기 (줌 변경 감지)
        private float _lastUpdateTime;

        // 초기화 상태
        private bool _isInitialized = false;

        #region Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized || !enableAutoStreaming) return;

            // 카메라 검증
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            // 카메라 위치 업데이트
            _cameraPosition = targetCamera.transform.position;

            // 줌 변경 감지 (Orthographic size 변경)
            if (targetCamera.orthographic && Mathf.Abs(targetCamera.orthographicSize - _lastCameraSize) > 0.1f)
            {
                _lastCameraSize = targetCamera.orthographicSize;
                if (showDebugInfo)
                    Debug.Log($"[TilemapStreamingManager] Camera zoom changed: {_lastCameraSize}");

                // 줌 변경 시 즉시 스트리밍 업데이트
                UpdateStreamingByCameraView().Forget();
            }

            // 주기적 업데이트
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                UpdateStreamingByCameraView().Forget();
                ProcessLoadQueue().Forget();
                _lastUpdateTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            // 모든 패턴 언로드
            UnloadAllPatterns().Forget();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 매니저 초기화
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // 레지스트리 검증
            if (patternRegistry == null)
            {
                Debug.LogError("[TilemapStreamingManager] PatternRegistry is not assigned!");
                return;
            }

            // 카메라 참조 설정
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    Debug.LogWarning("[TilemapStreamingManager] No camera assigned or found. Auto-streaming disabled.");
                    enableAutoStreaming = false;
                }
            }

            // Orthographic 검증
            if (targetCamera != null && !targetCamera.orthographic)
            {
                Debug.LogWarning("[TilemapStreamingManager] Camera is not Orthographic! Streaming may not work correctly.");
            }

            // 카메라 초기 상태 저장
            if (targetCamera != null)
            {
                _cameraPosition = targetCamera.transform.position;
                _lastCameraSize = targetCamera.orthographicSize;
            }

            // 레지스트리 초기화
            patternRegistry.Initialize();

            _isInitialized = true;
            _lastUpdateTime = Time.time;

            if (showDebugInfo)
            {
                Debug.Log($"[TilemapStreamingManager] Initialized. Camera: {targetCamera?.name}, MaxPatterns: {maxLoadedPatterns}, UpdateInterval: {updateInterval}s");
            }
        }

        /// <summary>
        /// 카메라 참조 설정 (외부에서 호출 가능)
        /// </summary>
        public void SetCamera(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogError("[TilemapStreamingManager] Cannot set null camera!");
                return;
            }

            if (!camera.orthographic)
            {
                Debug.LogWarning("[TilemapStreamingManager] Camera is not Orthographic!");
            }

            targetCamera = camera;
            _cameraPosition = camera.transform.position;
            _lastCameraSize = camera.orthographicSize;

            if (showDebugInfo)
                Debug.Log($"[TilemapStreamingManager] Camera set: {camera.name}");
        }

        #endregion

        #region Pattern Loading

        /// <summary>
        /// SubScene 초기 패턴 로드
        /// </summary>
        public async UniTask LoadInitialPatterns(string subSceneName)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[TilemapStreamingManager] Not initialized!");
                return;
            }

            var patterns = patternRegistry.GetPatternsForSubScene(subSceneName);

            if (patterns.Count == 0)
            {
                Debug.LogWarning($"[TilemapStreamingManager] No initial patterns found for SubScene: {subSceneName}");
                return;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[TilemapStreamingManager] Loading {patterns.Count} initial patterns for {subSceneName}");
            }

            // 모든 초기 패턴 로드
            var loadTasks = new List<UniTask>();
            for (int i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                if (pattern == null) continue;

                // 첫 번째 패턴은 원점(0,0), 나머지는 순차 배치
                var gridOffset = i == 0 ? Vector2Int.zero : new Vector2Int(i, 0);
                loadTasks.Add(LoadPattern(pattern.PatternID, gridOffset));
            }

            // 모든 로딩 완료 대기
            await UniTask.WhenAll(loadTasks);

            if (showDebugInfo)
            {
                Debug.Log($"[TilemapStreamingManager] Initial patterns loaded: {_loadedPatterns.Count}");
            }
        }

        /// <summary>
        /// 패턴 로드
        /// </summary>
        public async UniTask<GameObject> LoadPattern(string patternID, Vector2Int gridOffset)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[TilemapStreamingManager] Not initialized!");
                return null;
            }

            var key = GetPatternKey(patternID, gridOffset);

            // 이미 로드된 패턴 체크
            if (_loadedPatterns.ContainsKey(key))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[TilemapStreamingManager] Pattern already loaded: {key}");
                }
                return _loadedPatterns[key].TilemapInstance;
            }

            // 현재 로딩 중인지 체크
            if (_loadingKeys.Contains(key))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[TilemapStreamingManager] Pattern is currently loading: {key}");
                }
                // 로딩 완료 대기
                await UniTask.WaitUntil(() => _loadedPatterns.ContainsKey(key) || !_loadingKeys.Contains(key));
                return _loadedPatterns.ContainsKey(key) ? _loadedPatterns[key].TilemapInstance : null;
            }

            // 최대 로드 수 체크
            if (_loadedPatterns.Count >= maxLoadedPatterns)
            {
                Debug.LogWarning($"[TilemapStreamingManager] Max loaded patterns reached ({maxLoadedPatterns}). Unloading distant patterns...");
                await UnloadDistantPatterns(_cameraPosition, 1);
            }

            // 패턴 데이터 가져오기
            var patternData = patternRegistry.GetPattern(patternID);
            if (patternData == null)
            {
                Debug.LogError($"[TilemapStreamingManager] Pattern not found: {patternID}");
                return null;
            }

            // Addressable 참조 검증
            if (patternData.TilemapPrefab == null || !patternData.TilemapPrefab.RuntimeKeyIsValid())
            {
                Debug.LogError($"[TilemapStreamingManager] Invalid Addressable reference for pattern: {patternID}");
                return null;
            }

            // 로딩 시작 표시
            _loadingKeys.Add(key);

            try
            {
                // Addressables로 프리팹 인스턴스화
                var handle = Addressables.InstantiateAsync(patternData.TilemapPrefab);
                var instance = await handle.Task;

                if (instance == null)
                {
                    Debug.LogError($"[TilemapStreamingManager] Failed to instantiate pattern: {patternID}");
                    _loadingKeys.Remove(key);
                    return null;
                }

                // 월드 위치 계산 및 설정
                var worldOffset = new Vector3(
                    gridOffset.x * patternData.WorldSize.x,
                    gridOffset.y * patternData.WorldSize.y,
                    0
                );
                instance.transform.position = worldOffset;
                instance.name = $"TilemapPattern_{key}";

                // 로드된 패턴 등록
                var loadedPattern = new LoadedPattern
                {
                    PatternID = patternID,
                    GridOffset = gridOffset,
                    PatternData = patternData,
                    TilemapInstance = instance,
                    Handle = handle,
                    LoadTime = Time.time
                };

                _loadedPatterns[key] = loadedPattern;
                _loadingKeys.Remove(key);

                if (showDebugInfo)
                {
                    Debug.Log($"[TilemapStreamingManager] Pattern loaded: {key} at {worldOffset}");
                }

                return instance;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TilemapStreamingManager] Exception loading pattern {patternID}: {ex.Message}");
                _loadingKeys.Remove(key);
                return null;
            }
        }

        #endregion

        #region Pattern Unloading

        /// <summary>
        /// 패턴 언로드
        /// </summary>
        public async UniTask UnloadPattern(string patternID, Vector2Int gridOffset)
        {
            var key = GetPatternKey(patternID, gridOffset);

            if (!_loadedPatterns.TryGetValue(key, out var loaded))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[TilemapStreamingManager] Pattern not loaded: {key}");
                }
                return;
            }

            try
            {
                // GameObject 제거
                if (loaded.TilemapInstance != null)
                {
                    Addressables.ReleaseInstance(loaded.Handle);
                }

                _loadedPatterns.Remove(key);

                if (showDebugInfo)
                {
                    Debug.Log($"[TilemapStreamingManager] Pattern unloaded: {key}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TilemapStreamingManager] Exception unloading pattern {key}: {ex.Message}");
            }

            await UniTask.Yield();
        }

        /// <summary>
        /// 모든 패턴 언로드
        /// </summary>
        public async UniTask UnloadAllPatterns()
        {
            var keys = _loadedPatterns.Keys.ToList();

            foreach (var key in keys)
            {
                var loaded = _loadedPatterns[key];
                await UnloadPattern(loaded.PatternID, loaded.GridOffset);
            }

            _loadedPatterns.Clear();

            if (showDebugInfo)
            {
                Debug.Log("[TilemapStreamingManager] All patterns unloaded");
            }
        }

        /// <summary>
        /// 거리 기반 먼 패턴 언로드
        /// </summary>
        public async UniTask UnloadDistantPatterns(Vector3 playerPosition, int countToUnload = 1)
        {
            var distantPatterns = _loadedPatterns.Values
                .OrderByDescending(p => GetDistanceToPattern(p, playerPosition))
                .Take(countToUnload)
                .ToList();

            foreach (var pattern in distantPatterns)
            {
                await UnloadPattern(pattern.PatternID, pattern.GridOffset);
            }
        }

        #endregion

        #region Auto Streaming

        /// <summary>
        /// 카메라 가시 영역 기반 자동 스트리밍
        /// </summary>
        public async UniTask UpdateStreamingByCameraView()
        {
            if (!_isInitialized || targetCamera == null) return;

            _cameraPosition = targetCamera.transform.position;

            // 카메라 가시 영역 계산 (Orthographic)
            var cameraBounds = GetCameraBounds();

            // 언로드할 패턴 찾기 (카메라 가시 영역 + 버퍼 벗어난 패턴)
            var toUnload = _loadedPatterns.Values
                .Where(p => ShouldUnloadByCamera(p, cameraBounds))
                .ToList();

            // 언로드 실행
            foreach (var pattern in toUnload)
            {
                await UnloadPattern(pattern.PatternID, pattern.GridOffset);
            }

            if (showDebugInfo && toUnload.Count > 0)
            {
                Debug.Log($"[TilemapStreamingManager] Auto-unloaded {toUnload.Count} patterns outside camera view");
            }
        }

        /// <summary>
        /// 카메라 가시 영역 계산 (Orthographic Camera)
        /// </summary>
        private Bounds GetCameraBounds()
        {
            if (targetCamera == null || !targetCamera.orthographic)
                return new Bounds(_cameraPosition, Vector3.one * 100f);

            // Orthographic 카메라의 가시 영역 크기 계산
            float height = targetCamera.orthographicSize * 2f;
            float width = height * targetCamera.aspect;

            // 버퍼 추가
            var size = new Vector3(width + loadBufferSize * 2f, height + loadBufferSize * 2f, 0f);

            return new Bounds(_cameraPosition, size);
        }

        /// <summary>
        /// 카메라 위치 반환 (외부 접근용)
        /// </summary>
        public Vector3 GetCameraPosition()
        {
            return _cameraPosition;
        }

        #endregion

        #region Queue Processing

        /// <summary>
        /// 로딩 대기열 처리
        /// </summary>
        private async UniTask ProcessLoadQueue()
        {
            if (_loadQueue.Count == 0) return;

            int processedCount = 0;

            while (_loadQueue.Count > 0 && processedCount < maxConcurrentLoads)
            {
                var request = _loadQueue.Dequeue();
                await LoadPattern(request.PatternID, request.GridOffset);
                processedCount++;
            }
        }

        /// <summary>
        /// 로딩 요청 큐에 추가
        /// </summary>
        public void EnqueueLoadRequest(string patternID, Vector2Int gridOffset, int priority = 50)
        {
            var request = new LoadRequest
            {
                PatternID = patternID,
                GridOffset = gridOffset,
                Priority = priority,
                RequestTime = Time.time
            };

            _loadQueue.Enqueue(request);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 패턴 키 생성 (PatternID_GridX_GridY)
        /// </summary>
        private string GetPatternKey(string patternID, Vector2Int offset)
        {
            return $"{patternID}_{offset.x}_{offset.y}";
        }

        /// <summary>
        /// 패턴까지의 거리 계산
        /// </summary>
        private float GetDistanceToPattern(LoadedPattern pattern, Vector3 position)
        {
            var patternCenter = new Vector3(
                pattern.GridOffset.x * pattern.PatternData.WorldSize.x + pattern.PatternData.WorldSize.x * 0.5f,
                pattern.GridOffset.y * pattern.PatternData.WorldSize.y + pattern.PatternData.WorldSize.y * 0.5f,
                0
            );

            return Vector3.Distance(position, patternCenter);
        }

        /// <summary>
        /// 패턴 중심 위치 계산
        /// </summary>
        private Vector3 GetPatternCenter(LoadedPattern pattern)
        {
            return new Vector3(
                pattern.GridOffset.x * pattern.PatternData.WorldSize.x + pattern.PatternData.WorldSize.x * 0.5f,
                pattern.GridOffset.y * pattern.PatternData.WorldSize.y + pattern.PatternData.WorldSize.y * 0.5f,
                0
            );
        }

        /// <summary>
        /// 카메라 가시 영역 기반 언로드 여부 확인
        /// </summary>
        private bool ShouldUnloadByCamera(LoadedPattern pattern, Bounds cameraBounds)
        {
            var patternCenter = GetPatternCenter(pattern);

            // 패턴 중심이 카메라 가시 영역(버퍼 포함) 밖에 있으면 언로드
            return !cameraBounds.Contains(patternCenter);
        }

        /// <summary>
        /// 패턴을 언로드해야 하는지 확인 (거리 기반 - 호환성 유지)
        /// </summary>
        private bool ShouldUnload(LoadedPattern pattern, Vector3 position)
        {
            var distance = GetDistanceToPattern(pattern, position);
            return distance > pattern.PatternData.UnloadDistance;
        }

        #endregion

        #region Debug & Utility

        /// <summary>
        /// 현재 로드된 패턴 수
        /// </summary>
        public int LoadedPatternCount => _loadedPatterns.Count;

        /// <summary>
        /// 로드된 모든 패턴 키 목록
        /// </summary>
        public List<string> GetLoadedPatternKeys()
        {
            return new List<string>(_loadedPatterns.Keys);
        }

        /// <summary>
        /// 패턴이 로드되었는지 확인
        /// </summary>
        public bool IsPatternLoaded(string patternID, Vector2Int gridOffset)
        {
            var key = GetPatternKey(patternID, gridOffset);
            return _loadedPatterns.ContainsKey(key);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !_isInitialized) return;

            // 로드된 패턴 경계 그리기
            Gizmos.color = debugColor;
            foreach (var loaded in _loadedPatterns.Values)
            {
                var center = new Vector3(
                    loaded.GridOffset.x * loaded.PatternData.WorldSize.x + loaded.PatternData.WorldSize.x * 0.5f,
                    loaded.GridOffset.y * loaded.PatternData.WorldSize.y + loaded.PatternData.WorldSize.y * 0.5f,
                    0
                );

                var size = new Vector3(loaded.PatternData.WorldSize.x, loaded.PatternData.WorldSize.y, 0);
                Gizmos.DrawWireCube(center, size);
            }

            // 카메라 위치 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_cameraPosition, 3f);

            // 카메라 가시 영역 표시
            if (showCameraBounds && targetCamera != null && targetCamera.orthographic)
            {
                var cameraBounds = GetCameraBounds();

                // 내부 가시 영역 (버퍼 제외)
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f); // Cyan
                float height = targetCamera.orthographicSize * 2f;
                float width = height * targetCamera.aspect;
                Gizmos.DrawWireCube(_cameraPosition, new Vector3(width, height, 0f));

                // 버퍼 포함 영역
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow
                Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// 로드된 패턴 정보
        /// </summary>
        private class LoadedPattern
        {
            public string PatternID;
            public Vector2Int GridOffset;
            public TilemapPatternData PatternData;
            public GameObject TilemapInstance;
            public AsyncOperationHandle<GameObject> Handle;
            public float LoadTime;
        }

        /// <summary>
        /// 로딩 요청 정보
        /// </summary>
        private struct LoadRequest
        {
            public string PatternID;
            public Vector2Int GridOffset;
            public int Priority;
            public float RequestTime;
        }

        #endregion
    }
}
