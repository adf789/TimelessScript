using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Scenes;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
    // 패턴 데이터
    private TilemapPatternRegistry _patternRegistry;

    // 패턴 캐시
    private readonly Dictionary<int2, GroundExtensionButtonAddon> _loadedExtensionButtons = new Dictionary<int2, GroundExtensionButtonAddon>();

    // 로딩 관리
    private readonly Queue<LoadRequest> _loadQueue = new Queue<LoadRequest>();
    private readonly HashSet<int2> _loadingKeys = new HashSet<int2>();

    // 카메라 추적
    private Vector2 _cameraPosition;
    private float _lastCameraSize;
    private float _lastUpdateTime;

    // 초기화 상태
    private bool _isInitialized;

    // 모든 맵 데이터
    private Dictionary<int2, MapNodeEntry> _mapDatas = new Dictionary<int2, MapNodeEntry>();
    private Dictionary<GridPair, int> _ladderDatas = new Dictionary<GridPair, int>();

    // 콜백 이벤트
    private System.Action<int2> _onEventExtensionMap = null;
    #endregion

    #region Unity Lifecycle

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

    public async UniTask Initialize()
    {
        if (_isInitialized) return;

        await LoadMapDatas();

        if (!ValidateRegistry()) return;
        if (!InitializeCamera()) return;

        _isInitialized = true;
        _lastUpdateTime = Time.time;

        this.DebugLog($"Initialized. Camera: {_targetCamera?.name}, MaxPatterns: {_maxLoadedPatterns}, UpdateInterval: {_updateInterval}s");
    }

    public async UniTask LoadMapDatas()
    {
        _patternRegistry = await ResourcesTypeRegistry.Get()
        .LoadAsyncWithName<TilemapPatternRegistry>("TilemapPatternRegistry");

        if (_patternRegistry == null)
        {
            this.DebugLogError($"Failed to load map pattern registry.");
            return;
        }

        _patternRegistry.Initialize();
    }

    /// <summary>
    /// 테스트용
    /// </summary>
    public void SetTestMapData()
    {
        // 테스트 데이터
        var basePosition = new int2(0, 0);
        var baseMap = new MapNodeEntry("BaseTown")
        {
            GridOffset = basePosition,
            PatternData = _patternRegistry.GetPattern("BaseTown")
        };

        var secondPosition = new int2(0, -1);
        var secondMap = new MapNodeEntry("BaseTown_1")
        {
            GridOffset = secondPosition,
            PatternData = _patternRegistry.GetPattern("BaseTown_1")
        };

        _ladderDatas[new GridPair(basePosition, secondPosition)] = 33;

        _mapDatas[basePosition] = baseMap;
        _mapDatas[secondPosition] = secondMap;
    }

    public void SetEventExtensionMap(System.Action<int2> onEvent)
    {
        _onEventExtensionMap = onEvent;
    }

    private bool ValidateRegistry()
    {
        if (_patternRegistry != null) return true;

        this.DebugLogError("PatternRegistry is not assigned!");
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
            this.DebugLogWarning("No camera found. Auto-streaming disabled.");
            _enableAutoStreaming = false;
            return false;
        }

        if (!_targetCamera.orthographic)
        {
            this.DebugLogWarning("Camera is not Orthographic! Streaming may not work correctly.");
        }

        SetCameraPosition(_targetCamera.transform.position);
        SetCameraSize(_targetCamera.orthographicSize);

        return true;
    }

    public void SetCamera(Camera camera)
    {
        if (camera == null)
        {
            this.DebugLogError("Cannot set null camera!");
            return;
        }

        if (!camera.orthographic)
        {
            this.DebugLogWarning("Camera is not Orthographic!");
        }

        _targetCamera = camera;
        SetCameraPosition(camera.transform.position);
        SetCameraSize(camera.orthographicSize);

        this.DebugLog($"Camera set: {camera.name}");
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
        this.DebugLog($"Camera zoom changed: {_lastCameraSize}");
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

    public async UniTask<bool> LoadPattern(string patternID, int2 gridOffset)
    {
        if (!_isInitialized)
        {
            this.DebugLogError("Not initialized!");
            return false;
        }

        // 중복 로드 체크
        if (TryGetLoadedPattern(gridOffset, out var existingInstance))
        {
            return existingInstance;
        }

        // 로딩 중 체크
        if (IsPatternLoading(gridOffset))
        {
            return await WaitForPatternLoad(gridOffset);
        }

        // 패턴 데이터 검증
        var patternData = _patternRegistry.GetPattern(patternID);
        if (!ValidatePatternData(patternData, patternID))
        {
            return false;
        }

        // 로딩 실행
        return await LoadPatternInternal(patternID, gridOffset, patternData);
    }

    private bool TryGetLoadedPattern(int2 key, out GameObject instance)
    {
        if (_mapDatas.TryGetValue(key, out var loaded))
        {
            instance = loaded.TilemapInstance;
            return instance != null;
        }

        instance = null;
        return false;
    }

    private bool IsPatternLoading(int2 key)
    {
        if (_loadingKeys.Contains(key))
        {
            this.DebugLog($"Pattern is currently loading: {key}");
            return true;
        }
        return false;
    }

    private async UniTask<GameObject> WaitForPatternLoad(int2 key)
    {
        await UniTask.WaitUntil(() => _mapDatas.ContainsKey(key) || !_loadingKeys.Contains(key));
        return _mapDatas.TryGetValue(key, out var loaded) ? loaded.TilemapInstance : null;
    }

    private bool ValidatePatternData(TilemapPatternData patternData, string patternID)
    {
        if (patternData == null)
        {
            this.DebugLogError($"Pattern not found: {patternID}");
            return false;
        }

        if (patternData.TilemapPrefab == null || !patternData.TilemapPrefab.RuntimeKeyIsValid())
        {
            this.DebugLogError($"Invalid Addressable reference for pattern: {patternID}");
            return false;
        }

        return true;
    }

    private async UniTask<bool> LoadPatternInternal(string patternID, int2 gridOffset, TilemapPatternData patternData)
    {
        _loadingKeys.Add(gridOffset);

        try
        {
            var newTilemap = await InstantiateTilemap(patternData, gridOffset);
            if (newTilemap == null)
            {
                _loadingKeys.Remove(gridOffset);
                return false;
            }

            if (_mapDatas.TryGetValue(gridOffset, out var mapNode))
            {
                mapNode.TilemapInstance = newTilemap;

                if (!mapNode.IsLoaded)
                {
                    var subSceneEntity = await LoadSubScene(patternData, patternID, gridOffset);

                    mapNode.SubSceneEntity = subSceneEntity;

                    // SubScene의 모든 Entity에 offset 적용
                    ApplyOffsetToSubSceneEntities(mapNode);

                    await LoadLadders(gridOffset);
                    await LoadExtensionButton(gridOffset);
                }
            }

            _loadingKeys.Remove(gridOffset);

            return true;
        }
        catch (System.Exception ex)
        {
            this.DebugLogError($"Exception loading pattern {patternID}: {ex.Message}");
            _loadingKeys.Remove(gridOffset);
            return false;
        }
    }

    private async UniTask LoadLadders(int2 gridOffset)
    {
        // 해당 그리드의 패턴이 로드되었는지
        if (!_mapDatas.TryGetValue(gridOffset, out var node)
        && node.IsLoaded)
            return;

        var upGridOffset = new int2(gridOffset.x, gridOffset.y + 1);
        var downGridOffset = new int2(gridOffset.x, gridOffset.y - 1);

        // 위 방향에 연결된 노드가 있는지
        if (_mapDatas.TryGetValue(upGridOffset, out MapNodeEntry upNode))
        {
            if (!_ladderDatas.TryGetValue(new GridPair(gridOffset, upGridOffset), out int ladderPositionX))
            {
                this.DebugLogError($"Failed to found up ladder position: {gridOffset} -> {upGridOffset}");
                return;
            }

            // 사다리 생성 위치 계산 (baseLink.FromPosition 사용)
            // 패턴의 월드 위치 기준으로 계산
            float3 ladderPosition = GetLadderPosition(upNode, node, ladderPositionX);

            // 지형 Entity 가져옴
            Entity bottomGroundEntity = node.MaxGroundEntity;
            Entity topGroundEntity = upNode.MinGroundEntity;

            // 사다리 Entity 생성
            CreateLadderEntity(
                ladderPosition,
                topGroundEntity,
                bottomGroundEntity);

            this.DebugLog($"Ladder created at {ladderPosition} between {gridOffset} and {upGridOffset}");
        }

        // 아래 방향에 연결된 노드가 있는지
        if (_mapDatas.TryGetValue(downGridOffset, out var downNode))
        {
            if (!_ladderDatas.TryGetValue(new GridPair(gridOffset, downGridOffset), out int ladderPositionX))
            {
                this.DebugLogError($"Failed to found down ladder position: {gridOffset} -> {downGridOffset}");
                return;
            }

            // 사다리 생성 위치 계산 (baseLink.FromPosition 사용)
            // 패턴의 월드 위치 기준으로 계산
            float3 ladderPosition = GetLadderPosition(node, downNode, ladderPositionX);

            // TODO: Ground Entity 찾는 로직은 패턴 내부 구조에 따라 구현 필요
            // 현재는 Entity.Null로 설정 (나중에 실제 Ground 찾는 로직 추가)
            Entity bottomGroundEntity = downNode.MaxGroundEntity;
            Entity topGroundEntity = node.MinGroundEntity;

            // 사다리 Entity 생성
            CreateLadderEntity(
                ladderPosition,
                topGroundEntity,
                bottomGroundEntity);

            this.DebugLog($"Ladder created at {ladderPosition} between {gridOffset} and {downGridOffset}");
        }
    }

    private async UniTask<GameObject> InstantiateTilemap(TilemapPatternData patternData, int2 gridOffset)
    {
        var tileMapHandle = Addressables.InstantiateAsync(patternData.TilemapPrefab);
        var instance = await tileMapHandle.Task;

        if (instance == null)
        {
            this.DebugLogError($"Failed to instantiate pattern");
            return null;
        }

        instance.transform.position = CalculatePatternCenter(gridOffset);
        instance.name = $"TilemapPattern_{gridOffset}";

        return instance;
    }

    private async UniTask<Entity> LoadSubScene(TilemapPatternData patternData, string patternID, int2 gridOffset)
    {
        if (!patternData.SubScene.IsReferenceValid)
        {
            this.DebugLog($"No SubScene reference for pattern: {patternID}");
            return Entity.Null;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            this.DebugLogWarning($"World not available for SubScene loading: {patternID}");
            return Entity.Null;
        }

        // SubScene 로드 (BlockOnStreamIn으로 즉시 로드)
        var loadParams = new SceneSystem.LoadParameters
        {
            Flags = SceneLoadFlags.BlockOnStreamIn | SceneLoadFlags.LoadAdditive
        };

        var subSceneEntity = SceneSystem.LoadSceneAsync(
            world.Unmanaged,
            patternData.SubScene,
            loadParams
        );

        // SubScene 로드 완료 대기
        await UniTask.WaitUntil(() =>
            SceneSystem.IsSceneLoaded(world.Unmanaged, subSceneEntity)
        );

        this.DebugLog($"SubScene loaded for pattern: {patternID} at offset {gridOffset} (Entity: {subSceneEntity})");

        return subSceneEntity;
    }

    private void ApplyOffsetToSubSceneEntities(MapNodeEntry mapNode)
    {
        if (mapNode == null)
            return;

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // SubScene의 SceneReference 가져오기
        if (!entityManager.HasComponent<SceneReference>(mapNode.SubSceneEntity))
            return;

        // gridOffset 계산
        float3 offset = new float3(
            mapNode.GridOffset.x * IntDefine.MAP_TOTAL_GRID_WIDTH,
            mapNode.GridOffset.y * IntDefine.MAP_TOTAL_GRID_HEIGHT,
            0
        );

        var sceneRef = entityManager.GetComponentData<SceneReference>(mapNode.SubSceneEntity);

        // SubScene에 속한 모든 Entity 쿼리
        using var query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<LocalTransform>(),
            ComponentType.ReadOnly<SceneSection>()
        );

        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

        foreach (var entity in entities)
        {
            // 해당 SubScene의 Entity인지 확인
            var sceneSection = entityManager.GetSharedComponent<SceneSection>(entity);
            if (sceneSection.SceneGUID == sceneRef.SceneGUID)
            {
                // LocalTransform에 offset 적용
                var transform = entityManager.GetComponentData<LocalTransform>(entity);
                transform.Position += offset;
                entityManager.SetComponentData(entity, transform);

                if (entityManager.HasBuffer<GroundReferenceBuffer>(entity))
                {
                    var referenceBuffer = entityManager.GetBuffer<GroundReferenceBuffer>(entity);

                    int min = int.MaxValue;
                    int max = int.MinValue;

                    foreach (var reference in referenceBuffer)
                    {
                        if (min > reference.Max.y)
                        {
                            mapNode.MinGroundEntity = reference.Ground;
                            min = reference.Max.y;
                        }

                        if (max < reference.Max.y)
                        {
                            mapNode.MaxGroundEntity = reference.Ground;
                            max = reference.Max.y;
                        }
                    }
                }
            }
        }
    }

    public Rect CreateRect(in Vector2 position, in Vector2 size)
    => new Rect(position - size * 0.5f, size);

    private TilemapPatternData[] GetNeighborDatas(int2 grid)
    {
        TilemapPatternData[] neighborNodes = new TilemapPatternData[4];

        if (_mapDatas.TryGetValue(new int2(grid.x, grid.y + 1), out var upNode))
            neighborNodes[(int) FourDirection.Up] = _patternRegistry.GetPattern(upNode.PatternID);

        if (_mapDatas.TryGetValue(new int2(grid.x, grid.y - 1), out var downNode))
            neighborNodes[(int) FourDirection.Down] = _patternRegistry.GetPattern(downNode.PatternID);

        if (_mapDatas.TryGetValue(new int2(grid.x - 1, grid.y), out var leftNode))
            neighborNodes[(int) FourDirection.Left] = _patternRegistry.GetPattern(leftNode.PatternID);

        if (_mapDatas.TryGetValue(new int2(grid.x + 1, grid.y), out var rightNode))
            neighborNodes[(int) FourDirection.Right] = _patternRegistry.GetPattern(rightNode.PatternID);

        return neighborNodes;
    }

    public RandomMapResult GetRandomMap(int2 grid)
    {
        if (_patternRegistry == null)
        {
            this.DebugLogError("Failed to map: registry is null");
            return default;
        }

        var neighborDatas = GetNeighborDatas(grid);
        TilemapPatternData selectPatternData = null;
        int randomCount = 0;

        foreach (TilemapPatternData pattern in _patternRegistry.AllPatterns)
        {
            bool pass = false;
            for (FourDirection dir = FourDirection.Up;
            System.Enum.IsDefined(typeof(FourDirection), dir);
            dir++)
            {
                var neighborData = neighborDatas[(int) dir];
                if (neighborData == null)
                    continue;

                if (!pattern.CheckOverlap(neighborData, dir))
                {
                    pass = true;
                    break;
                }
            }

            if (pass)
                continue;

            if (UnityEngine.Random.Range(0, ++randomCount) == 0)
                selectPatternData = pattern;
        }

        if (selectPatternData == null)
            return default;

        int topCount = 0, bottomCount = 0;
        int topPosition = -1, bottomPosition = -1;
        long topOverlap = 0;
        long bottomOverlap = 0;

        if (neighborDatas[(int) FourDirection.Up] != null)
            topOverlap = selectPatternData.GetOverlap(neighborDatas[(int) FourDirection.Up], FourDirection.Up);

        if (neighborDatas[(int) FourDirection.Down] != null)
            bottomOverlap = selectPatternData.GetOverlap(neighborDatas[(int) FourDirection.Down], FourDirection.Down);

        for (int num = 0; num < IntDefine.MAP_TOTAL_GRID_WIDTH; num++)
        {
            var bit = 1L << num;
            var compareTop = topOverlap | bit;
            var compareBottom = bottomOverlap | bit;
            if (topOverlap > 0
            && compareTop > 0
            && UnityEngine.Random.Range(0, ++topCount) == 0)
                topPosition = num;

            if (bottomOverlap > 0
            && compareBottom > 0
            && UnityEngine.Random.Range(0, ++bottomCount) == 0)
                bottomPosition = num;
        }

        string id = selectPatternData.PatternID;
        int2 topPos = new int2(topPosition, topPosition >= 0 ? selectPatternData.MaxHeight : -1);
        int2 bottomPos = new int2(bottomPosition, bottomPosition >= 0 ? selectPatternData.MinHeight : -1);

        return new RandomMapResult(id, topPos, bottomPos);
    }

    #endregion

    #region Pattern Unloading

    public async UniTask UnloadPattern(int2 gridOffset)
    {
        if (!_mapDatas.TryGetValue(gridOffset, out var mapNode))
        {
            this.DebugLog($"Pattern not loaded: {gridOffset}");
            return;
        }

        try
        {
            UnloadTilemap(mapNode);

            this.DebugLog($"Pattern unloaded and saved to history: {gridOffset}");
        }
        catch (System.Exception ex)
        {
            this.DebugLogError($"Exception unloading pattern {gridOffset}: {ex.Message}");
        }

        await UniTask.Yield();
    }

    private void UnloadTilemap(MapNodeEntry mapNode)
    {
        if (mapNode == null)
            return;

        if (mapNode.TilemapInstance != null)
        {
            Object.Destroy(mapNode.TilemapInstance);
            mapNode.TilemapInstance = null;
        }
    }

    public async UniTask UnloadAllPatterns()
    {
        var patternsToUnload = new List<int2>();

        foreach (var loaded in _mapDatas.Values)
        {
            patternsToUnload.Add(loaded.GridOffset);
        }

        foreach (var gridOffset in patternsToUnload)
        {
            await UnloadPattern(gridOffset);
        }

        _mapDatas.Clear();

        this.DebugLog("All patterns unloaded");
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
        // 맵 로드 영역 체크 (y좌표 무시, 영역 내 그리드만 탐색)
        Vector2Int minGrid = CalculateGrid(new Vector3(cameraRect.min.x, cameraRect.min.y));
        Vector2Int maxGrid = CalculateGrid(new Vector3(cameraRect.max.x, cameraRect.max.y));

        for (int x = minGrid.x; x <= maxGrid.x; x++)
        {
            for (int y = minGrid.y; y <= maxGrid.y; y++)
            {
                int2 grid = new int2(x, y);

                if (_mapDatas.TryGetValue(grid, out var node))
                {
                    if (node.IsLoaded)
                        continue;

                    await LoadPattern(node.PatternID, grid);
                }
            }
        }
    }

    private async UniTask UnloadPatternsOutsideCameraView(Rect cameraRect)
    {
        List<int2> unloadGrids = null;

        foreach (var mapData in _mapDatas)
        {
            if (ShouldUnloadByCamera(mapData.Value, cameraRect))
            {
                if (!mapData.Value.IsLoaded)
                    continue;

                if (unloadGrids == null)
                    unloadGrids = new List<int2> { mapData.Key };
                else
                    unloadGrids.Add(mapData.Key);
            }
        }

        if (unloadGrids != null)
        {
            foreach (var grid in unloadGrids)
            {
                await UnloadPattern(grid);
            }

            if (unloadGrids.Count > 0)
            {
                this.DebugLog($"Auto-unloaded {unloadGrids.Count} patterns outside camera view");
            }
        }
    }

    private async UniTask LoadExtensionButton(int2 grid)
    {
        GroundExtensionButtonAddon extensionButtonPrefab = null;

        _loadedExtensionButtons.Remove(grid, out var removeExtensionButton);

        removeExtensionButton?.gameObject.SetActive(false);

        foreach (var neighborGrid in GetNeighborGrids(grid))
        {
            // 그리드에 맵이 있거나 로드된 버튼이 있으면 패스
            if (_mapDatas.ContainsKey(neighborGrid)
            || _loadedExtensionButtons.ContainsKey(neighborGrid))
                continue;

            GroundExtensionButtonAddon newButton = null;

            if (removeExtensionButton != null)
            {
                newButton = removeExtensionButton;
            }
            else
            {
                if (extensionButtonPrefab == null)
                {
                    extensionButtonPrefab = await ResourcesTypeRegistry.Get().LoadAsyncWithName<GroundExtensionButtonAddon>("Plus");
                }

                newButton = Instantiate(extensionButtonPrefab);
            }

            newButton.SetGrid(neighborGrid);
            newButton.SetEventExtension(_onEventExtensionMap);
            newButton.transform.position = new Vector3(IntDefine.MAP_TOTAL_GRID_WIDTH * neighborGrid.x, IntDefine.MAP_TOTAL_GRID_HEIGHT * neighborGrid.y);

            _loadedExtensionButtons[neighborGrid] = newButton;

            newButton.gameObject.SetActive(true);
        }
    }

    private IEnumerable<int2> GetNeighborGrids(int2 currentGrid)
    {
        // Up
        yield return new int2(currentGrid.x, currentGrid.y + 1);

        // Down
        yield return new int2(currentGrid.x, currentGrid.y - 1);

        // Left
        yield return new int2(currentGrid.x - 1, currentGrid.y);

        // Right
        yield return new int2(currentGrid.x + 1, currentGrid.y);
    }

    private Vector3 GetLadderPosition(MapNodeEntry upNode, MapNodeEntry downNode, int ladderOffsetX)
    {
        if (upNode == null || downNode == null)
            return Vector3.zero;

        int2 upGridPosition = new int2(IntDefine.MAP_TOTAL_GRID_WIDTH * upNode.GridOffset.x, IntDefine.MAP_TOTAL_GRID_HEIGHT * upNode.GridOffset.y);
        int2 downGridPosition = new int2(IntDefine.MAP_TOTAL_GRID_WIDTH * downNode.GridOffset.x, IntDefine.MAP_TOTAL_GRID_HEIGHT * downNode.GridOffset.y);
        float upNodeOffsetY = upGridPosition.y + upNode.PatternData.MinHeight * IntDefine.MAP_GRID_SIZE + IntDefine.MAP_GRID_SIZE * 0.5f;
        float downNodeOffsetY = downGridPosition.y + downNode.PatternData.MaxHeight * IntDefine.MAP_GRID_SIZE + IntDefine.MAP_GRID_SIZE * 0.5f;

        float ladderX = upGridPosition.x + IntDefine.MAP_TOTAL_GRID_WIDTH * ladderOffsetX + IntDefine.MAP_TOTAL_GRID_WIDTH * 0.5f;
        float ladderY = (upNodeOffsetY + downNodeOffsetY) * 0.5f;

        return new Vector3(ladderX, ladderY, 0);
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

    public void EnqueueLoadRequest(string patternID, int2 gridOffset, int priority = 50)
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

    private Vector2 CalculatePatternCenter(int2 gridOffset)
    {
        return new Vector2(
            gridOffset.x * _patternRegistry.GridSize.x,
            gridOffset.y * _patternRegistry.GridSize.y
        );
    }

    private Rect CalculatePatternRect(int2 gridOffset)
    {
        float x = gridOffset.x * _patternRegistry.GridSize.x - _patternRegistry.GridSize.x * 0.5f;
        float y = gridOffset.y * _patternRegistry.GridSize.y - _patternRegistry.GridSize.y * 0.5f;

        return new Rect(x, y, _patternRegistry.GridSize.x, _patternRegistry.GridSize.y);
    }

    private Vector2Int CalculateGrid(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x * FloatDefine.INVERSE_MAP_GRID_WIDTH);
        int y = Mathf.FloorToInt(position.y * FloatDefine.INVERSE_MAP_GRID_HEIGHT);

        return new Vector2Int(x, y);
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

    private float GetDistanceToPattern(MapNodeEntry mapNode, Vector2 position)
    {
        var patternCenter = CalculatePatternCenter(mapNode.GridOffset);
        return Vector2.SqrMagnitude(position - patternCenter);
    }

    private bool ShouldUnloadByCamera(MapNodeEntry mapNode, Rect cameraRect)
    {
        var patternRect = CalculatePatternRect(mapNode.GridOffset);
        return !Utility.Geometry.IsAABBOverlap(in cameraRect, in patternRect);
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

        foreach (var loaded in _mapDatas.Values)
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

    #region Ladder Creation (Runtime ECS Entity Generation)

    /// <summary>
    /// 런타임 중 사다리 Entity 생성 (TSLadderAuthoring.Baker 로직 참고)
    /// </summary>
    /// <param name="position">사다리 생성 위치</param>
    /// <param name="topGroundEntity">상단 연결 지형 Entity</param>
    /// <param name="bottomGroundEntity">하단 연결 지형 Entity</param>
    /// <returns>생성된 사다리 Entity</returns>
    private Entity CreateLadderEntity(float3 position, Entity topGroundEntity, Entity bottomGroundEntity)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            this.DebugLogError("DefaultGameObjectInjectionWorld is null!");
            return Entity.Null;
        }

        var entityManager = world.EntityManager;

        // 1. Entity 생성
        Entity ladderEntity = entityManager.CreateEntity();

#if UNITY_EDITOR
        // 2. 디버깅용 이름 설정
        entityManager.AddComponentData(ladderEntity, new SetNameComponent() { Name = $"Ladder_{position.x:F1}_{position.y:F1}" });
#endif

        // 3. Transform 컴포넌트 추가
        entityManager.AddComponentData(ladderEntity, LocalTransform.FromPosition(position));

        // 4. TSObjectComponent 추가
        entityManager.AddComponentData(ladderEntity, new TSObjectComponent
        {
            Self = ladderEntity,
            ObjectType = TSObjectType.Ladder,
            RootOffset = 0f
        });

        // 5. TSLadderComponent 추가 (연결된 Ground 설정)
        entityManager.AddComponentData(ladderEntity, new TSLadderComponent
        {
            TopConnectedGround = topGroundEntity,
            BottomConnectedGround = bottomGroundEntity
        });

        // 6. 사다리 높이 계산 (TSLadderAuthoring.CalculateLadderHeight 참고)
        float calculatedHeight = CalculateLadderHeight(
            entityManager,
            position.y,
            topGroundEntity,
            bottomGroundEntity);

        // 7. ColliderComponent 추가 (TSLadderAuthoring.Baker 참고)
        entityManager.AddComponentData(ladderEntity, new ColliderComponent
        {
            Layer = ColliderLayer.Ladder,
            Size = new float2(0.5f, calculatedHeight),
            Offset = new float2(0f, 0.5f),  // TSLadderAuthoring와 동일
            IsTrigger = true  // 사다리는 반드시 Trigger!
        });

        // 8. ColliderBoundsComponent 추가
        entityManager.AddComponentData(ladderEntity, new ColliderBoundsComponent());

        // 9. CollisionBuffer 추가
        entityManager.AddBuffer<CollisionBuffer>(ladderEntity);

        return ladderEntity;
    }

    /// <summary>
    /// 사다리 높이 계산 (TSLadderAuthoring.CalculateLadderHeight 로직 그대로 구현)
    /// </summary>
    private float CalculateLadderHeight(
        EntityManager entityManager,
        float ladderY,
        Entity topGroundEntity,
        Entity bottomGroundEntity)
    {
        float defaultHeight = 3.0f; // 기본 높이

        // TopConnectedGround와 BottomConnectedGround가 모두 있는 경우
        if (topGroundEntity != Entity.Null && bottomGroundEntity != Entity.Null)
        {
            if (entityManager.HasComponent<LocalTransform>(topGroundEntity) &&
                entityManager.HasComponent<LocalTransform>(bottomGroundEntity))
            {
                float topY = entityManager.GetComponentData<LocalTransform>(topGroundEntity).Position.y;
                float bottomY = entityManager.GetComponentData<LocalTransform>(bottomGroundEntity).Position.y;
                float groundDistance = math.abs(topY - bottomY);

                // TopConnectedGround보다 1 높게 설정
                return groundDistance + 1.0f;
            }
        }
        // TopConnectedGround만 있는 경우
        else if (topGroundEntity != Entity.Null)
        {
            if (entityManager.HasComponent<LocalTransform>(topGroundEntity))
            {
                float topY = entityManager.GetComponentData<LocalTransform>(topGroundEntity).Position.y;
                float distanceToTop = math.abs(topY - ladderY);

                // TopConnectedGround보다 1 높게 설정
                return distanceToTop + 1.0f;
            }
        }
        // BottomConnectedGround만 있는 경우
        else if (bottomGroundEntity != Entity.Null)
        {
            if (entityManager.HasComponent<LocalTransform>(bottomGroundEntity))
            {
                float bottomY = entityManager.GetComponentData<LocalTransform>(bottomGroundEntity).Position.y;
                float distanceToBottom = math.abs(ladderY - bottomY);

                // 기본적으로 하단에서 위로 올라가는 높이 + 1
                return distanceToBottom + 1.0f;
            }
        }

        return defaultHeight;
    }

    #endregion

    #region Nested Classes

    private struct LoadRequest
    {
        public string PatternID;
        public int2 GridOffset;
        public int Priority;
        public float RequestTime;
    }

    #endregion
}
