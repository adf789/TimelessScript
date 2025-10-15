using Cysharp.Threading.Tasks;
using UnityEngine;
using TS.HighLevel.Manager;

[CreateAssetMenu(fileName = "HomeFlow", menuName = "Scriptable Objects/Flow/Town Flow")]
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

        // 2. Tilemap 패턴 로드 (옵션)
        if (loadTilemapPatterns)
        {
            await LoadTilemapPatterns();
        }

        // 3. UI 오픈
        OpenUI();
    }

    private async UniTask LoadTilemapPatterns()
    {
        if (TilemapStreamingManager.Instance == null)
        {
            Debug.LogWarning("[TownFlow] TilemapStreamingManager is not initialized. Skipping tilemap loading.");
            return;
        }

        string subSceneName = string.IsNullOrEmpty(tilemapSubSceneName)
            ? State.ToString()
            : tilemapSubSceneName;

        Debug.Log($"[TownFlow] Loading tilemap patterns for SubScene: {subSceneName}");

        try
        {
            await TilemapStreamingManager.Instance.LoadInitialPattern(subSceneName);
            Debug.Log($"[TownFlow] Tilemap patterns loaded successfully for {subSceneName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TownFlow] Failed to load tilemap patterns: {ex.Message}");
        }
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

        Debug.Log("[TownFlow] Unloading tilemap patterns");

        try
        {
            await TilemapStreamingManager.Instance.UnloadAllPatterns();
            Debug.Log("[TownFlow] Tilemap patterns unloaded successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TownFlow] Failed to unload tilemap patterns: {ex.Message}");
        }
    }
}
