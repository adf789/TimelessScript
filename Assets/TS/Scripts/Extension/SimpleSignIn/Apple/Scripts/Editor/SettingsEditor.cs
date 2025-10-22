using UnityEditor;
using UnityEngine;

namespace Assets.SimpleSignIn.Apple.Scripts.Editor
{
    [CustomEditor(typeof(AppleAuthSettings))]
    public class SettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (AppleAuthSettings) target;
            var warning = settings.Validate();

            if (warning != null)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            DrawDefaultInspector();

            if (GUILayout.Button("Certificates, Identifiers & Profiles"))
            {
                Application.OpenURL("https://developer.apple.com/account/resources/identifiers/list");
            }

            if (GUILayout.Button("Wiki"))
            {
                Application.OpenURL("https://github.com/hippogamesunity/SimpleSignIn/wiki/Apple");
            }
        }
    }
}