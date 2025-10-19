using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Scenes;
using Unity.Entities;

/// <summary>
/// 타일맵 패턴 스트리밍 매니저
/// 포트 기반 동적 로딩/언로딩 시스템 (카메라 기반)
/// </summary>
public class TilemapStreamingManager : BaseManager<TilemapStreamingManager>
{
    #region Constants

    private const float CAMERA_ZOOM_THRESHOLD = 0.1f;
    private const float DEFAULT_CAMERA_BOUNDS_SIZE = 100f;
    private const float CAMERA_SIZE_MULTIPLIER = 2f;
    private const float DEBUG_CAMERA_SPHERE_RADIUS = 3f;

    #endregion

    #region Inspector Fields

    [Header("Registry")]
    [SerializeField] private TilemapPatternRegistry _patternRegistry;

    [Header("Camera Reference")]
    [SerializeField] private Camera _targetCamera;

    [Header("Streaming Settings")]
    [SerializeField] private int _maxLoadedPatterns = 9;
    [SerializeField] private float _updateInterval = 0.5f;
    [SerializeField] private bool _enableAutoStreaming = false;
    [SerializeField] private float _loadBufferSize = 20f;
    [SerializeField] private float _unloadMargin = 10f; // 언로드 여유 거리 (히스테리시스)

    [Header("Performance")]
    [SerializeField] private int _maxConcurrentLoads = 3;

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private Color _debugColor = Color.green;
    [SerializeField] private bool _showCameraBounds = true;

    #endregion

    #region Private Fields

    // 패턴 캐시
    private readonly Dictionary<string, LoadedPattern> _loadedPatterns = new Dictionary<string, LoadedPattern>();
    private readonly Dictionary<string, PatternHistory> _unloadedPatternHistory = new Dictionary<string, PatternHistory>();

    // 로딩 관리
    private readonly Queue<LoadRequest> _loadQueue = new Queue<LoadRequest>();
    private readonly HashSet<string> _loadingKeys = new HashSet<string>();

    // 카메라 추적
    private Vector2 _cameraPosition;
    private float _lastCameraSize;
    private float _lastUpdateTime;

    // 초기화 상태
    private bool _isInitialized;

