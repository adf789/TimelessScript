using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.Scenes;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System;

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

    [Header("References")]
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private LadderResizingAddon _ladderPrefab;

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
    private readonly Queue<LoadMapRequest> _loadQueue = new Queue<LoadMapRequest>();
    private readonly HashSet<int2> _loadingKeys = new HashSet<int2>();

    // 카메라 추적
    private Vector2 _cameraPosition;
    private float _lastCameraSize;
    private float _lastUpdateTime;

    // 초기화 상태
    private bool _isInitialized;

    // 모든 맵 데이터
    private Dictionary<int2, MapNodeEntry> _mapDatas = new Dictionary<int2, MapNodeEntry>();

    // 콜백 이벤트
    private System.Action<int2> _onEventExtensionMap = null;

    // 타일맵 생성
    private Transform _mapParent;
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

    public void SetMapData(in MapDto dto)
    {
        for (int i = 0; i < dto.MapCount; i++)
        {
            var position = dto.MapGrids[i].Grid;
            var id = dto.MapGrids[i].MapDataID;
            var map = new MapNodeEntry(id)
            {
                GridOffset = position,
                PatternData = _patternRegistry.GetPattern(id)
            };

            _mapDatas[position] = map;
        }
    }

    public void SetMapParent(Transform parent)
    {
        _mapParent = parent;
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

    #region Auto Streaming

    /// <summary>
    /// 카메라 가시 영역 기반 자동 스트리밍 (히스테리시스 적용)
    /// 로드 범위: 카메라 + loadBufferSize
    /// 언로드 범위: 카메라 + loadBufferSize + unloadMargin
    /// 이를 통해 경계에서 로드/언로드 반복을 방지
    /// </summary>
    public async UniTask UpdateStreamingByCameraView()
    {
        if (!_isInitialized || _targetCamera == null || !_enableAutoStreaming || _mapParent == null) return;

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
        if (CheckLoadedPattern(gridOffset))
        {
            return true;
        }

        // 로딩 중 체크
        if (IsPatternLoading(gridOffset))
        {
            await WaitForPatternLoad(gridOffset);

            return true;
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

    private bool CheckLoadedPattern(int2 key)
    {
        if (_mapDatas.TryGetValue(key, out var entry))
        {
            return entry.IsLoaded;
        }

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

    private async UniTask WaitForPatternLoad(int2 key)
    {
        await UniTask.WaitUntil(() => _mapDatas.ContainsKey(key) || !_loadingKeys.Contains(key));
    }

    private bool ValidatePatternData(TilemapPatternData patternData, string patternID)
    {
        if (patternData == null)
        {
            this.DebugLogError($"Pattern not found: {patternID}");
            return false;
        }

        return true;
    }

    private async UniTask<bool> LoadPatternInternal(string patternID, int2 gridOffset, TilemapPatternData patternData)
    {
        _loadingKeys.Add(gridOffset);

        try
        {
            if (_mapDatas.TryGetValue(gridOffset, out var mapNode))
            {
                if (mapNode.SubSceneEntity == Entity.Null)
                {
                    mapNode.SubSceneEntity = await LoadSubScene(patternData, patternID, gridOffset);

                    // SubScene의 모든 Entity에 offset 적용
                    ApplyOffsetToSubSceneEntities(mapNode);

                    await LoadExtensionButton(gridOffset);

                    await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());

                    await LoadLadders(gridOffset);
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
        if (!_mapDatas.TryGetValue(gridOffset, out var node))
            return;

        var upGridOffset = new int2(gridOffset.x, gridOffset.y + 1);
        var downGridOffset = new int2(gridOffset.x, gridOffset.y - 1);

        // 위 방향에 연결된 노드가 있는지
        if (_mapDatas.TryGetValue(upGridOffset, out MapNodeEntry upNode)
        && upNode.IsLoaded)
        {
            // 사다리 생성 위치 계산 (baseLink.FromPosition 사용)
            // 패턴의 월드 위치 기준으로 계산
            float3 ladderPosition = GetLadderPosition(upNode, node);

            // 지형 Entity 가져옴
            Entity topGroundEntity = upNode.MinGroundEntity;
            Entity bottomGroundEntity = node.MaxGroundEntity;

            // 사다리 높이 계산
            float calculatedHeight = CalculateLadderHeight(upNode, node);

            // 사다리 Entity 생성
            CreateLadderEntity(
                ladderPosition,
                calculatedHeight,
                topGroundEntity,
                bottomGroundEntity);

            // 사다리 렌더링 오브젝트 생성
            CreateLadderRenderTarget(ladderPosition, calculatedHeight - 1);

            this.DebugLog($"Ladder created at {ladderPosition} between {gridOffset} and {upGridOffset}");
        }

        // 아래 방향에 연결된 노드가 있는지
        if (_mapDatas.TryGetValue(downGridOffset, out var downNode)
        && downNode.IsLoaded)
        {
            // 사다리 생성 위치 계산 (baseLink.FromPosition 사용)
            // 패턴의 월드 위치 기준으로 계산
            float3 ladderPosition = GetLadderPosition(node, downNode);

            // 지형 Entity 가져옴
            Entity topGroundEntity = node.MinGroundEntity;
            Entity bottomGroundEntity = downNode.MaxGroundEntity;

            // 사다리 높이 계산
            float calculatedHeight = CalculateLadderHeight(node, downNode);

            // 사다리 Entity 생성
            CreateLadderEntity(
                ladderPosition,
                calculatedHeight,
                topGroundEntity,
                bottomGroundEntity);

            // 사다리 렌더링 오브젝트 생성
            CreateLadderRenderTarget(ladderPosition, calculatedHeight - 1);

            this.DebugLog($"Ladder created at {ladderPosition} between {gridOffset} and {downGridOffset}");
        }
    }

    private async UniTask<Entity> LoadSubScene(TilemapPatternData patternData, string patternID, int2 gridOffset)
    {
        this.DebugLog($"LoadSubScene ({gridOffset})");

        if (!patternData.Prefab.IsReferenceValid)
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

        var entityManager = world.EntityManager;

        // EntityPrefabReference 로드 요청
        var prefabLoadRequest = entityManager.CreateEntity();
        entityManager.AddComponentData(prefabLoadRequest, new RequestEntityPrefabLoaded
        {
            Prefab = patternData.Prefab
        });

        // Prefab 로드 완료 대기
        await UniTask.WaitUntil(() =>
        {
            if (!entityManager.Exists(prefabLoadRequest))
                return false;

            return entityManager.HasComponent<PrefabLoadResult>(prefabLoadRequest);
        });

        // 로드된 Prefab Entity 가져오기
        var loadResult = entityManager.GetComponentData<PrefabLoadResult>(prefabLoadRequest);
        var prefabEntity = loadResult.PrefabRoot;

        // 요청 Entity 정리
        entityManager.DestroyEntity(prefabLoadRequest);

        // Prefab Entity 인스턴스화
        var subSceneEntity = entityManager.Instantiate(prefabEntity);

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
                if (entityManager.HasBuffer<GroundReferenceBuffer>(entity))
                {
                    // LocalTransform에 offset 적용
                    entityManager.AddComponentData(entity, new PendingPositionComponent { Position = offset });

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

    public void AddRandomMap(int2 grid)
    {
        if (_patternRegistry == null)
        {
            this.DebugLogError("Failed to add random map: registry is null");
            return;
        }

        var neighborDatas = GetNeighborDatas(grid);
        TilemapPatternData selectPatternData = null;
        int randomCount = 0;

        foreach (TilemapPatternData pattern in _patternRegistry.AllPatterns)
        {
            if (!pattern.IsRandomCreate)
                continue;

            bool pass = false;
            for (FourDirection dir = FourDirection.Up;
            System.Enum.IsDefined(typeof(FourDirection), dir);
            dir++)
            {
                var neighborData = neighborDatas[(int) dir];
                if (neighborData == null)
                    continue;

                if (neighborData != null
                && !pattern.CheckOverlap(neighborData, dir))
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
        {
            this.DebugLogError($"Not found any random map: ({grid.x},{grid.y})");
            return;
        }

        var id = selectPatternData.PatternID;
        var map = new MapNodeEntry(id)
        {
            GridOffset = grid,
            PatternData = _patternRegistry.GetPattern(id)
        };

        _mapDatas[grid] = map;

        LoadExtensionButton(grid).Forget();
    }

    public MapDto CreateDto()
    {
        MapDto dto = new MapDto();

        dto.MapGrids = new MapGridDto[_mapDatas.Count];

        int index = 0;
        foreach (var map in _mapDatas)
        {
            dto.MapGrids[index++] = new MapGridDto()
            {
                Grid = map.Key,
                MapDataID = map.Value.PatternID
            };
        }

        return dto;
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

        this.DebugLog($"UnloadSubScene ({mapNode.GridOffset})");

        // if (mapNode.TilemapInstance != null)
        // {
        //     Destroy(mapNode.TilemapInstance);
        //     mapNode.TilemapInstance = null;
        // }
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

                newButton = Instantiate(extensionButtonPrefab, transform);
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

    private Vector3 GetLadderPosition(MapNodeEntry upNode, MapNodeEntry downNode)
    {
        if (upNode == null || downNode == null)
            return Vector3.zero;

        var upGround = upNode.PatternData.GetLowGroundSize();
        var downGround = downNode.PatternData.GetHighGroundSize();
        int groundMin = Mathf.Max(upGround.min, downGround.min);
        int groundMax = Mathf.Min(upGround.max, downGround.max);
        int groundMiddle = (groundMin + groundMax) / 2;

        int2 upGridPosition = new int2(IntDefine.MAP_TOTAL_GRID_WIDTH * upNode.GridOffset.x, IntDefine.MAP_TOTAL_GRID_HEIGHT * upNode.GridOffset.y);
        int2 downGridPosition = new int2(IntDefine.MAP_TOTAL_GRID_WIDTH * downNode.GridOffset.x, IntDefine.MAP_TOTAL_GRID_HEIGHT * downNode.GridOffset.y);

        float upNodeOffsetY = upGridPosition.y + upNode.PatternData.MinHeight;
        float downNodeOffsetY = downGridPosition.y + downNode.PatternData.MaxHeight;

        float ladderX = upGridPosition.x + IntDefine.MAP_GRID_SIZE * groundMiddle + IntDefine.MAP_GRID_SIZE * 0.5f - IntDefine.MAP_TOTAL_GRID_HALF_WIDTH;
        float ladderY = (upNodeOffsetY + downNodeOffsetY) * 0.5f;

        return new Vector3(ladderX, ladderY, 0);
    }

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
        int x = Mathf.FloorToInt((position.x + IntDefine.MAP_TOTAL_GRID_HALF_WIDTH) * FloatDefine.INVERSE_MAP_GRID_WIDTH);
        int y = Mathf.FloorToInt((position.y + IntDefine.MAP_TOTAL_GRID_HALF_HEIGHT) * FloatDefine.INVERSE_MAP_GRID_HEIGHT);

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
    /// 런타임 중 사다리 Entity 생성
    /// </summary>
    private Entity CreateLadderEntity(float3 position, float height, Entity topGroundEntity, Entity bottomGroundEntity)
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
        entityManager.AddComponentData(ladderEntity, new SetNameComponent($"Ladder_{position.x:F1}_{position.y:F1}"));
#endif

        // 3. Transform 컴포넌트 추가
        entityManager.AddComponentData(ladderEntity, LocalTransform.FromPosition(position));
        entityManager.AddComponentData(ladderEntity, new LocalToWorld { Value = float4x4.Translate(position) });

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

        // 6. ColliderComponent 추가 (TSLadderAuthoring.Baker 참고)
        entityManager.AddComponentData(ladderEntity, new ColliderComponent
        {
            Layer = ColliderLayer.Ladder,
            Size = new float2(0.5f, height),
            Offset = new float2(0f, 0.5f),  // TSLadderAuthoring와 동일
            IsTrigger = true  // 사다리는 반드시 Trigger!
        });

        // 7. ColliderBoundsComponent 추가
        entityManager.AddComponentData(ladderEntity, new ColliderBoundsComponent());

        // 8. CollisionBuffer 추가
        entityManager.AddBuffer<CollisionBuffer>(ladderEntity);

        return ladderEntity;
    }

    /// <summary>
    /// 런타임 중 사다리 Entity 생성
    /// </summary>
    private void CreateLadderRenderTarget(float3 position, float height)
    {
        if (_ladderPrefab == null)
            return;

        var newLadder = Instantiate(_ladderPrefab, transform);

        newLadder.transform.position = position;

        newLadder.SetHeight(Mathf.RoundToInt(height));
        newLadder.Initialize();
    }

    /// <summary>
    /// 사다리 높이 계산
    /// </summary>
    private float CalculateLadderHeight(MapNodeEntry upNode, MapNodeEntry downNode)
    {
        int2 upGridPosition = new int2(IntDefine.MAP_TOTAL_GRID_WIDTH * upNode.GridOffset.x, IntDefine.MAP_TOTAL_GRID_HEIGHT * upNode.GridOffset.y);
        int2 downGridPosition = new int2(IntDefine.MAP_TOTAL_GRID_WIDTH * downNode.GridOffset.x, IntDefine.MAP_TOTAL_GRID_HEIGHT * downNode.GridOffset.y);

        float upNodeOffsetY = upGridPosition.y + upNode.PatternData.MinHeight;
        float downNodeOffsetY = downGridPosition.y + downNode.PatternData.MaxHeight;

        return upNodeOffsetY - downNodeOffsetY + 1;
    }

    #endregion
}
