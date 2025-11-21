using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class AuthManager : BaseManager<AuthManager>
{
    private FirebaseAuth _auth;
    private FirebaseUser _currentUser;
    private bool _isInitialized;

    /// <summary>
    /// Firebase 인증 상태
    /// </summary>
    public bool IsAuthenticated => _currentUser != null;

    /// <summary>
    /// 현재 로그인한 사용자 ID
    /// </summary>
    public string PlayerID => _currentUser?.UserId ?? string.Empty;

    /// <summary>
    /// 현재 로그인한 사용자 이름
    /// </summary>
    public string PlayerName => _currentUser?.DisplayName ?? "Guest";

    /// <summary>
    /// 현재 로그인한 사용자 이메일
    /// </summary>
    public string PlayerEmail => _currentUser?.Email ?? string.Empty;

    /// <summary>
    /// Firebase User 객체
    /// </summary>
    public FirebaseUser CurrentUser => _currentUser;

    private async void Start()
    {
        await InitializeFirebaseAuth();
    }

    /// <summary>
    /// Firebase Auth 초기화
    /// </summary>
    private async UniTask InitializeFirebaseAuth()
    {
        try
        {
            // Firebase 의존성 확인
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance;
                _auth.StateChanged += OnAuthStateChanged;

                _isInitialized = true;
                Debug.Log("[AuthManager] Firebase Auth initialized successfully");

                // 자동 로그인 시도 (이전 세션 복원)
                if (_auth.CurrentUser != null)
                {
                    _currentUser = _auth.CurrentUser;
                    Debug.Log($"[AuthManager] Auto sign-in successful. User: {PlayerName} (ID: {PlayerID})");
                }
            }
            else
            {
                Debug.LogError($"[AuthManager] Firebase dependencies error: {dependencyStatus}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Firebase Auth initialization failed: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (_auth != null)
        {
            _auth.StateChanged -= OnAuthStateChanged;
        }
    }

    /// <summary>
    /// Firebase Auth 상태 변경 콜백
    /// </summary>
    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (_auth.CurrentUser != _currentUser)
        {
            bool signedIn = _currentUser != _auth.CurrentUser && _auth.CurrentUser != null;
            _currentUser = _auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log($"[AuthManager] User signed in: {PlayerName} (ID: {PlayerID})");
            }
            else if (_currentUser == null)
            {
                Debug.Log("[AuthManager] User signed out");
            }
        }
    }

    /// <summary>
    /// Google Sign-In (Android/iOS)
    /// </summary>
    public async UniTask<bool> SignInWithGoogleAsync(Action onEventFinished = null)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AuthManager] Firebase Auth not initialized");
            return false;
        }

#if UNITY_ANDROID || UNITY_IOS
        try
        {
            Debug.Log("[AuthManager] Starting Google Sign-In...");

            // FederatedOAuthProvider 사용
            var provider = new FederatedOAuthProvider();
            provider.SetProviderData(new FederatedOAuthProviderData
            {
                ProviderId = GoogleAuthProvider.ProviderId
            });

            var credential = await _auth.SignInWithProviderAsync(provider).AsUniTask();

            if (credential != null && credential.User != null)
            {
                _currentUser = credential.User;
                Debug.Log($"[AuthManager] Google sign-in successful. User: {PlayerName} (ID: {PlayerID})");

                // Firestore에 유저 데이터 저장
                await SaveUserDataToDatabase();

                onEventFinished?.Invoke();
                return true;
            }

            Debug.LogError("[AuthManager] Google sign-in failed: Invalid credential");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Google sign-in error: {ex.Message}");
            return false;
        }
#else
        Debug.LogWarning("[AuthManager] Google Sign-In only supported on Android/iOS");
        await UniTask.Yield();
        return false;
