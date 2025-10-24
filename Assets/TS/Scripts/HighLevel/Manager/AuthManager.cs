using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if UNITY_EDITOR
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
#endif

public class AuthManager : BaseManager<AuthManager>
{
    private bool isAuthenticated;
    private string playerId;
    private string playerName;

#if UNITY_EDITOR
    private AuthSetting authSetting;
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";

    private string _redirectUri;
    private string _state;
    private string _codeVerifier;
    private HttpListener _httpListener;
#endif

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

    private async void Awake()
    {
#if UNITY_EDITOR
        Debug.Log("[AuthManager][EDITOR] Google OAuth authentication system initialized");

        authSetting = await ResourcesTypeRegistry.Get().LoadAsyncWithName<ScriptableObject, AuthSetting>("AuthSetting");
#elif UNITY_ANDROID
        InitializeGooglePlayGames();
#else
        Debug.LogWarning("[AuthManager] Google Play Games is only supported on Android platform");
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        _httpListener?.Stop();
        _httpListener?.Close();
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
#endif

    /// <summary>
    /// Google Play Games 자동 로그인 시도
    /// </summary>
    public async UniTask<bool> SignInSilentlyAsync()
    {
#if UNITY_EDITOR
        // 에디터에서는 자동 로그인 불가 (OAuth는 항상 수동 브라우저 인증 필요)
        Debug.Log("[AuthManager][EDITOR] Silent sign-in not supported. Please use SignInAsync()");
        await UniTask.Yield();
        return false;
#elif UNITY_ANDROID
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
#else
        Debug.LogWarning("[AuthManager] Sign-in not supported on this platform");
        await UniTask.Yield();
        return false;
#endif
    }

    /// <summary>
    /// Google Play Games 수동 로그인 (UI 표시)
    /// </summary>
    public async UniTask<bool> SignInAsync(Action onEventFinished = null)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(authSetting.EditorClientId) || string.IsNullOrEmpty(authSetting.EditorClientSecret))
        {
            Debug.LogError("[AuthManager][EDITOR] Client ID and Client Secret must be set in Inspector!");
            return false;
        }

        try
        {
            Debug.Log("[AuthManager][EDITOR] Starting Google OAuth sign-in...");

            var success = await EditorGoogleOAuthSignIn();

            if (success)
            {
                Debug.Log($"[AuthManager][EDITOR] Sign-in successful. Player: {playerName} (ID: {playerId})");
                onEventFinished?.Invoke();
            }
            else
            {
                Debug.LogError("[AuthManager][EDITOR] Sign-in failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager][EDITOR] Sign-in error: {ex.Message}");
            return false;
        }
#elif UNITY_ANDROID
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
#else
        Debug.LogWarning("[AuthManager] Sign-in not supported on this platform");
        await UniTask.Yield();
        return false;
#endif
    }

    /// <summary>
    /// 현재 인증 상태 확인
    /// </summary>
    public bool IsPlayerAuthenticated()
    {
#if UNITY_EDITOR
        return isAuthenticated;
#elif UNITY_ANDROID
        return PlayGamesPlatform.Instance.IsAuthenticated();
#else
        return false;
#endif
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

#if UNITY_EDITOR
        Debug.Log($"[AuthManager][EDITOR] Achievement unlocked (simulated): {achievementId}");
        callback?.Invoke(true);
#elif UNITY_ANDROID
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
#endif
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

#if UNITY_EDITOR
        Debug.Log($"[AuthManager][EDITOR] Score posted to leaderboard (simulated) {leaderboardId}: {score}");
        callback?.Invoke(true);
#elif UNITY_ANDROID
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
#endif
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

#if UNITY_EDITOR
        Debug.Log("[AuthManager][EDITOR] Showing Achievements UI (simulated)");
#elif UNITY_ANDROID
        PlayGamesPlatform.Instance.ShowAchievementsUI();
#endif
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

#if UNITY_EDITOR
        if (string.IsNullOrEmpty(leaderboardId))
        {
            Debug.Log("[AuthManager][EDITOR] Showing all Leaderboards UI (simulated)");
        }
        else
        {
            Debug.Log($"[AuthManager][EDITOR] Showing Leaderboard UI for {leaderboardId} (simulated)");
        }
#elif UNITY_ANDROID
        if (string.IsNullOrEmpty(leaderboardId))
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI();
        }
        else
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
        }
#endif
    }

