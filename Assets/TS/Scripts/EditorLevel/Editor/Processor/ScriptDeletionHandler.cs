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
        // ��� ������ ã��
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

                    Debug.Log($"[ScriptDeletionHandler] '{prefab.name}' �����տ��� ������ ��ũ��Ʈ �߰�, ���� ��...");

                    Object.DestroyImmediate(component, true);

                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[ScriptDeletionHandler] '{prefab.name}' �����տ��� ������ ��ũ��Ʈ�� �����ϰ� �����߽��ϴ�.");
            }
        }
    }
}
