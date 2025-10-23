using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class AuthManager : BaseManager<AuthManager>
{
    private bool isAuthenticated;
    private string playerId;
    private string playerName;

    /// <summary>
    /// 구글 플레이 게임즈 로그인 상태
    /// </summary>
    public bool IsAuthenticated => isAuthenticated;

    /// <summary>
    /// 현재 로그인한 플레이어 ID
    /// </summary>
    public string PlayerId => playerId;

    /// <summary>
    /// 현재 로그인한 플레이어 이름
    /// </summary>
    public string PlayerName => playerName;

    private void Awake()
    {
#if UNITY_ANDROID
        InitializeGooglePlayGames();
#else
        Debug.LogWarning("Google Play Games is only supported on Android platform");
#endif
    }

#if UNITY_ANDROID
    /// <summary>
    /// Google Play Games 초기화
    /// </summary>
    private void InitializeGooglePlayGames()
    {
        PlayGamesPlatform.DebugLogEnabled = Debug.isDebugBuild;
        PlayGamesPlatform.Activate();

        Debug.Log("[AuthManager] Google Play Games initialized");
    }

    /// <summary>
    /// Google Play Games 자동 로그인 시도
    /// </summary>
    public async UniTask<bool> SignInSilentlyAsync()
    {
        var tcs = new UniTaskCompletionSource<bool>();

        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                UpdatePlayerInfo();
                Debug.Log($"[AuthManager] Silent sign-in successful. Player: {playerName} (ID: {playerId})");
                tcs.TrySetResult(true);
            }
            else
            {
                Debug.Log($"[AuthManager] Silent sign-in failed: {status}");
                tcs.TrySetResult(false);
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    /// Google Play Games 수동 로그인 (UI 표시)
    /// </summary>
    public async UniTask<bool> SignInAsync(Action onEventFinished = null)
    {
        var tcs = new UniTaskCompletionSource<bool>();

        PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                UpdatePlayerInfo();
                Debug.Log($"[AuthManager] Sign-in successful. Player: {playerName} (ID: {playerId})");
                tcs.TrySetResult(true);

                onEventFinished?.Invoke();
            }
            else
            {
                Debug.LogError($"[AuthManager] Sign-in failed: {status}");
                tcs.TrySetResult(false);
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    /// 플레이어 정보 업데이트
    /// </summary>
    private void UpdatePlayerInfo()
    {
        isAuthenticated = PlayGamesPlatform.Instance.localUser.authenticated;
        playerId = PlayGamesPlatform.Instance.localUser.id;
        playerName = PlayGamesPlatform.Instance.localUser.userName;
    }

    /// <summary>
    /// 현재 인증 상태 확인
    /// </summary>
    public bool IsPlayerAuthenticated()
    {
        return PlayGamesPlatform.Instance.IsAuthenticated();
    }

    /// <summary>
    /// 업적 잠금 해제
    /// </summary>
    public void UnlockAchievement(string achievementId, Action<bool> callback = null)
    {
        if (!isAuthenticated)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated");
            callback?.Invoke(false);
            return;
        }

        PlayGamesPlatform.Instance.ReportProgress(achievementId, 100.0, success =>
        {
            if (success)
            {
                Debug.Log($"[AuthManager] Achievement unlocked: {achievementId}");
            }
            else
            {
                Debug.LogError($"[AuthManager] Failed to unlock achievement: {achievementId}");
            }
            callback?.Invoke(success);
        });
    }

    /// <summary>
    /// 리더보드 점수 전송
    /// </summary>
    public void PostScoreToLeaderboard(string leaderboardId, long score, Action<bool> callback = null)
    {
        if (!isAuthenticated)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated");
            callback?.Invoke(false);
            return;
        }

        PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, success =>
        {
            if (success)
            {
                Debug.Log($"[AuthManager] Score posted to leaderboard {leaderboardId}: {score}");
            }
            else
            {
                Debug.LogError($"[AuthManager] Failed to post score to leaderboard: {leaderboardId}");
            }
            callback?.Invoke(success);
        });
    }

    /// <summary>
    /// 업적 UI 표시
    /// </summary>
    public void ShowAchievementsUI()
    {
        if (!isAuthenticated)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated");
            return;
        }

        PlayGamesPlatform.Instance.ShowAchievementsUI();
    }

    /// <summary>
    /// 리더보드 UI 표시
    /// </summary>
    public void ShowLeaderboardUI(string leaderboardId = null)
    {
        if (!isAuthenticated)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated");
            return;
        }

        if (string.IsNullOrEmpty(leaderboardId))
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI();
        }
        else
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
        }
    }
#endif
}
