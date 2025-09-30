using UnityEditor;
using UnityEngine;
using TS.MiddleLevel;

namespace TS.EditorLevel
{
    [CustomEditor(typeof(TweenComponent))]
    public class TweenComponentEditor : Editor
    {
        private SerializedProperty tweenDataProperty;
        private SerializedProperty graphicsProperty;
        private SerializedProperty spriteRenderersProperty;

        private void OnEnable()
        {
            graphicsProperty = serializedObject.FindProperty("graphics");
            spriteRenderersProperty = serializedObject.FindProperty("spriteRenderers");
            tweenDataProperty = serializedObject.FindProperty("tweenData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(graphicsProperty);
            EditorGUILayout.PropertyField(spriteRenderersProperty);
            EditorGUILayout.PropertyField(tweenDataProperty);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Controls", EditorStyles.boldLabel);

            TweenComponent tween = (TweenComponent) target;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play", GUILayout.Height(30)))
            {
                tween.Play();
            }
            if (GUILayout.Button("Stop", GUILayout.Height(30)))
            {
                tween.Stop();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Start"))
            {
                tween.ResetToStart();
            }
            if (GUILayout.Button("Reset to End"))
            {
                tween.ResetToEnd();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Start"))
            {
                tween.PreviewStart();
            }
            if (GUILayout.Button("Preview End"))
            {
                tween.PreviewEnd();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}