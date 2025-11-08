#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PrefabIDFinderWindow : EditorWindow
{
    [MenuItem("TS/Finder/Prefab ID Finder", false, 0)]
    public static void OpenWindow()
    {
        PrefabIDFinderWindow window = (PrefabIDFinderWindow) GetWindow(typeof(PrefabIDFinderWindow));
        window.titleContent.text = "Prefab ID Finder Window";
    }

    private GameObject selectedPrefab;
    private string inputFileID = "";
    private string inputGUID = "";
    private string referenceInfoText = "";
    private Vector2 referenceScrollPosition;

    private List<string> prefabReferences = null;

    private void OnGUI()
    {
        GUILayout.Label("Find FileID in Prefab", EditorStyles.boldLabel);

        // Prefab ObjectField
        EditorGUI.BeginChangeCheck();
        {
            selectedPrefab = (GameObject) EditorGUILayout.ObjectField("Prefab:", selectedPrefab, typeof(GameObject), false);

            if (selectedPrefab == null)
                return;
        }
        bool check = EditorGUI.EndChangeCheck();

        if (check)
            prefabReferences = null;

        if (GUILayout.Button("Print Dependencies in Prefab"))
        {
            FindDependenciesInPrefab(selectedPrefab);
        }

        if (GUILayout.Button("Find References By File ID"))
        {
            string targetPath = AssetDatabase.GetAssetPath(selectedPrefab);
            ulong fileId = Unsupported.GetLocalIdentifierInFileForPersistentObject(selectedPrefab);
            inputFileID = fileId.ToString();
            inputGUID = null;

            prefabReferences = FindPrefabsReferencingByFileId(targetPath, fileId);
            referenceInfoText = "By File ID";
        }

        if (GUILayout.Button("Find References By GUID"))
        {
            string targetPath = AssetDatabase.GetAssetPath(selectedPrefab);
            inputGUID = AssetDatabase.AssetPathToGUID(targetPath);
            inputFileID = null;

            prefabReferences = FindPrefabsReferencingByGuid(targetPath, inputGUID);
            referenceInfoText = "By GUID";
        }

        if (prefabReferences != null)
        {
            GUILayout.Space(10);

            if (prefabReferences.Count > 0)
            {
                referenceScrollPosition = GUILayout.BeginScrollView(referenceScrollPosition);
                {
                    GUILayout.Label($"Prefab References {referenceInfoText}", EditorStyles.boldLabel);

                    foreach (var reference in prefabReferences)
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(reference);

                        if (GUILayout.Button(fileName))
                        {
                            Object referenceObject = AssetDatabase.LoadAssetAtPath(reference, typeof(Object));

                            if (referenceObject != null)
                                EditorGUIUtility.PingObject(referenceObject);
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label($"No referencing Prefabs found {referenceInfoText}.", EditorStyles.boldLabel);
            }

            GUILayout.Space(10);
        }

        GUILayout.Space(10);
        GUILayout.Label("Search by FileID", EditorStyles.boldLabel);

        inputFileID = EditorGUILayout.TextField("FileID:", inputFileID);
        if (GUILayout.Button("Find by FileID") && long.TryParse(inputFileID, out long fileID))
        {
            FindObjectByFileID(fileID);
        }

        GUILayout.Space(10);
        GUILayout.Label("Search by GUID", EditorStyles.boldLabel);

        inputGUID = EditorGUILayout.TextField("GUID:", inputGUID);
        if (GUILayout.Button("Find by GUID") && !string.IsNullOrEmpty(inputGUID))
        {
            FindObjectByGUID(inputGUID);
        }

        GUILayout.Space(10);
    }

    private List<string> FindPrefabsReferencingByFileId(string targetPath, ulong fileId)
    {
        string fileIDPattern = $"fileID: {fileId}";
        List<string> referencePaths = new List<string>();
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == targetPath) continue; // 자기 자신은 제외

            string content = System.IO.File.ReadAllText(path);
            if (content.Contains(fileIDPattern)) // FileID가 포함되어 있는지 확인
            {
                referencePaths.Add(path);
            }
        }

        foreach (string guid in sceneGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            string content = System.IO.File.ReadAllText(path);
            if (content.Contains(fileIDPattern)) // FileID가 포함되어 있는지 확인
            {
                referencePaths.Add(path);
            }
        }

        return referencePaths;
    }

    private List<string> FindPrefabsReferencingByGuid(string targetPath, string findGuid)
    {
        string guidPattern = $"guid: {findGuid}";
        List<string> referencePaths = new List<string>();
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == targetPath) continue; // 자기 자신은 제외

            string content = System.IO.File.ReadAllText(path);
            if (content.Contains(guidPattern)) // GUID가 포함되어 있는지 확인
            {
                referencePaths.Add(path);
            }
        }

        foreach (string guid in sceneGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            string content = System.IO.File.ReadAllText(path);
            if (content.Contains(guidPattern)) // GUID가 포함되어 있는지 확인
            {
                referencePaths.Add(path);
            }
        }

        return referencePaths;
    }

    private void FindDependenciesInPrefab(GameObject prefab)
    {
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Invalid prefab selection.");
            return;
        }

        var dependencies = EditorUtility.CollectDependencies(new Object[] { prefab });
        Debug.Log($"Prefab Path: {prefabPath}");

        foreach (var dependency in dependencies)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(dependency, out string guid, out long localFileID))
            {
                Debug.Log($"Dependency: {dependency.name}, GUID: {guid}, FileID: {localFileID}");
            }
        }
    }

    private void FindObjectByFileID(long fileID)
    {
        if (selectedPrefab == null)
            return;

        string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Invalid prefab selection.");
            return;
        }

        var dependencies = EditorUtility.CollectDependencies(new Object[] { selectedPrefab });

        foreach (var dependency in dependencies)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(dependency, out string guid, out long localFileID))
            {
                if (fileID == localFileID)
                {
                    EditorUtility.DisplayDialog("Alarm", $"Object name: {dependency.name}\nGUID: {guid}\nFileID: {localFileID}", "Confirm");
                    break;
                }
            }
        }
    }

    private void FindObjectByGUID(string findGuid)
    {
        if (selectedPrefab == null)
            return;

        string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Invalid prefab selection.");
            return;
        }

        var dependencies = EditorUtility.CollectDependencies(new Object[] { selectedPrefab });

        foreach (var dependency in dependencies)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(dependency, out string guid, out long localFileID))
            {
                if (findGuid == guid)
                {
                    EditorUtility.DisplayDialog("Alarm", $"Object name: {dependency.name}\nGUID: {guid}\nFileID: {localFileID}", "Confirm");
                    break;
                }
            }
        }
    }
}
#endif