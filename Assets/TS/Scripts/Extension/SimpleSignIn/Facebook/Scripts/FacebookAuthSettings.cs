using System.Collections.Generic;
using UnityEngine;

namespace Assets.SimpleSignIn.Facebook.Scripts
{
    [CreateAssetMenu(fileName = "FacebookAuthSettings", menuName = "Simple Sign-In/Auth Settings/Facebook")]
    public class FacebookAuthSettings : ScriptableObject
    {
        public string Id = "Default";
        public string ClientId;
        public string CustomUriScheme;
        public List<string> AccessScopes = new() { "openid" };
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