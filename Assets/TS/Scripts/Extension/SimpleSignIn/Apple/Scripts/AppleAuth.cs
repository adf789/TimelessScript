using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleSignIn.Apple.Scripts
{
    /// <summary>
    /// Documentation:
    /// https://developer.apple.com/documentation/accountorganizationaldatasharing
    /// </summary>
    public partial class AppleAuth
    {
        public SavedAuth SavedAuth { get; private set; }
        public TokenResponse TokenResponse { get; private set; }
        public string ClientId => _settings.ClientId;
        public bool DebugLog = true;

        private const string AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize";
        private const string TokenEndpoint = "https://appleid.apple.com/auth/token";
        private const string RevocationEndpoint = "https://appleid.apple.com/auth/revoke";

        private readonly AppleAuthSettings _settings;
        private string _redirectUri, _state;
        private Action<bool, string, UserInfo> _callbackU;
        private Action<bool, string, TokenResponse> _callbackT;

        /// <summary>
        /// A constructor that accepts an instance of AppleAuthSettings. If Null is passed, it will load default settings from Resources (AppleAuthSettings scriptable object).
        /// </summary>
        public AppleAuth(AppleAuthSettings settings = null)
        {
            _settings = settings == null ? Resources.Load<AppleAuthSettings>("AppleAuthSettings") : settings;

            if (_settings == null) throw new NullReferenceException(nameof(_settings));

            SavedAuth = SavedAuth.GetInstance(_settings.ClientId);
            Application.deepLinkActivated += OnDeepLinkActivated;

            #if UNITY_IOS && !UNITY_EDITOR

            SafariViewController.DidCompleteInitialLoad += DidCompleteInitialLoad;
            SafariViewController.DidFinish += UserCancelledHook;

            #endif
        }

        /// <summary>
        /// A destructor.
        /// </summary>
        ~AppleAuth()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;

            #if UNITY_IOS && !UNITY_EDITOR

            SafariViewController.DidCompleteInitialLoad -= DidCompleteInitialLoad;
            SafariViewController.DidFinish -= UserCancelledHook;

            #endif
        }

        /// <summary>
        /// Performs sign-in and returns an instance of UserInfo with `callback`. If `caching` is True, it will return the previously saved UserInfo.
        /// </summary>
        public void SignIn(Action<bool, string, UserInfo> callback, bool caching = true, string nonce = null)
        {
            _callbackU = callback;
            _callbackT = null;

            Initialize();

            if (SavedAuth == null || nonce != null)
            {
                Auth(nonce);
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

        /// <summary>
        /// Returns an instance of TokenResponse which contains AccessToken and other related information (expiration, type and other). It may also contain IdToken (JWT), which contains information about the user.
        /// </summary>
        public void GetTokenResponse(Action<bool, string, TokenResponse> callback, string nonce = null)
        {
            _callbackU = null;
            _callbackT = callback;

            Initialize();

            if (SavedAuth == null || nonce != null)
            {
                Auth(nonce);
            }
            else
            {
                if (SavedAuth.TokenResponse.Expired)
                {
                    Log("Refreshing expired access token...");
                    RefreshAccessToken(callback);
                }
                else
                {
                    callback(true, null, SavedAuth.TokenResponse);
                }
            }
        }

        /// <summary>
        /// Performs sign-out.
        /// </summary>
        public void SignOut(bool revokeAccessToken = false)
        {
            TokenResponse = null;

            if (SavedAuth != null)
            {
                if (revokeAccessToken && SavedAuth.TokenResponse != null)
                {
                    RevokeAccessToken(SavedAuth.TokenResponse.AccessToken);
                }

                SavedAuth.Delete();
                SavedAuth = null;
            }
        }

        /// <summary>
        /// Force cancel.
        /// </summary>
        public void Cancel()
        {
            _redirectUri = _state = null;
            _callbackU = null;
            _callbackT = null;
            ApplicationFocusHook.Cancel();
        }

        private const string TempKey = "oauth.temp";

        /// <summary>
        /// This can be called on app startup to continue oauth.
        /// In some scenarios, the app may be terminated while the user performs sign-in.
        /// </summary>
        public void TryResume(Action<bool, string, UserInfo> callbackUserInfo = null, Action<bool, string, TokenResponse> callbackTokenResponse = null)
        {
            if (string.IsNullOrEmpty(Application.absoluteURL) || !PlayerPrefs.HasKey(TempKey)) return;

            var parts = PlayerPrefs.GetString(TempKey).Split('|');

            if (!Application.absoluteURL.StartsWith(parts[2])) return;

            _state = parts[0];
            _redirectUri = parts[1];
            _callbackU = callbackUserInfo;
            _callbackT = callbackTokenResponse;

            OnDeepLinkActivated(Application.absoluteURL);
        }

        private void Initialize()
        {
            #if UNITY_EDITOR || UNITY_WEBGL

            _redirectUri = "";
            
            #elif UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_WSA || UNITY_STANDALONE_WIN

            _redirectUri = $"{_settings.CustomUriScheme}:/oauth2/apple";

            #if UNITY_STANDALONE_WIN

            WindowsDeepLinking.Initialize(_settings.CustomUriScheme, OnDeepLinkActivated);

            #endif

            #endif

            if (SavedAuth != null && SavedAuth.ClientId != _settings.ClientId)
            {
                SavedAuth.Delete();
                SavedAuth = null;
            }
        }

        private void Auth(string nonce = null)
        {
            _state = Guid.NewGuid().ToString("N");
            
            nonce ??= Guid.NewGuid().ToString("N");

            PlayerPrefs.SetString(TempKey, $"{_state}|{_redirectUri}");
            PlayerPrefs.Save();

            if (!_settings.ManualCancellation)
            {
                #if UNITY_IOS && !UNITY_EDITOR

                if (!_settings.UseSafariViewController) ApplicationFocusHook.Create(UserCancelledHook);

                #else

                ApplicationFocusHook.Create(UserCancelledHook);

                #endif
            }

            var redirectUri = AuthorizationMiddleware.Endpoint + "/apple_redirect";
            var authorizationRequest = $"{AuthorizationEndpoint}?client_id={_settings.ClientId}&nonce={Helpers.CreateSha256Hex(nonce)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_mode=form_post&response_type=code&scope={Uri.EscapeDataString(string.Join(" ", _settings.AccessScopes))}&state={_state}";

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
            //if (loaded) return;

            //const string error = "Failed to load auth screen.";

            //_callbackT?.Invoke(false, error, null);
            //_callbackU?.Invoke(false, error, null);
        }

        private async void UserCancelledHook()
        {
            if (_settings.ManualCancellation) return;

            var time = DateTime.UtcNow;

            while ((DateTime.UtcNow - time).TotalSeconds < 1)
            {
                await Task.Yield();
            }

            if (_state == null) return;

            _state = null;

            const string error = "User cancelled.";

            _callbackT?.Invoke(false, error, null);
            _callbackU?.Invoke(false, error, null);
        }

        private void UseSavedToken()
        {
            if (SavedAuth == null || SavedAuth.ClientId != _settings.ClientId)
            {
                SignOut();
                SignIn(_callbackU);
            }
            else if (!SavedAuth.TokenResponse.Expired)
            {
                Log("Using saved access token...");
                _callbackU(true, null, SavedAuth.UserInfo);
            }
            else
            {
                Log("Refreshing expired access token...");
                RefreshAccessToken((success, _, _) =>
                {
                    if (success)
                    {
                        _callbackU(true, null, SavedAuth.UserInfo);
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

            if (_redirectUri == null || !deepLink.StartsWith(_redirectUri) || _state == null)
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
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(code)); Debug.Log(json);
            var jObject = JObject.Parse(json);
            
            code = (jObject["code"] ?? throw new Exception("Code expected.")).Value<string>();

            var redirectUri = AuthorizationMiddleware.Endpoint + "/apple_redirect";
            var formFields = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", AppleJWT.CreateClientSecret(_settings) },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", redirectUri }
            };

            _state = null;

            var request = CreateWebRequest(TokenEndpoint, formFields);

            Log($"Exchanging code for access token: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                var error = GetError(request);

                if (error == null)
                {
                    Log($"TokenExchangeResponse={request.downloadHandler.text}");

                    TokenResponse = TokenResponse.Parse(request.downloadHandler.text);
                    SavedAuth = new SavedAuth(_settings.ClientId, TokenResponse);

                    var jwt = new JWT(TokenResponse.IdToken);
                    
                    SavedAuth.UserInfo = JsonConvert.DeserializeObject<UserInfo>(jwt.Payload);

                    var user = jObject["user"];

                    if (user != null && user.HasValues)
                    {
                        SavedAuth.UserInfo.FirstName = user["name"]["firstName"].Value<string>();
                        SavedAuth.UserInfo.LastName = user["name"]["lastName"].Value<string>();
                        SavedAuth.UserInfo.Name = $"{SavedAuth.UserInfo.FirstName} {SavedAuth.UserInfo.LastName}";
                    }

                    SavedAuth.Save();

                    if (_callbackT != null)
                    {
                        _callbackT(true, null, TokenResponse);
                    }

                    if (_callbackU != null)
                    {
                        _callbackU(true, null, SavedAuth.UserInfo);
                    }
                }
                else
                {
                    _callbackU?.Invoke(false, error, null);
                    _callbackT?.Invoke(false, error, null);
                }

                request.Dispose();
            };

            if (PlayerPrefs.HasKey(TempKey))
            {
                PlayerPrefs.DeleteKey(TempKey);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// https://developer.apple.com/documentation/sign_in_with_apple/generate_and_validate_tokens
        /// </summary>
        public void RefreshAccessToken(Action<bool, string, TokenResponse> callback)
        {
            if (SavedAuth == null) throw new Exception("Initial authorization is required.");

            var refreshToken = SavedAuth.TokenResponse.RefreshToken;
            var formFields = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", AppleJWT.CreateClientSecret(_settings) },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };
            var request = CreateWebRequest(TokenEndpoint, formFields);

            Log($"Access token refresh: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                var error = GetError(request);

                if (error == null)
                {
                    Log($"TokenExchangeResponse={request.downloadHandler.text}");

                    TokenResponse = TokenResponse.Parse(request.downloadHandler.text);
                    TokenResponse.RefreshToken = refreshToken;
                    SavedAuth.TokenResponse = TokenResponse;
                    SavedAuth.Save();
                    callback(true, null, TokenResponse);
                }
                else
                {
                    Debug.LogError(error);
                    callback(false, error, null);
                }

                request.Dispose();
            };
        }

        private void RevokeAccessToken(string accessToken)
        {
            var formFields = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "client_secret", AppleJWT.CreateClientSecret(_settings) },
                { "token", accessToken }
            };
            var request = CreateWebRequest(RevocationEndpoint, formFields);

            Log($"Revoking access token: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                Log(request.error ?? "Access token revoked!");
                request.Dispose();
            };
        }

        private static UnityWebRequest CreateWebRequest(string url, Dictionary<string, string> formFields = null)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR // CORS workaround.

            var dict = new Dictionary<string, string> { { "url", url } };

            if (formFields != null)
            {
                dict.Add("form", JsonConvert.SerializeObject(formFields));
            }

            return UnityWebRequest.Post($"{AuthorizationMiddleware.Endpoint}/download", dict);
            
            #else

            return formFields == null ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, formFields);

            #endif
        }

        private string GetError(UnityWebRequest request)
        {
            string error = null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.downloadHandler.text.StartsWith("{"))
                {
                    var json = request.downloadHandler.text;
                    var jObject = JObject.Parse(json);

                    if (jObject.ContainsKey("error")) error = (string) jObject["error"];
                }
            }
            else
            {
                error = request.GetError();
            }

            if (error != null)
            {
                Log($"Error: {error}");
            }

            return error;
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