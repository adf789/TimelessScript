using Newtonsoft.Json;
using System;

namespace Assets.SimpleSignIn.Apple.Scripts
{
    /// <summary>
    /// JWT specification: https://developer.apple.com/documentation/sign_in_with_apple/sign_in_with_apple_rest_api/authenticating_users_with_sign_in_with_apple#3383773
    /// </summary>
    [Serializable]
    public class UserInfo
    {
        [JsonProperty("sub")]
        public string Id;

        [JsonProperty("email")]
        public string Email;

        [JsonProperty("email_verified")]
        public bool EmailVerified;

        [JsonProperty("is_private_email")]
        public bool IsPrivateEmail;

        [JsonProperty("real_user_status")]
        public int RealUserStatus;

        /// <summary>
        /// Apple only returns the user object the first time the user authorizes the app.
        /// Validate and persist this information from your app to your server.
        /// Subsequent authorization requests do not contain the user object, however, the user's email is provided in the identity token for all requests.
        /// https://developer.apple.com/documentation/sign_in_with_apple/sign_in_with_apple_js/configuring_your_webpage_for_sign_in_with_apple#3331292
        /// Notice:
        /// For debugging, you can get user name again by visiting Apple ID > Sign-In and Security > Sign in with Apple > Select app > Stop using Sign in with Apple.
        /// Alternatively, Apple will return user name again after revoking access tokens.
        /// </summary>
        public string Name;
        public string FirstName;
        public string LastName;
    }
}