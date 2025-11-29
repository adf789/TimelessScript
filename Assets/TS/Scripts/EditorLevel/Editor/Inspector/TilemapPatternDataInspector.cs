
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof(TilemapPatternData))]
public class TilemapPatternDataInspector : Editor
{
    private TilemapPatternData _inspectorTarget;
    private SerializedProperty _mapLinkInfo;

    private void OnEnable()
    {
        _inspectorTarget = (TilemapPatternData) target;
        _mapLinkInfo = serializedObject.FindProperty("_mapLinkInfo");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginDisabledGroup(true);
        {
            EditorGUILayout.PropertyField(_mapLinkInfo);
        }
        EditorGUI.EndDisabledGroup();
    }
}
