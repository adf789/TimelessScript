using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Assets.SimpleSignIn.Facebook.Scripts
{
    public class TokenResponse
    {
        /// <summary>
        /// The token that your application sends to authorize a Facebook API request.
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken;

        /// <summary>
        /// The type of token returned. At this time, this field's value is always set to Bearer.
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType;

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