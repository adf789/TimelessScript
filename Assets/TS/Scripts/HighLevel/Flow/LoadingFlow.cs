using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "LoadingFlow", menuName = "TS/Flow/Loading Flow")]
public class LoadingFlow : BaseFlow
{
    [Header("Tilemap Settings")]
    [SerializeField] private bool loadTilemapPatterns = true;
    [SerializeField] private string tilemapSubSceneName = ""; // 비어있으면 State 이름 사용

    public override GameState State => GameState.Loading;

    public override async UniTask Enter()
    {
        // 1. Scene 로드
        await OpenScene();

        // 3. UI 오픈
        OpenUI();
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

    /// <summary>
    /// 타일맵 패턴 언로드
    /// </summary>
    private async UniTask UnloadTilemapPatterns()
    {
        if (TilemapStreamingManager.Instance == null)
        {
            return;
        }

        Debug.Log("[LoadingFlow] Unloading tilemap patterns");

        try
        {
            await TilemapStreamingManager.Instance.UnloadAllPatterns();
            Debug.Log("[LoadingFlow] Tilemap patterns unloaded successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LoadingFlow] Failed to unload tilemap patterns: {ex.Message}");
        }
    }
}
