using UnityEngine;
using Cysharp.Threading.Tasks;
using TS.HighLevel.Manager;

[CreateAssetMenu(fileName = "LoadingFlow", menuName = "Scriptable Objects/Flow/Loading Flow")]
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

        // 2. Tilemap 패턴 로드 (옵션)
        if (loadTilemapPatterns)
        {
            await LoadTilemapPatterns();
        }

        // 3. UI 오픈
        OpenUI();
    }

    /// <summary>
    /// 타일맵 패턴 로드
    /// </summary>
    private async UniTask LoadTilemapPatterns()
    {
        // TilemapStreamingManager가 초기화되었는지 확인
        if (TilemapStreamingManager.Instance == null)
        {
            Debug.LogWarning("[LoadingFlow] TilemapStreamingManager is not initialized. Skipping tilemap loading.");
            return;
        }

        // SubScene 이름 결정 (설정값 우선, 없으면 State 이름 사용)
        string subSceneName = string.IsNullOrEmpty(tilemapSubSceneName)
            ? State.ToString()
            : tilemapSubSceneName;

        Debug.Log($"[LoadingFlow] Loading tilemap patterns for SubScene: {subSceneName}");

        try
        {
            // 초기 패턴 로드
            await TilemapStreamingManager.Instance.LoadInitialPatterns(subSceneName);

            Debug.Log($"[LoadingFlow] Tilemap patterns loaded successfully for {subSceneName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LoadingFlow] Failed to load tilemap patterns: {ex.Message}");
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
