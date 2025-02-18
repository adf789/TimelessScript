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
            RemoveScriptFromPrefabs(assetPath, scriptName);
        }

        return AssetDeleteResult.DidNotDelete;
    }

    private static void RemoveScriptFromPrefabs(string assetPath, string scriptName)
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
                if (!component)
                    continue;

                if(component.GetType().Name == scriptName)
                {
                    MonoScript monoScript = MonoScript.FromMonoBehaviour((MonoBehaviour)component);
                    string attachedScriptPath = AssetDatabase.GetAssetPath(monoScript);

                    if (attachedScriptPath != assetPath)
                        continue;

                    Debug.Log($"[ScriptDeletionHandler] '{prefab.name}' 프리팹에서 삭제된 스크립트 발견, 제거 중...");

                    Object.DestroyImmediate(component, true);

                    modified = true;
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
