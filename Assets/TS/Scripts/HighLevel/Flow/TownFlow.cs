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
        await LoadTilemapPatterns();

        // 3. UI 오픈
        OpenUI();
    }

    private async UniTask LoadTilemapPatterns()
    {
        if (TilemapStreamingManager.Instance == null)
        {
            this.DebugLogWarning("TilemapStreamingManager is not initialized. Skipping tilemap loading.");
            return;
        }

        await TilemapStreamingManager.Instance.Initialize();

        TilemapStreamingManager.Instance.SetTestMapData();
        TilemapStreamingManager.Instance.SetEventExtensionMap(OnExtensionMap);
        TilemapStreamingManager.Instance.SetEnableAutoStreaming(true);
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

        var randomMap = TilemapStreamingManager.Instance.GetRandomMap(grid);
    }
}
