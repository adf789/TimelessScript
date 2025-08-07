#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(ResourcePath))]
public class ResourcePathInspector : Editor
{
    private ResourcePath resourcePath;
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private UnityEngine.Object objectToAdd;
    private bool showAdvanced = false;

    private void OnEnable()
    {
        resourcePath = (ResourcePath)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space(10);

        // Header
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Resource Path", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open in Window", GUILayout.Width(100)))
        {
            ResourceManageEditorWindow.OpenWindowWithPath(new MenuCommand(resourcePath));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Stats
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"Total Resources: {resourcePath.Count}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Validate References", GUILayout.Width(120)))
        {
            resourcePath.ValidateReferences();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Add new resource section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Add New Resource", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        objectToAdd = EditorGUILayout.ObjectField("Asset", objectToAdd, typeof(UnityEngine.Object), false);

        EditorGUI.BeginDisabledGroup(objectToAdd == null);
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            if (resourcePath.AddResourceFromObject(objectToAdd))
            {
                objectToAdd = null;
                EditorUtility.SetDirty(resourcePath);
            }
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        // Folder population
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add from Selected Folder"))
        {
            string[] selectedGUIDs = Selection.assetGUIDs;
            if (selectedGUIDs.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(selectedGUIDs[0]);
                if (AssetDatabase.IsValidFolder(path))
                {
                    resourcePath.PopulateFromFolder(path);
                    EditorUtility.SetDirty(resourcePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Selection", "Please select a folder in the Project window.", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a folder in the Project window.", "OK");
            }
        }

        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Clear All Resources",
                "Are you sure you want to clear all resource entries?", "Yes", "Cancel"))
            {
                resourcePath.Clear();
                EditorUtility.SetDirty(resourcePath);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Search and filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        // Advanced options
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Options");
        if (showAdvanced)
        {
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Export to JSON"))
            {
                ExportToJSON();
            }

            if (GUILayout.Button("Import from JSON"))
            {
                ImportFromJSON();
            }

            if (GUILayout.Button("Show Missing References"))
            {
                ShowMissingReferences();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // Resource list
        var entries = resourcePath.GetAllEntries();
        var filteredEntries = string.IsNullOrEmpty(searchFilter)
            ? entries
            : entries.Where(e => e.DisplayName.ToLower().Contains(searchFilter.ToLower()) ||
                                e.AssetPath.ToLower().Contains(searchFilter.ToLower())).ToList();

        EditorGUILayout.LabelField($"Resources ({filteredEntries.Count})", EditorStyles.boldLabel);

        if (filteredEntries.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box");

            for (int i = 0; i < filteredEntries.Count; i++)
            {
                var entry = filteredEntries[i];
                DrawResourceEntry(entry, i % 2 == 0);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("No resources found.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);
    }

    private void DrawResourceEntry(ResourcePath.ResourceEntry entry, bool isEven)
    {
        var backgroundColor = isEven ? new Color(0.8f, 0.8f, 0.8f, 0.1f) : Color.clear;
        var originalColor = GUI.backgroundColor;
        GUI.backgroundColor = backgroundColor;

        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = originalColor;

        EditorGUILayout.BeginHorizontal();

        // Asset reference
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.AssetPath);
        var newAsset = EditorGUILayout.ObjectField(asset, typeof(UnityEngine.Object), false, GUILayout.Width(200));

        // Asset info
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(entry.DisplayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Path: {entry.AssetPath}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"GUID: {entry.Guid}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Actions
        EditorGUILayout.BeginVertical(GUILayout.Width(80));

        if (GUILayout.Button("Ping", GUILayout.Height(16)))
        {
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
        }

        if (GUILayout.Button("Remove", GUILayout.Height(16)))
        {
            if (EditorUtility.DisplayDialog("Remove Resource",
                $"Remove {entry.DisplayName} from the resource path?", "Remove", "Cancel"))
            {
                resourcePath.RemoveResource(entry.Guid);
                EditorUtility.SetDirty(resourcePath);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // Check if asset was changed
        if (newAsset != asset && newAsset != null)
        {
            string newPath = AssetDatabase.GetAssetPath(newAsset);
            string newGuid = AssetDatabase.AssetPathToGUID(newPath);

            if (newGuid != entry.Guid)
            {
                resourcePath.RemoveResource(entry.Guid);
                resourcePath.AddResourceFromObject(newAsset);
                EditorUtility.SetDirty(resourcePath);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void ExportToJSON()
    {
        string path = EditorUtility.SaveFilePanel("Export Resource Path", "", "resource_path.json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            var data = new ResourcePathData();
            data.entries = resourcePath.GetAllEntries().ToArray();

            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);

            EditorUtility.DisplayDialog("Export Complete", $"Resource path exported to:\n{path}", "OK");
        }
    }

    private void ImportFromJSON()
    {
        string path = EditorUtility.OpenFilePanel("Import Resource Path", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                var data = JsonUtility.FromJson<ResourcePathData>(json);

                int importedCount = 0;
                foreach (var entry in data.entries)
                {
                    if (resourcePath.AddResource(entry.Guid, entry.AssetPath, entry.DisplayName))
                    {
                        importedCount++;
                    }
                }

                EditorUtility.SetDirty(resourcePath);
                EditorUtility.DisplayDialog("Import Complete", $"Imported {importedCount} resources.", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Failed to import JSON file:\n{e.Message}", "OK");
            }
        }
    }

    private void ShowMissingReferences()
    {
        var entries = resourcePath.GetAllEntries();
        var missingEntries = entries.Where(e =>
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(e.AssetPath) == null).ToList();

        if (missingEntries.Count > 0)
        {
            string message = $"Found {missingEntries.Count} missing references:\n\n";
            message += string.Join("\n", missingEntries.Select(e => $"• {e.DisplayName} ({e.AssetPath})"));

            EditorUtility.DisplayDialog("Missing References", message, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No Missing References", "All resource references are valid!", "OK");
        }
    }

    [System.Serializable]
    private class ResourcePathData
    {
        public ResourcePath.ResourceEntry[] entries;
    }
}
#endif