using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleSignIn.Facebook.Scripts
{
    /// <summary>
    /// https://developers.facebook.com/docs/facebook-login/limited-login/token/
    /// </summary>
    public partial class FacebookAuth
    {
        public SavedAuth SavedAuth { get; private set; }
        public TokenResponse TokenResponse { get; private set; }
        public string ClientId => _settings.ClientId;
        public bool DebugLog = true;

        private const string AuthorizationEndpoint = "https://www.facebook.com/v11.0/dialog/oauth";
        private const string TokenEndpoint = "https://graph.facebook.com/v11.0/oauth/access_token";
        private const string UserInfoEndpoint = "https://graph.facebook.com/v17.0/me?fields=id,name,email";
        private const string RevocationEndpoint = "https://graph.facebook.com/{user-id}/permissions";

        private readonly FacebookAuthSettings _settings;
        private Implementation _implementation;
        private string _redirectUri, _state, _codeVerifier;
        private Action<bool, string, UserInfo> _callbackU;
        private Action<bool, string, TokenResponse> _callbackT;

        public FacebookAuth(FacebookAuthSettings settings = null)
        {
            _settings = settings == null ? Resources.Load<FacebookAuthSettings>("FacebookAuthSettings") : settings;

            if (_settings == null) throw new NullReferenceException(nameof(_settings));

            SavedAuth = SavedAuth.GetInstance(_settings.ClientId);
            Application.deepLinkActivated += OnDeepLinkActivated;

            #if UNITY_IOS && !UNITY_EDITOR

            SafariViewController.DidCompleteInitialLoad += DidCompleteInitialLoad;
            SafariViewController.DidFinish += UserCancelledHook;

            #endif

            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            WindowsDeepLinking.Initialize(_settings.CustomUriScheme, OnDeepLinkActivated);

            #endif
        }

        ~FacebookAuth()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;

            #if UNITY_IOS && !UNITY_EDITOR

            SafariViewController.DidCompleteInitialLoad -= DidCompleteInitialLoad;
            SafariViewController.DidFinish -= UserCancelledHook;

            #endif
        }

        public void SignIn(Action<bool, string, UserInfo> callback, bool caching = true)
        {
            _callbackU = callback;
            _callbackT = null;

            Initialize();

            if (SavedAuth == null)
            {
                Auth();
            }
            else if (caching && SavedAuth.UserInfo != null)
            {
                callback(true, null, SavedAuth.UserInfo);
            }
            else
            {
                UseSavedToken();
            }
        }

        public void GetTokenResponse(Action<bool, string, TokenResponse> callback)
        {
            _callbackU = null;
            _callbackT = callback;

            Initialize();

            if (SavedAuth == null || SavedAuth.TokenResponse.Expired)
            {
                Auth();
            }
            else
            {
                callback(true, null, SavedAuth.TokenResponse);
            }
        }

        public void SignOut(bool revokeAccessToken = false)
        {
            TokenResponse = null;

            if (SavedAuth != null)
            {
                if (revokeAccessToken && SavedAuth.TokenResponse != null)
                {
                    string userId = null;

                    if (SavedAuth.UserInfo != null)
                    {
                        userId = SavedAuth.UserInfo.id;
                    }
                    else if (SavedAuth.TokenResponse.IdToken != null)
                    {
                        userId = (JObject.Parse(new JWT(SavedAuth.TokenResponse.IdToken).Payload)["sub"] ?? throw new Exception("Can not get user ID from JWT.")).Value<string>();
                    }

                    if (userId != null)
                    {
                        RevokeAccessToken(userId, SavedAuth.TokenResponse.AccessToken);
                    }
                }

                SavedAuth.Delete();
                SavedAuth = null;
            }
        }

        private void Initialize()
        {
            #if UNITY_EDITOR

            //_implementation = Implementation.LoopbackFlow; // LoopbackFlow will work only for App Mode: Development.
            //_redirectUri = $"http://localhost:{Helpers.GetRandomUnusedPort()}/";

            _implementation = Implementation.AuthorizationMiddleware;
            _redirectUri = "";
            
            #elif UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || UNITY_WSA

            _implementation = Implementation.AuthorizationMiddleware;
            _redirectUri = $"{_settings.CustomUriScheme}:/oauth2/facebook";

            #elif UNITY_WEBGL

            _implementation = Implementation.AuthorizationMiddleware;
            _redirectUri = "";

            #endif

            if (SavedAuth != null && SavedAuth.ClientId != _settings.ClientId)
            {
                SavedAuth.Delete();
                SavedAuth = null;
            }
        }

        private void Auth()
        {
            _state = Guid.NewGuid().ToString("N");
            _codeVerifier = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";

            #if UNITY_IOS && !UNITY_EDITOR

            if (!_settings.UseSafariViewController) ApplicationFocusHook.Create(UserCancelledHook);

            #else

            ApplicationFocusHook.Create(UserCancelledHook);

            #endif

            var codeChallenge = Helpers.CreateCodeChallenge(_codeVerifier);
            var redirectUri = _implementation == Implementation.AuthorizationMiddleware ? AuthorizationMiddleware.Endpoint + "/redirect" : _redirectUri;
            var scope = string.Join(" ", _settings.AccessScopes);
            var authorizationRequest = $"{AuthorizationEndpoint}?client_id={_settings.ClientId}&scope={Uri.EscapeDataString(scope)}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={_state}&code_challenge={codeChallenge}&code_challenge_method=S256&nonce={Guid.NewGuid()}";

            if (_implementation == Implementation.AuthorizationMiddleware)
            {
                #if UNITY_EDITOR || UNITY_WEBGL

                AuthorizationMiddleware.Auth(_redirectUri, _state, () => AuthorizationRequest(authorizationRequest), (success, error, code) =>
                {
                    if (success)
                    {
                        PerformCodeExchange(code);
                    }
                    else
                    {
                        _callbackU?.Invoke(false, error, null);
                        _callbackT?.Invoke(false, error, null);
                    }
                });

                #elif UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || UNITY_WSA

                AuthorizationMiddleware.Auth(_redirectUri, _state, (success, error) =>
                {
                    if (success)
                    {
                        AuthorizationRequest(authorizationRequest);
                    }
                    else
                    {
                        _callbackU?.Invoke(false, error, null);
                        _callbackT?.Invoke(false, error, null);
                    }
                });

                #endif
            }
            else
            {
                AuthorizationRequest(authorizationRequest);
                
                switch (_implementation)
                {
                    case Implementation.LoopbackFlow:
                        LoopbackFlow.Initialize(_redirectUri, OnDeepLinkActivated);
                        break;
                }
            }
        }

        private void AuthorizationRequest(string url)
        {
            Log($"Authorization: {url}");

            #if UNITY_IOS && !UNITY_EDITOR

            if (_settings.UseSafariViewController)
            {
                SafariViewController.OpenURL(url);
            }
            else
            {
                Application.OpenURL(url);
            }

            #else

            Application.OpenURL(url);

            #endif
        }

        private void DidCompleteInitialLoad(bool loaded)
        {
            if (loaded) return;

            const string error = "Failed to load auth screen.";

            _callbackT?.Invoke(false, error, null);
            _callbackU?.Invoke(false, error, null);
        }

        private async void UserCancelledHook()
        {
            var time = Time.time;

            while (Time.time < time + 1)
            {
                await Task.Yield();
            }

            if (_codeVerifier == null) return;

            _codeVerifier = null;

            const string error = "User cancelled.";

            _callbackT?.Invoke(false, error, null);
            _callbackU?.Invoke(false, error, null);
        }

        private void UseSavedToken()
        {
            if (SavedAuth == null || SavedAuth.ClientId != _settings.ClientId || SavedAuth.TokenResponse.Expired)
            {
                SignOut();
                SignIn(_callbackU);
            }
            else if (!SavedAuth.TokenResponse.Expired)
            {
                Log("Using saved access token...");
                RequestUserInfo(SavedAuth.TokenResponse.AccessToken, (success, _, userInfo) =>
                {
                    if (success)
                    {
                        _callbackU(true, null, userInfo);
                    }
                    else
                    {
                        SignOut();
                        SignIn(_callbackU);
                    }
                });
            }
        }

        private void OnDeepLinkActivated(string deepLink)
        {
            Log($"Deep link activated: {deepLink}");

            deepLink = deepLink.Replace(":///", ":/"); // Some browsers may add extra slashes.

            if (_redirectUri == null || !deepLink.StartsWith(_redirectUri) || _codeVerifier == null)
            {
                Log("Unexpected deep link.");
                return;
            }

            #if UNITY_IOS && !UNITY_EDITOR

            if (_settings.UseSafariViewController)
            {
                Log($"Closing SafariViewController");
                SafariViewController.Close();
            }

            #endif

            var parameters = Helpers.ParseQueryString(deepLink);
            var error = parameters.Get("error");

            if (error != null)
            {
                _callbackU?.Invoke(false, error, null);
                _callbackT?.Invoke(false, error, null);
                return;
            }

            var state = parameters.Get("state");
            var code = parameters.Get("code");
            
            if (state == null || code == null) return;

            if (state == _state)
            {
                PerformCodeExchange(code);
            }
            else
            {
                Log("Unexpected response.");
            }
        }

        private void PerformCodeExchange(string code)
        {
            var redirectUri = _implementation == Implementation.AuthorizationMiddleware ? AuthorizationMiddleware.Endpoint + "/redirect" : _redirectUri;
            var exchangeRequest = $"{TokenEndpoint}?client_id={_settings.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&code_verifier={_codeVerifier}&code={code}";
            var request = UnityWebRequest.Get(exchangeRequest);

            _codeVerifier = null;

            Log($"Exchanging code for access token: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                if (request.error == null)
                {
                    Log($"TokenExchangeResponse={request.downloadHandler.text}");

                    TokenResponse = TokenResponse.Parse(request.downloadHandler.text);
                    SavedAuth = new SavedAuth(_settings.ClientId, TokenResponse);
                    SavedAuth.Save();

                    if (_callbackT != null)
                    {
                        _callbackT(true, null, TokenResponse);
                    }

                    if (_callbackU != null)
                    {
                        RequestUserInfo(TokenResponse.AccessToken, _callbackU);
                    }
                }
                else
                {
                    _callbackU?.Invoke(false, request.GetError(), null);
                    _callbackT?.Invoke(false, request.GetError(), null);
                }

                request.Dispose();
            };
        }

        /// <summary>
        /// You can move this function to your backend for more security.
        /// </summary>
        public void RequestUserInfo(string accessToken, Action<bool, string, UserInfo> callback)
        {
            var request = UnityWebRequest.Get(UserInfoEndpoint);

            Log($"Requesting user info: {request.url}");

            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SendWebRequest().completed += _ =>
            {
                if (request.error == null)
                {
                    Log($"UserInfo={request.downloadHandler.text}");
                    SavedAuth.UserInfo = JsonUtility.FromJson<UserInfo>(request.downloadHandler.text);
                    SavedAuth.Save();
                    callback(true, null, SavedAuth.UserInfo);
                }
                else
                {
                    callback(false, request.GetError(), null);
                }

                request.Dispose();
            };
        }

        private void RevokeAccessToken(string userId, string accessToken)
        {
            var request = UnityWebRequest.Delete(RevocationEndpoint.Replace("{user-id}", userId));

            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

            Log($"Revoking access token: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                Log(request.error ?? "Access token revoked!");
                request.Dispose();
            };
        }

        private void Log(string message)
        {
            if (DebugLog)
            {
                Debug.Log(message); // TODO: Remove in Release.
            }
        }
    }
}