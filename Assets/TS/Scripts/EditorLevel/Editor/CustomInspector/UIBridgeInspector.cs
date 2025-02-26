#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(UIBridge))]
public class UIBridgeInspector : Editor
{
    private SerializedProperty bridges = null;

    private void OnEnable()
    {
        bridges = serializedObject.FindProperty("bridges");

        foreach(var property in serializedObject.GetIterator())
        {

        }
    }

    public override void OnInspectorGUI()
    {
        if (bridges != null)
        {
            for (int i = 0; i < bridges.arraySize; i++)
            {

            }
        }
    }
}
#endif
