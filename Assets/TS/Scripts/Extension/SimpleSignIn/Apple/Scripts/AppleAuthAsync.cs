using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Assets.SimpleSignIn.Apple.Scripts
{
    public partial class AppleAuth
    {
        /// <summary>
        /// Performs sign-in and returns UserInfo asynchronously.
        /// </summary>
        public async Task<UserInfo> SignInAsync(bool caching = true, string nonce = null)
        {
            var completed = false;
            string error = null;
            UserInfo userInfo = null;

            SignIn((success, e, result) =>
            {
                if (success)
                {
                    userInfo = result;
                }
                else
                {
                    error = e;
                }

                completed = true;
            }, caching, nonce);

            while (!completed)
            {
                await Task.Yield();
            }

            if (userInfo == null) throw new Exception(error);

            Log($"userInfo={JsonConvert.SerializeObject(userInfo)}");

            return userInfo;
        }

        /// <summary>
        /// Returns TokenResponse asynchronously.
        /// </summary>
        public async Task<TokenResponse> GetTokenResponseAsync(string nonce = null)
        {
            var completed = false;
            string error = null;
            TokenResponse tokenResponse = null;

            GetTokenResponse((success, e, result) =>
            {
                if (success)
                {
                    tokenResponse = result;
                }
                else
                {
                    error = e;
                }

                completed = true;
            }, nonce);

            while (!completed)
            {
                await Task.Yield();
            }

            if (tokenResponse == null) throw new Exception(error);

            Log($"TokenResponse={JsonConvert.SerializeObject(TokenResponse)}");

            return tokenResponse;
        }
    }
}