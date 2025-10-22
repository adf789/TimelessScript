using UnityEditor;
using UnityEngine;

namespace Assets.SimpleSignIn.Facebook.Scripts.Editor
{
    [CustomEditor(typeof(FacebookAuthSettings))]
    public class SettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (FacebookAuthSettings) target;
            var warning = settings.Validate();

            if (warning != null)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            DrawDefaultInspector();

            if (GUILayout.Button("Meta for Developers / My Apps"))
            {
                Application.OpenURL("https://developers.facebook.com/apps");
            }

            if (GUILayout.Button("Wiki"))
            {
                Application.OpenURL("https://github.com/hippogamesunity/SimpleSignIn/wiki/Facebook");
            }
        }
    }
}