#endif
    }

    /// <summary>
    /// 익명 로그인
    /// </summary>
    public async UniTask<bool> SignInAnonymouslyAsync(Action onEventFinished = null)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AuthManager] Firebase Auth not initialized");
            return false;
        }

        try
        {
            Debug.Log("[AuthManager] Starting anonymous sign-in...");

            var credential = await _auth.SignInAnonymouslyAsync().AsUniTask();

            if (credential != null && credential.User != null)
            {
                _currentUser = credential.User;
                Debug.Log($"[AuthManager] Anonymous sign-in successful. User ID: {PlayerID}");

                onEventFinished?.Invoke();
                return true;
            }

            Debug.LogError("[AuthManager] Anonymous sign-in failed");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Anonymous sign-in error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 이메일/비밀번호 회원가입
    /// </summary>
    public async UniTask<bool> SignUpWithEmailAsync(string email, string password, string displayName = null)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AuthManager] Firebase Auth not initialized");
            return false;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("[AuthManager] Email or password is empty");
            return false;
        }

        try
        {
            Debug.Log($"[AuthManager] Creating account for {email}...");

            var credential = await _auth.CreateUserWithEmailAndPasswordAsync(email, password).AsUniTask();

            if (credential != null && credential.User != null)
            {
                _currentUser = credential.User;

                // DisplayName 설정
                if (!string.IsNullOrEmpty(displayName))
                {
                    var profile = new UserProfile { DisplayName = displayName };
                    await _currentUser.UpdateUserProfileAsync(profile).AsUniTask();
                }

                Debug.Log($"[AuthManager] Account created successfully. User: {PlayerName} (ID: {PlayerID})");

                // Firestore에 유저 데이터 저장
                await SaveUserDataToDatabase();

                return true;
            }

            Debug.LogError("[AuthManager] Account creation failed");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Account creation error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 이메일/비밀번호 로그인
    /// </summary>
    public async UniTask<bool> SignInWithEmailAsync(string email, string password, Action onEventFinished = null)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AuthManager] Firebase Auth not initialized");
            return false;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("[AuthManager] Email or password is empty");
            return false;
        }

        try
        {
            Debug.Log($"[AuthManager] Signing in with email: {email}...");

            var credential = await _auth.SignInWithEmailAndPasswordAsync(email, password).AsUniTask();

            if (credential != null && credential.User != null)
            {
                _currentUser = credential.User;
                Debug.Log($"[AuthManager] Email sign-in successful. User: {PlayerName} (ID: {PlayerID})");

                // Firestore에 유저 데이터 저장
                await SaveUserDataToDatabase();

                onEventFinished?.Invoke();
                return true;
            }

            Debug.LogError("[AuthManager] Email sign-in failed");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Email sign-in error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 로그아웃
    /// </summary>
    public void SignOut()
    {
        if (_auth != null)
        {
            _auth.SignOut();
            _currentUser = null;
            Debug.Log("[AuthManager] User signed out");
        }
    }

    /// <summary>
    /// 현재 인증 상태 확인
    /// </summary>
    public bool IsPlayerAuthenticated()
    {
        return _currentUser != null && !_currentUser.IsAnonymous;
    }

    /// <summary>
    /// 유저 데이터를 Firebase Firestore에 저장
    /// </summary>
    public async UniTask<bool> SaveUserDataToDatabase()
    {
        if (_currentUser == null)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated. Cannot save data.");
            return false;
        }

        if (!DatabaseSubManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[AuthManager] DatabaseSubManager not initialized.");
            return false;
        }

        try
        {
            // 유저 데이터 구조 생성
            var userData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "userId", PlayerID },
                { "displayName", PlayerName },
                { "email", PlayerEmail },
                { "isAnonymous", _currentUser.IsAnonymous },
                { "lastLogin", DateTime.UtcNow.ToString("o") },
                { "createdAt", _currentUser.Metadata.CreationTimestamp }
            };

            // Firebase Firestore에 저장 (users 컬렉션의 {userId} 문서)
            bool success = await DatabaseSubManager.Instance.SetDocumentAsync("users", PlayerID, userData);

            if (success)
            {
                Debug.Log($"[AuthManager] User data saved to Firestore: {PlayerName}");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Failed to save user data: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Firebase Firestore에서 유저 데이터 로드
    /// </summary>
    public async UniTask<bool> LoadUserDataFromDatabase()
    {
        if (_currentUser == null)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated. Cannot load data.");
            return false;
        }

        if (!DatabaseSubManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[AuthManager] DatabaseSubManager not initialized.");
            return false;
        }

        try
        {
            // Firestore에서 문서 읽기 (users 컬렉션의 {userId} 문서)
            var snapshot = await DatabaseSubManager.Instance.GetDocumentAsync("users", PlayerID);

            if (snapshot != null && snapshot.Exists)
            {
                // Firestore 데이터 파싱
                var data = snapshot.ToDictionary();
                var displayName = data.ContainsKey("displayName") ? data["displayName"]?.ToString() : null;
                var email = data.ContainsKey("email") ? data["email"]?.ToString() : null;
                var lastLogin = data.ContainsKey("lastLogin") ? data["lastLogin"]?.ToString() : null;

                Debug.Log($"[AuthManager] User data loaded from Firestore");
                Debug.Log($"[AuthManager] Name: {displayName}, Email: {email}, Last Login: {lastLogin}");

                return true;
            }
            else
            {
                Debug.LogWarning($"[AuthManager] No user data found for {PlayerID}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Failed to load user data: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 비밀번호 재설정 이메일 전송
    /// </summary>
    public async UniTask<bool> SendPasswordResetEmailAsync(string email)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[AuthManager] Firebase Auth not initialized");
            return false;
        }

        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("[AuthManager] Email is empty");
            return false;
        }

        try
        {
            await _auth.SendPasswordResetEmailAsync(email).AsUniTask();
            Debug.Log($"[AuthManager] Password reset email sent to {email}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Password reset email error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 사용자 프로필 업데이트
    /// </summary>
    public async UniTask<bool> UpdateUserProfileAsync(string displayName = null, Uri photoUrl = null)
    {
        if (_currentUser == null)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated");
            return false;
        }

        try
        {
            var profile = new UserProfile();

            if (!string.IsNullOrEmpty(displayName))
                profile.DisplayName = displayName;

            if (photoUrl != null)
                profile.PhotoUrl = photoUrl;

            await _currentUser.UpdateUserProfileAsync(profile).AsUniTask();
            Debug.Log($"[AuthManager] User profile updated: {displayName}");

            // Firestore 데이터도 업데이트
            await SaveUserDataToDatabase();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Profile update error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 계정 삭제
    /// </summary>
    public async UniTask<bool> DeleteAccountAsync()
    {
        if (_currentUser == null)
        {
            Debug.LogWarning("[AuthManager] User is not authenticated");
            return false;
        }

        try
        {
            var userId = PlayerID;

            // Firebase Auth 계정 삭제
            await _currentUser.DeleteAsync().AsUniTask();
            Debug.Log("[AuthManager] User account deleted");

            // Firestore 데이터 삭제
            if (DatabaseSubManager.Instance.IsInitialized)
            {
                await DatabaseSubManager.Instance.DeleteDocumentAsync("users", userId);
            }

            _currentUser = null;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Account deletion error: {ex.Message}");
            return false;
        }
    }
}