    private Dictionary<Vector2Int, TilemapPatternNode> mapDatas = new Dictionary<Vector2Int, TilemapPatternNode>();
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!_isInitialized || !_enableAutoStreaming || !ValidateCamera()) return;

        UpdateCameraState();

        if (HasCameraZoomChanged())
        {
            OnCameraZoomChanged();
        }

        if (ShouldUpdate())
        {
            PerformPeriodicUpdate();
        }
    }

    private void OnDestroy()
    {
        UnloadAllPatterns().Forget();
    }

    #endregion

    #region Initialization

    public void Initialize()
    {
        if (_isInitialized) return;

        LoadMapDatas();

        if (!ValidateRegistry()) return;
        if (!InitializeCamera()) return;

        _patternRegistry.Initialize();

        _isInitialized = true;
        _lastUpdateTime = Time.time;

        LogDebug($"Initialized. Camera: {_targetCamera?.name}, MaxPatterns: {_maxLoadedPatterns}, UpdateInterval: {_updateInterval}s");
    }

    public void LoadMapDatas()
    {
        // 테스트 데이터
        var basePosition = new Vector2Int(0, 0);
        var basePattern = new TilemapPatternNode("BaseTown", basePosition);

        var secondPosition = new Vector2Int(1, 0);
        var secondPattern = new TilemapPatternNode("1000002", secondPosition);

        mapDatas[basePosition] = basePattern;
        mapDatas[secondPosition] = secondPattern;
    }

    private bool ValidateRegistry()
    {
        if (_patternRegistry != null) return true;

        Debug.LogError("[TilemapStreamingManager] PatternRegistry is not assigned!");
        return false;
    }

    private bool InitializeCamera()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }

        if (_targetCamera == null)
        {
            Debug.LogWarning("[TilemapStreamingManager] No camera found. Auto-streaming disabled.");
            _enableAutoStreaming = false;
            return false;
        }

        if (!_targetCamera.orthographic)
        {
            Debug.LogWarning("[TilemapStreamingManager] Camera is not Orthographic! Streaming may not work correctly.");
        }

        SetCameraPosition(_targetCamera.transform.position);
        SetCameraSize(_targetCamera.orthographicSize);

        return true;
    }

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

        _targetCamera = camera;
        SetCameraPosition(camera.transform.position);
        SetCameraSize(camera.orthographicSize);

        LogDebug($"Camera set: {camera.name}");
    }

    public void SetCameraPosition(Vector2 position) => _cameraPosition = position;
    public void SetCameraSize(float size) => _lastCameraSize = size;
    public void SetEnableAutoStreaming(bool value) => _enableAutoStreaming = value;
    #endregion

    #region Camera Update

    private bool ValidateCamera()
    {
        if (_targetCamera != null) return true;

        _targetCamera = Camera.main;
        return _targetCamera != null;
    }

    private void UpdateCameraState()
    {
        SetCameraPosition(_targetCamera.transform.position);
    }

    private bool HasCameraZoomChanged()
    {
        return _targetCamera.orthographic &&
               Mathf.Abs(_targetCamera.orthographicSize - _lastCameraSize) > CAMERA_ZOOM_THRESHOLD;
    }

    private void OnCameraZoomChanged()
    {
        SetCameraSize(_targetCamera.orthographicSize);
        LogDebug($"Camera zoom changed: {_lastCameraSize}");
        UpdateStreamingByCameraView().Forget();
    }

    private bool ShouldUpdate()
    {
        return Time.time - _lastUpdateTime >= _updateInterval;
    }

    private void PerformPeriodicUpdate()
    {
        UpdateStreamingByCameraView().Forget();
        ProcessLoadQueue().Forget();
        _lastUpdateTime = Time.time;
    }

    #endregion

    #region Pattern Loading - Core

    public async UniTask<GameObject> LoadPattern(string patternID, Vector2Int gridOffset)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[TilemapStreamingManager] Not initialized!");
            return null;
        }

        var key = GetPatternKey(patternID, gridOffset);

        // 중복 로드 체크
        if (TryGetLoadedPattern(key, out var existingInstance))
        {
            return existingInstance;
        }

        // 로딩 중 체크
        if (IsPatternLoading(key))
        {
            return await WaitForPatternLoad(key);
        }

        // 용량 체크
        await EnsureLoadCapacity();

        // 패턴 데이터 검증
        var patternData = _patternRegistry.GetPattern(patternID);
        if (!ValidatePatternData(patternData, patternID))
        {
            return null;
        }

        // 로딩 실행
        return await LoadPatternInternal(key, patternID, gridOffset, patternData);
    }

    private bool TryGetLoadedPattern(string key, out GameObject instance)
    {
        if (_loadedPatterns.TryGetValue(key, out var loaded))
        {
            LogDebug($"Pattern already loaded: {key}");
            instance = loaded.TilemapInstance;
            return true;
        }

        instance = null;
        return false;
    }

    private bool IsPatternLoading(string key)
    {
        if (_loadingKeys.Contains(key))
        {
            LogDebug($"Pattern is currently loading: {key}");
            return true;
        }
        return false;
    }

    private async UniTask<GameObject> WaitForPatternLoad(string key)
    {
        await UniTask.WaitUntil(() => _loadedPatterns.ContainsKey(key) || !_loadingKeys.Contains(key));
        return _loadedPatterns.TryGetValue(key, out var loaded) ? loaded.TilemapInstance : null;
    }

    private async UniTask EnsureLoadCapacity()
    {
        if (_loadedPatterns.Count >= _maxLoadedPatterns)
        {
            Debug.LogWarning($"[TilemapStreamingManager] Max loaded patterns reached ({_maxLoadedPatterns}). Unloading distant patterns...");
            await UnloadDistantPatterns(_cameraPosition, 1);
        }
    }

    private bool ValidatePatternData(TilemapPatternData patternData, string patternID)
    {
        if (patternData == null)
        {
            Debug.LogError($"[TilemapStreamingManager] Pattern not found: {patternID}");
            return false;
        }

        if (patternData.TilemapPrefab == null || !patternData.TilemapPrefab.RuntimeKeyIsValid())
        {
            Debug.LogError($"[TilemapStreamingManager] Invalid Addressable reference for pattern: {patternID}");
            return false;
        }

        return true;
    }

    private async UniTask<GameObject> LoadPatternInternal(string key, string patternID, Vector2Int gridOffset, TilemapPatternData patternData)
    {
        _loadingKeys.Add(key);

        try
        {
            var instance = await InstantiateTilemap(patternData, gridOffset, key);
            if (instance == null)
            {
                _loadingKeys.Remove(key);
                return null;
            }

            var subSceneEntity = await LoadSubScene(patternData, patternID);

            RegisterLoadedPattern(key, patternID, gridOffset, patternData, instance, subSceneEntity);

            return instance;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TilemapStreamingManager] Exception loading pattern {patternID}: {ex.Message}");
            _loadingKeys.Remove(key);
            return null;
        }
    }

    private async UniTask<GameObject> InstantiateTilemap(TilemapPatternData patternData, Vector2Int gridOffset, string key)
    {
        var tileMapHandle = Addressables.InstantiateAsync(patternData.TilemapPrefab);
        var instance = await tileMapHandle.Task;

        if (instance == null)
        {
            Debug.LogError($"[TilemapStreamingManager] Failed to instantiate pattern");
            return null;
        }

        instance.transform.position = CalculatePatternCenter(gridOffset);
        instance.name = $"TilemapPattern_{key}";

        return instance;
    }

    private async UniTask<Entity> LoadSubScene(TilemapPatternData patternData, string patternID)
    {
        if (!patternData.SubScene.IsReferenceValid)
        {
            LogDebug($"No SubScene reference for pattern: {patternID}");
            return Entity.Null;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            Debug.LogWarning($"[TilemapStreamingManager] World not available for SubScene loading: {patternID}");
            return Entity.Null;
        }

        var subSceneEntity = SceneSystem.LoadSceneAsync(
            world.Unmanaged,
            patternData.SubScene,
            new SceneSystem.LoadParameters { Flags = SceneLoadFlags.LoadAdditive }
        );

        LogDebug($"SubScene loaded for pattern: {patternID} (Entity: {subSceneEntity})");

        return subSceneEntity;
    }

    private void RegisterLoadedPattern(string key, string patternID, Vector2Int gridOffset, TilemapPatternData patternData, GameObject instance, Entity subSceneEntity)
    {
        var loadedPattern = new LoadedPattern
        {
            PatternID = patternID,
            GridOffset = gridOffset,
            PatternData = patternData,
            TilemapInstance = instance,
            SubSceneEntity = subSceneEntity,
            LoadTime = Time.time
        };

        _loadedPatterns[key] = loadedPattern;
        _loadingKeys.Remove(key);
        _unloadedPatternHistory.Remove(key);

        LogDebug($"Pattern loaded: {key}");
    }

    #endregion

    #region Pattern Unloading

    public async UniTask UnloadPatternNode(TilemapPatternNode node)
    {
        if (node == null) return;

        await UnloadPattern(node.PatternID, node.WorldGridPosition);

        node.IsLoaded = false;

        LogDebug($"Node unloaded: {node.PatternID}");
    }

    public async UniTask UnloadPattern(string patternID, Vector2Int gridOffset)
    {
        var key = GetPatternKey(patternID, gridOffset);

        if (!_loadedPatterns.TryGetValue(key, out var loaded))
        {
            LogDebug($"Pattern not loaded: {key}");
            return;
        }

        try
        {
            UnloadTilemap(loaded);
            SaveToHistory(loaded, key);

            _loadedPatterns.Remove(key);

            LogDebug($"Pattern unloaded and saved to history: {key}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TilemapStreamingManager] Exception unloading pattern {key}: {ex.Message}");
        }

        await UniTask.Yield();
    }

    private async UniTask UnloadSubScene(LoadedPattern loaded, string key)
    {
        if (loaded.SubSceneEntity == Entity.Null) return;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        SceneSystem.UnloadScene(
            world.Unmanaged,
            loaded.SubSceneEntity,
            SceneSystem.UnloadParameters.DestroyMetaEntities
        );

        LogDebug($"SubScene unloaded for pattern: {key} (Entity: {loaded.SubSceneEntity})");
    }

    private void UnloadTilemap(LoadedPattern loaded)
    {
        if (loaded.TilemapInstance != null)
        {
            Object.Destroy(loaded.TilemapInstance);
        }
    }

    private void SaveToHistory(LoadedPattern loaded, string key)
    {
        _unloadedPatternHistory[key] = new PatternHistory
        {
            PatternID = loaded.PatternID,
            GridOffset = loaded.GridOffset,
            UnloadTime = Time.time
        };
    }

    public async UniTask UnloadAllPatterns()
    {
        var patternsToUnload = new List<(string patternID, Vector2Int gridOffset)>();

        foreach (var loaded in _loadedPatterns.Values)
        {
            patternsToUnload.Add((loaded.PatternID, loaded.GridOffset));
        }

        foreach (var (patternID, gridOffset) in patternsToUnload)
        {
            await UnloadPattern(patternID, gridOffset);
        }

        _loadedPatterns.Clear();

        LogDebug("All patterns unloaded");
    }

    public async UniTask UnloadDistantPatterns(Vector2 position, int countToUnload = 1)
    {
        var distantPatterns = FindDistantPatterns(position, countToUnload);

        foreach (var pattern in distantPatterns)
        {
            await UnloadPattern(pattern.PatternID, pattern.GridOffset);
        }
    }

    private List<LoadedPattern> FindDistantPatterns(Vector2 position, int count)
    {
        var patterns = new List<LoadedPattern>(_loadedPatterns.Values);
        patterns.Sort((a, b) =>
        {
            var distA = GetDistanceToPattern(a, position);
            var distB = GetDistanceToPattern(b, position);
            return distB.CompareTo(distA);
        });

        var result = new List<LoadedPattern>();
        for (int i = 0; i < count && i < patterns.Count; i++)
        {
            result.Add(patterns[i]);
        }

        return result;
    }

    #endregion

    #region Auto Streaming

    /// <summary>
    /// 카메라 가시 영역 기반 자동 스트리밍 (히스테리시스 적용)
    /// 로드 범위: 카메라 + loadBufferSize
    /// 언로드 범위: 카메라 + loadBufferSize + unloadMargin
    /// 이를 통해 경계에서 로드/언로드 반복을 방지
    /// </summary>
    public async UniTask UpdateStreamingByCameraView()
    {
        if (!_isInitialized || _targetCamera == null || !_enableAutoStreaming) return;

        SetCameraPosition(_targetCamera.transform.position);

        var loadRect = GetCameraRect();
        var unloadRect = GetCameraUnloadRect();

        await LoadPatternsInCameraView(loadRect);
        await UnloadPatternsOutsideCameraView(unloadRect);
    }

    private async UniTask LoadPatternsInCameraView(Rect cameraRect)
    {
        // 로드된 패턴이 없으면 히스토리에서 복구
        if (_loadedPatterns.Count == 0)
        {
            await LoadPatternFromHistory(cameraRect);
            return;
        }

        var patternsToLoad = FindNeighborPatternsToLoad(cameraRect);

        foreach (var (patternID, offset) in patternsToLoad)
        {
            await LoadPattern(patternID, offset);
        }

        if (patternsToLoad.Count > 0)
        {
            LogDebug($"Auto-loaded {patternsToLoad.Count} neighbor patterns in camera view");
        }
    }

    private List<(string patternID, Vector2Int offset)> FindNeighborPatternsToLoad(Rect cameraRect)
    {
        var result = new List<(string, Vector2Int)>();
        var check = new HashSet<string>();

        foreach (var loaded in _loadedPatterns.Values)
        {
            foreach (var connection in loaded.PatternData.Connections)
            {
                var neighborOffset = CalculateNeighborOffset(loaded.GridOffset, connection.Direction);
                // var key = GetPatternKey(connection.LinkedPatternID, neighborOffset);

                // if (check.Contains(key) || IsPatternLoaded(connection.LinkedPatternID, neighborOffset))
                //     continue;

                // check.Add(key);

                // var pattern = patternRegistry.GetPattern(connection.LinkedPatternID);
                // if (pattern == null)
                // {
                //     LogDebug($"Linked pattern not found in registry: {connection.LinkedPatternID}");
                //     continue;
                // }

                // var patternCenter = CalculatePatternCenter(pattern, neighborOffset);
                // if (cameraRect.Contains(patternCenter))
                // {
                //     result.Add((connection.LinkedPatternID, neighborOffset));
                // }
            }
        }

        return result;
    }

    private async UniTask UnloadPatternsOutsideCameraView(Rect cameraRect)
    {
        var patternsToUnload = new List<LoadedPattern>();

        foreach (var loaded in _loadedPatterns.Values)
        {
            if (ShouldUnloadByCamera(loaded, cameraRect))
            {
                patternsToUnload.Add(loaded);
            }
        }

        foreach (var pattern in patternsToUnload)
        {
            await UnloadPattern(pattern.PatternID, pattern.GridOffset);
        }

        if (patternsToUnload.Count > 0)
        {
            LogDebug($"Auto-unloaded {patternsToUnload.Count} patterns outside camera view");
        }
    }

    #endregion

    #region History Recovery

    private async UniTask LoadPatternFromHistory(Rect cameraRect)
    {
        if (_unloadedPatternHistory.Count == 0)
        {
            await LoadInitialPatternFallback(cameraRect);
            return;
        }

        var closestHistory = FindClosestHistoryInBounds(in cameraRect);

        if (closestHistory != null)
        {
            LogDebug($"Loading pattern from history: {closestHistory.PatternID} at {closestHistory.GridOffset}");
            await LoadPattern(closestHistory.PatternID, closestHistory.GridOffset);
        }
    }

    private async UniTask LoadInitialPatternFallback(Rect cameraRect)
    {
        // 좌상단
        float gridHalfWidth = _patternRegistry.GridSize.x * 0.5f;
        float gridHalfHeight = _patternRegistry.GridSize.y * 0.5f;

        for (float y = cameraRect.yMin; y <= cameraRect.yMax; y += _patternRegistry.GridSize.y)
        {
            for (float x = cameraRect.xMin; x <= cameraRect.xMax; x += _patternRegistry.GridSize.x)
            {
                int integerX = (int) ((cameraRect.xMin + gridHalfWidth) / _patternRegistry.GridSize.x);
                int integerY = (int) ((cameraRect.yMax + gridHalfHeight) / _patternRegistry.GridSize.y);
                var position = new Vector2Int(integerX, integerY);

                if (mapDatas.TryGetValue(position, out var node))
                    await LoadPattern(node.PatternID, position);
            }
        }
    }

    private PatternHistory FindClosestHistoryInBounds(in Rect cameraRect)
    {
        PatternHistory closest = null;
        float closestDistance = float.MaxValue;

        foreach (var history in _unloadedPatternHistory.Values)
        {
            var pattern = _patternRegistry.GetPattern(history.PatternID);
            if (pattern == null) continue;

            var patternRect = CalculatePatternRect(history.GridOffset);

            if (Utility.Geometry.IsAABBOverlap(in cameraRect, in patternRect))
            {
                float distance = Vector3.SqrMagnitude(_cameraPosition - patternRect.center);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = history;
                }
            }
        }

        return closest;
    }

    #endregion

    #region Queue Processing

    private async UniTask ProcessLoadQueue()
    {
        if (_loadQueue.Count == 0) return;

        int processedCount = 0;

        while (_loadQueue.Count > 0 && processedCount < _maxConcurrentLoads)
        {
            var request = _loadQueue.Dequeue();
            await LoadPattern(request.PatternID, request.GridOffset);
            processedCount++;
        }
    }

    public void EnqueueLoadRequest(string patternID, Vector2Int gridOffset, int priority = 50)
    {
        _loadQueue.Enqueue(new LoadRequest
        {
            PatternID = patternID,
            GridOffset = gridOffset,
            Priority = priority,
            RequestTime = Time.time
        });
    }

    #endregion

    #region Calculation Helpers

    private Vector2Int CalculateNeighborOffset(Vector2Int currentOffset, PatternDirection direction)
    {
        return direction switch
        {
            PatternDirection.Top => new Vector2Int(currentOffset.x, currentOffset.y + 1),
            PatternDirection.Bottom => new Vector2Int(currentOffset.x, currentOffset.y - 1),
            PatternDirection.Left => new Vector2Int(currentOffset.x - 1, currentOffset.y),
            PatternDirection.Right => new Vector2Int(currentOffset.x + 1, currentOffset.y),
            _ => currentOffset
        };
    }

    private Vector2 CalculatePatternCenter(Vector2Int gridOffset)
    {
        return new Vector2(
            gridOffset.x * _patternRegistry.GridSize.x,
            gridOffset.y * _patternRegistry.GridSize.y
        );
    }

    private Rect CalculatePatternRect(Vector2Int gridOffset)
    {
        float x = gridOffset.x * _patternRegistry.GridSize.x - _patternRegistry.GridSize.x * 0.5f;
        float y = gridOffset.y * _patternRegistry.GridSize.y - _patternRegistry.GridSize.y * 0.5f;

        return new Rect(x, y, _patternRegistry.GridSize.x, _patternRegistry.GridSize.y);
    }

    /// <summary>
    /// 로드 범위 계산 (카메라 + loadBufferSize)
    /// </summary>
    private Rect GetCameraRect()
    {
        return GetCameraBoundsWithBuffer(_loadBufferSize);
    }

    /// <summary>
    /// 언로드 범위 계산 (카메라 + loadBufferSize + unloadMargin)
    /// 히스테리시스를 통해 경계에서 떨림 방지
    /// </summary>
    private Rect GetCameraUnloadRect()
    {
        return GetCameraBoundsWithBuffer(_loadBufferSize + _unloadMargin);
    }

    private Rect GetCameraBoundsWithBuffer(float bufferSize)
    {
        if (_targetCamera == null || !_targetCamera.orthographic)
            return CreateRect(_cameraPosition, Vector3.one * DEFAULT_CAMERA_BOUNDS_SIZE);

        float height = _targetCamera.orthographicSize * CAMERA_SIZE_MULTIPLIER;
        float width = height * _targetCamera.aspect;
        var size = new Vector3(width + bufferSize * CAMERA_SIZE_MULTIPLIER, height + bufferSize * CAMERA_SIZE_MULTIPLIER);
        return CreateRect(_cameraPosition, size);
    }

    private float GetDistanceToPattern(LoadedPattern pattern, Vector2 position)
    {
        var patternCenter = CalculatePatternCenter(pattern.GridOffset);
        return Vector2.SqrMagnitude(position - patternCenter);
    }

    private bool ShouldUnloadByCamera(LoadedPattern pattern, Rect cameraRect)
    {
        var patternRect = CalculatePatternRect(pattern.GridOffset);
        return !Utility.Geometry.IsAABBOverlap(in cameraRect, in patternRect);
    }

    #endregion

    #region Utility

    private string GetPatternKey(string patternID, Vector2Int offset)
    {
        return $"{patternID}_{offset.x}_{offset.y}";
    }

    public Vector3 GetCameraPosition() => _cameraPosition;
    public int LoadedPatternCount => _loadedPatterns.Count;

    public List<string> GetLoadedPatternKeys()
    {
        return new List<string>(_loadedPatterns.Keys);
    }

    public bool IsPatternLoaded(string patternID, Vector2Int gridOffset)
    {
        var key = GetPatternKey(patternID, gridOffset);
        return _loadedPatterns.ContainsKey(key);
    }

    public Rect CreateRect(in Vector2 position, in Vector2 size)
    => new Rect(position - size * 0.5f, size);

    private void LogDebug(string message)
    {
        if (_showDebugInfo)
        {
            Debug.Log($"[TilemapStreamingManager] {message}");
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!_showDebugInfo || !_isInitialized) return;

        DrawLoadedPatternBounds();
        DrawCameraPosition();
        DrawCameraBounds();
    }

    private void DrawLoadedPatternBounds()
    {
        Gizmos.color = _debugColor;
        var size = new Vector3(_patternRegistry.GridSize.x, _patternRegistry.GridSize.y, 0);

        foreach (var loaded in _loadedPatterns.Values)
        {
            var center = CalculatePatternCenter(loaded.GridOffset);

            Gizmos.DrawWireCube(center, size);
        }
    }

    private void DrawCameraPosition()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_cameraPosition, DEBUG_CAMERA_SPHERE_RADIUS);
    }

    private void DrawCameraBounds()
    {
        if (!_showCameraBounds || _targetCamera == null || !_targetCamera.orthographic) return;

        float height = _targetCamera.orthographicSize * CAMERA_SIZE_MULTIPLIER;
        float width = height * _targetCamera.aspect;

        // 내부 가시 영역 (버퍼 제외)
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(_cameraPosition, new Vector3(width, height, 0f));

        // 로드 범위 (버퍼 포함)
        var loadRect = GetCameraRect();
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 녹색
        Gizmos.DrawWireCube(loadRect.center, loadRect.size);

        // 언로드 범위 (버퍼 + 마진 포함)
        var unloadRect = GetCameraUnloadRect();
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // 주황색
        Gizmos.DrawWireCube(unloadRect.center, unloadRect.size);
    }

    #endregion

    #region Nested Classes

    private class LoadedPattern
    {
        public string PatternID;
        public Vector2Int GridOffset;
        public TilemapPatternData PatternData;
        public GameObject TilemapInstance;
        public Entity SubSceneEntity;
        public float LoadTime;
    }

    private struct LoadRequest
    {
        public string PatternID;
        public Vector2Int GridOffset;
        public int Priority;
        public float RequestTime;
    }

    private class PatternHistory
    {
        public string PatternID;
        public Vector2Int GridOffset;
        public float UnloadTime;
    }

    #endregion
}
