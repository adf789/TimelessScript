using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "HomeFlow", menuName = "TS/Flow/Town Flow")]
public class TownFlow : BaseFlow
{
    [Header("Tilemap Settings")]
    [SerializeField] private bool loadTilemapPatterns = true;
    [SerializeField] private string tilemapSubSceneName = ""; // 비어있으면 State 이름 사용

    public override GameState State => GameState.Town;

    public override async UniTask Enter()
    {
        // 1. Scene 로드
        await OpenScene();

        // 2. Tilemap 패턴 로드
        await InitializeMap();

        // 3. UI 오픈
        OpenUI();
    }

    private async UniTask InitializeMap()
    {
        if (TilemapStreamingManager.Instance == null)
        {
            this.DebugLogWarning("TilemapStreamingManager is not initialized. Skipping tilemap loading.");
            return;
        }

        var dto = await LoadMapDto();

        await TilemapStreamingManager.Instance.Initialize();

        TilemapStreamingManager.Instance.SetMapData(in dto);
        TilemapStreamingManager.Instance.SetEventExtensionMap(OnExtensionMap);
        TilemapStreamingManager.Instance.SetEnableAutoStreaming(true);
    }

    private async UniTask<MapDto> LoadMapDto()
    {
        string playerId = AuthManager.Instance.PlayerID;
        var mapDoc = await DatabaseSubManager.Instance.GetDocumentAsync("maps", playerId);

        if (mapDoc != null)
        {
            var mapDic = mapDoc.ToDictionary();
            if (mapDic.TryGetValue("maps", out var value)
            && value is string valueJson)
            {
                var dto = JsonUtility.FromJson<MapDto>(valueJson);

                return dto;
            }
        }

        return await CreateBaseMapDto();
    }

    private async UniTask<MapDto> CreateBaseMapDto()
    {
        var mapDto = new MapDto()
        {
            MapGrids = new MapGridDto[1] { new MapGridDto() { Grid = int2.zero, MapDataID = "BaseTown" } }
        };
        var mapData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "maps", JsonUtility.ToJson(mapDto) }
            };

        // Firebase Firestore에 저장
        bool success = await DatabaseSubManager.Instance.SetDocumentAsync("maps", AuthManager.Instance.PlayerID, mapData);

        return mapDto;
    }

    public override async UniTask Exit()
    {
        // 1. Tilemap 패턴 언로드 (옵션)
        if (loadTilemapPatterns)
        {
            await UnloadTilemapPatterns();
        }

        // 2. UI 닫기
        CloseUI();

        // 3. Scene 언로드
        await CloseScene();
    }

    private async UniTask UnloadTilemapPatterns()
    {
        if (TilemapStreamingManager.Instance == null)
        {
            return;
        }

        this.DebugLog("Unloading tilemap patterns");

        try
        {
            await TilemapStreamingManager.Instance.UnloadAllPatterns();
            this.DebugLog("Tilemap patterns unloaded successfully");
        }
        catch (System.Exception ex)
        {
            this.DebugLogError($"Failed to unload tilemap patterns: {ex.Message}");
        }
    }

    private async void OnExtensionMap(int2 grid)
    {
        this.DebugLog($"Load map: {grid}");

        TilemapStreamingManager.Instance.AddRandomMap(grid);

        var mapDto = TilemapStreamingManager.Instance.CreateDto();

        var mapData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "maps", JsonUtility.ToJson(mapDto) }
            };

        // Firebase Firestore에 저장 (users 컬렉션의 {userId} 문서)
        bool success = await DatabaseSubManager.Instance.SetDocumentAsync("maps", AuthManager.Instance.PlayerID, mapData);

        if (!success)
        {
            this.DebugLogError($"Failed to save maps");
        }
    }
}
