using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ScriptDeletionHandler : AssetModificationProcessor
{
    public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        if (assetPath.EndsWith(".cs"))
        {
            string scriptName = Path.GetFileNameWithoutExtension(assetPath);
            RemoveScriptFromPrefabs(scriptName);
        }

        return AssetDeleteResult.DidNotDelete;
    }

    private static void RemoveScriptFromPrefabs(string scriptName)
    {
        // 모든 프리팹 찾기
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null) continue;

            bool modified = false;
            var components = prefab.GetComponentsInChildren<Component>(true);

            foreach (var component in components)
            {
                if (!component && component.GetType().Name == scriptName)
                {
                    Debug.Log($"[ScriptDeletionHandler] '{prefab.name}' 프리팹에서 삭제된 스크립트 발견, 제거 중...");
                    GameObject target = component.gameObject;
                    var serializedObject = new SerializedObject(target);
                    var property = serializedObject.FindProperty("m_Component");

                    for (int i = property.arraySize - 1; i >= 0; i--)
                    {
                        var element = property.GetArrayElementAtIndex(i);
                        var objRef = element.objectReferenceValue;
                        if (objRef == null)
                        {
                            property.DeleteArrayElementAtIndex(i);
                            modified = true;
                        }
                    }

                    serializedObject.ApplyModifiedProperties();
                }
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[ScriptDeletionHandler] '{prefab.name}' 프리팹에서 삭제된 스크립트를 제거하고 저장했습니다.");
            }
        }
    }
}
