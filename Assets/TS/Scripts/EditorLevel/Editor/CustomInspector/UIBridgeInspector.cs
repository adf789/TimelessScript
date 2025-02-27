#if UNITY_EDITOR
using UnityEditor;

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
        foreach(var pair in wrapperTarget.Controllers)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(pair.Key.ToString());
                EditorGUILayout.LabelField(pair.Value.GetType().FullName);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
    }
}
#endif
