#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIBridge))]
public class UIBridgeInspector : Editor
{
    private UIBridge wrapperTarget;

    private void OnEnable()
    {
        wrapperTarget = target as UIBridge;
    }

    public override void OnInspectorGUI()
    {
        if (wrapperTarget.Controllers.Count == 0)
            return;

        foreach (var pair in wrapperTarget.Controllers)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(pair.uiType.ToString());
                EditorGUILayout.LabelField(pair.typeName);

                if (GUILayout.Button("Á¦°Å"))
                {
                    wrapperTarget.Remove(pair.uiType);
                    break;
                }
            }
            EditorGUILayout.EndHorizontal();

            Type type = Type.GetType(pair.typeName);

            if (type != null)
            {
                BaseController controller = Activator.CreateInstance(type) as BaseController;

                if(controller != null)
                    EditorGUILayout.LabelField("Controller: " + controller);
            }

            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
