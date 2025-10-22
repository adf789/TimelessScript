using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Assets.SimpleSignIn.Apple.Scripts
{
    /// <summary>
    /// Generate and validate tokens: https://developer.apple.com/documentation/sign_in_with_apple/generate_and_validate_tokens
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// The token that your application sends to authorize API requests.
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken;

        /// <summary>
        /// The remaining lifetime of the access token in seconds.
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn;

        /// <summary>
        /// Note: This property is only returned if your request included an identity scope, such as openid, profile, or email.
        /// The value is a JSON Web Token (JWT) that contains digitally signed identity information about the user.
        /// </summary>
        [JsonProperty("id_token")]
        public string IdToken;

        /// <summary>
        /// A token that you can use to obtain a new access token.
        /// Refresh tokens are valid until the user revokes access.
        /// Note that refresh tokens are always returned for installed applications.
        /// </summary>
        [JsonProperty("refresh_token")]
        public string RefreshToken;

        /// <summary>
        /// The scopes of access granted by the access_token expressed as a list of space-delimited, case-sensitive strings.
        /// </summary>
        [JsonProperty("scope")]
        public string Scope;

        /// <summary>
        /// The type of token returned. At this time, this field's value is always set to Bearer.
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType;

        /// <summary>
        /// This aux property is calculated by the asset.
        /// </summary>
        public DateTime Expiration;

        public bool Expired => Expiration < DateTime.UtcNow;

        [Preserve]
        private TokenResponse()
        {
        }

        public static TokenResponse Parse(string json)
        {
            var response = JsonConvert.DeserializeObject<TokenResponse>(json);

            response.Expiration = DateTime.UtcNow.AddSeconds(response.ExpiresIn - 10);

            return response;
        }
    }
}