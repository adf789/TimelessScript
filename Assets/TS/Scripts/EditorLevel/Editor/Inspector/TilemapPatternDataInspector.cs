
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof(TilemapPatternData))]
public class TilemapPatternDataInspector : Editor
{
    private TilemapPatternData _inspectorTarget;
    private SerializedProperty _portValues;

    private void OnEnable()
    {
        _inspectorTarget = (TilemapPatternData) target;
        _portValues = serializedObject.FindProperty("_portValues");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginDisabledGroup(true);
        {
            EditorGUILayout.PropertyField(_portValues);
        }
        EditorGUI.EndDisabledGroup();
    }
}
