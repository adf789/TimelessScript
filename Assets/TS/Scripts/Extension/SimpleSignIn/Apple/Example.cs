using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.SimpleSignIn.Apple.Scripts;

namespace Assets.SimpleSignIn.Apple
{
    public class Example : MonoBehaviour
    {
        public AppleAuth AppleAuth;
        public Text Log;
        public Text Output;
        
        public void Start()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            AppleAuth = new AppleAuth();
            AppleAuth.TryResume(OnSignIn, OnGetTokenResponse);
        }

        public void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        public void SignIn()
        {
            AppleAuth.SignIn(OnSignIn, caching: true);
        }

        public void SignOut()
        {
            AppleAuth.SignOut(revokeAccessToken: true);
            Output.text = "Not signed in";
        }

        public void GetAccessToken()
        {
            AppleAuth.GetTokenResponse(OnGetTokenResponse);
        }

        private void OnSignIn(bool success, string error, UserInfo userInfo)
        {
            Output.text = success ? $"Hello, {userInfo.Name}!" : error;
        }

        private void OnGetTokenResponse(bool success, string error, TokenResponse tokenResponse)
        {
            Output.text = success ? $"Access token: {tokenResponse.AccessToken}" : error;

            if (!success) return;

            var jwt = new JWT(tokenResponse.IdToken);

            Debug.Log($"JSON Web Token (JWT) Payload: {jwt.Payload}");
            
            jwt.ValidateSignature(AppleAuth.ClientId, OnValidateSignature);
        }

        private void OnValidateSignature(bool success, string error)
        {
            Output.text += Environment.NewLine;
            Output.text += success ? "JWT signature validated" : error;
        }

        public void Navigate(string url)
        {
            Application.OpenURL(url);
        }

        #region Research

        /// <summary>
        /// https://docs.unity.com/ugs/en-us/manual/authentication/manual/platform-signin-apple
        /// </summary>
        public async void UnityAuthentication()
        {
            //try
            //{
            //    var idToken = (await AppleAuth.GetTokenResponseAsync()).IdToken;

            //    await Unity.Services.Core.UnityServices.InitializeAsync();

            //    var authService = Unity.Services.Authentication.AuthenticationService.Instance;

            //    await authService.SignInWithAppleAsync(idToken);

            //    Debug.Log($"IsAuthorized={authService.IsSignedIn}");
            //}
            //catch (Exception e)
            //{
            //    Debug.LogException(e);
            //}
        }

        #endregion

        private void OnLogMessageReceived(string condition, string stackTrace, LogType logType)
        {
            Log.text += condition + '\n';
        }
    }
}