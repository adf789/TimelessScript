
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof(ObjectPoolSupport))]
public class ObjectPoolSupportInspector : Editor
{
    private ObjectPoolSupport inspectorTarget;
    private GameObject gameObject;
    private SerializedProperty guidProperty;

    private void OnEnable()
    {
        inspectorTarget = (ObjectPoolSupport) target;
        guidProperty = serializedObject.FindProperty("guid");

        gameObject = LoadObject(guidProperty.stringValue);
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            gameObject = (GameObject) EditorGUILayout.ObjectField(gameObject, typeof(GameObject));
            EditorGUILayout.LabelField("GUID", guidProperty.stringValue);
        }
        if (EditorGUI.EndChangeCheck())
        {
            if (gameObject != null)
            {
                var resourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<GameObject>();
                string path = AssetDatabase.GetAssetPath(gameObject);
                guidProperty.stringValue = AssetDatabase.GUIDFromAssetPath(path).ToString();

                resourcesPath.AddResourceFromObject(gameObject);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private GameObject LoadObject(string guid)
    {
        if (string.IsNullOrEmpty(guid))
            return null;

        GameObject loadObject = ResourcesTypeRegistry.Get().Load<GameObject>(guid);

        return loadObject;
    }
}
