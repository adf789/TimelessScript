using System.Collections.Generic;
using UnityEngine;

namespace Assets.SimpleSignIn.Apple.Scripts
{
    [CreateAssetMenu(fileName = "AppleAuthSettings", menuName = "Simple Sign-In/Auth Settings/Apple")]
    public class AppleAuthSettings : ScriptableObject
    {
        public string Id = "Default";
        public string ClientId;
        public string TeamId;
        public string PrivateKeyId;
        public string PrivateKey;
        public string CustomUriScheme;

        [Header("Options")]
        public List<string> AccessScopes = new() { "name", "email" };
        [Tooltip("`AppleAuth.Cancel()` method should be called manually. `User cancelled` callback will not called automatically when the user returns to the app without performing auth.")]
        public bool ManualCancellation;
        [Tooltip("Use Safari API on iOS instead of a default web browser. This option is required for passing App Store review.")]
        public bool UseSafariViewController = true;

        public string Validate()
        {
#if UNITY_EDITOR

            const string androidManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";

            if (!System.IO.File.Exists(androidManifestPath))
            {
                return $"Android manifest is missing: {androidManifestPath}";
            }

            var scheme = $"<data android:scheme=\"{CustomUriScheme}\" />";

            if (!System.IO.File.ReadAllText(androidManifestPath).Contains(scheme))
            {
                return $"Custom URI scheme (deep linking) is missing in AndroidManifest.xml: {scheme}";
            }

#endif

            return null;
        }
    }
}