#if UNITY_EDITOR
    // ==================== 에디터 전용 Google OAuth 구현 ====================

    /// <summary>
    /// 에디터 전용 Google OAuth 로그인 플로우
    /// </summary>
    private async UniTask<bool> EditorGoogleOAuthSignIn()
    {
        // 1. Loopback flow 초기화 (고정 포트 사용)
        _redirectUri = $"http://localhost:{authSetting.EditorRedirectPort}/";
        _state = Guid.NewGuid().ToString("N");
        _codeVerifier = Guid.NewGuid().ToString("N");

        // 2. HttpListener 시작
        try
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_redirectUri);
            _httpListener.Start();
            Debug.Log($"[AuthManager][EDITOR] Listening on {_redirectUri}");
        }
        catch (HttpListenerException ex)
        {
            Debug.LogError($"[AuthManager][EDITOR] Failed to start HttpListener on port {authSetting.EditorRedirectPort}. " +
                           $"Port may be in use. Error: {ex.Message}");
            return false;
        }

        // 3. Authorization URL 생성 및 브라우저 열기
        var codeChallenge = CreateCodeChallenge(_codeVerifier);
        var scopes = "openid email profile";
        var authUrl = $"{AuthorizationEndpoint}?response_type=code" +
                      $"&scope={Uri.EscapeDataString(scopes)}" +
                      $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                      $"&client_id={authSetting.EditorClientId}" +
                      $"&state={_state}" +
                      $"&code_challenge={codeChallenge}" +
                      $"&code_challenge_method=S256";

        Debug.Log($"[AuthManager][EDITOR] Opening browser for authentication...");
        Application.OpenURL(authUrl);

        // 4. Authorization Code 대기
        var code = await WaitForAuthorizationCode();
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        // 5. Access Token 교환
        var accessToken = await ExchangeCodeForToken(code);
        if (string.IsNullOrEmpty(accessToken))
        {
            return false;
        }

        // 6. UserInfo 요청
        return await GetUserInfo(accessToken);
    }

    private string CreateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private async UniTask<string> WaitForAuthorizationCode()
    {
        try
        {
            var context = await _httpListener.GetContextAsync().AsUniTask();

            // HTML 응답 전송
            var response = context.Response;
            var html = "<html><body><h1>Authentication successful!</h1><p>You can close this window.</p></body></html>";
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            // URL 파싱
            var url = context.Request.Url.AbsoluteUri;
            var parameters = ParseQueryString(url);
            var state = parameters.Get("state");
            var code = parameters.Get("code");
            var error = parameters.Get("error");

            _httpListener.Stop();
            _httpListener.Close();

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[AuthManager][EDITOR] OAuth error: {error}");
                return null;
            }

            if (state != _state)
            {
                Debug.LogError("[AuthManager][EDITOR] State mismatch!");
                return null;
            }

            return code;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager][EDITOR] Authorization error: {ex.Message}");
            return null;
        }
    }

    private NameValueCollection ParseQueryString(string url)
    {
        var result = new NameValueCollection();
        foreach (Match match in Regex.Matches(url, @"(?<key>\w+)=(?<value>[^&#]+)"))
        {
            result.Add(match.Groups["key"].Value, Uri.UnescapeDataString(match.Groups["value"].Value));
        }
        return result;
    }

    private async UniTask<string> ExchangeCodeForToken(string code)
    {
        var form = new WWWForm();
        form.AddField("code", code);
        form.AddField("redirect_uri", _redirectUri);
        form.AddField("client_id", authSetting.EditorClientId);
        form.AddField("client_secret", authSetting.EditorClientSecret);
        form.AddField("code_verifier", _codeVerifier);
        form.AddField("grant_type", "authorization_code");

        using var request = UnityWebRequest.Post(TokenEndpoint, form);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AuthManager][EDITOR] Token exchange failed: {request.error}");
            return null;
        }

        var json = request.downloadHandler.text;
        Debug.Log($"[AuthManager][EDITOR] Token response: {json}");

        // JSON 파싱 (간단한 방식)
        var accessTokenMatch = Regex.Match(json, @"""access_token""\s*:\s*""([^""]+)""");
        if (accessTokenMatch.Success)
        {
            return accessTokenMatch.Groups[1].Value;
        }

        Debug.LogError("[AuthManager][EDITOR] Failed to parse access token");
        return null;
    }

    private async UniTask<bool> GetUserInfo(string accessToken)
    {
        using var request = UnityWebRequest.Get(UserInfoEndpoint);
        request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AuthManager][EDITOR] UserInfo request failed: {request.error}");
            return false;
        }

        var json = request.downloadHandler.text;
        Debug.Log($"[AuthManager][EDITOR] UserInfo: {json}");

        // JSON 파싱 (간단한 방식)
        var subMatch = Regex.Match(json, @"""sub""\s*:\s*""([^""]+)""");
        var nameMatch = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
        var emailMatch = Regex.Match(json, @"""email""\s*:\s*""([^""]+)""");

        if (subMatch.Success && nameMatch.Success)
        {
            playerId = subMatch.Groups[1].Value;
            playerName = nameMatch.Groups[1].Value;
            isAuthenticated = true;

            if (emailMatch.Success)
            {
                Debug.Log($"[AuthManager][EDITOR] Email: {emailMatch.Groups[1].Value}");
            }

            return true;
        }

        Debug.LogError("[AuthManager][EDITOR] Failed to parse UserInfo");
        return false;
    }
#endif
}