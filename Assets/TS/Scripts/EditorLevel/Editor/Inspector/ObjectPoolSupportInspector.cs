
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof(ObjectPoolSupport))]
public class ObjectPoolSupportInspector : Editor
{
    private ObjectPoolSupport inspectorTarget;
    private GameObject gameObject;
    private SerializedProperty guidProperty;
    private SerializedProperty parentProperty;

    private void OnEnable()
    {
        inspectorTarget = (ObjectPoolSupport) target;
        guidProperty = serializedObject.FindProperty("guid");
        parentProperty = serializedObject.FindProperty("parent");

        gameObject = LoadObject(guidProperty.stringValue);
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            gameObject = (GameObject) EditorGUILayout.ObjectField("Target", gameObject, typeof(GameObject));
            EditorGUILayout.PropertyField(parentProperty);

            if (gameObject != null)
                EditorGUILayout.LabelField("GUID", guidProperty.stringValue);
            else
                EditorGUILayout.LabelField("타겟 없음");
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
