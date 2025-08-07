#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class ResourceManageEditorWindow : EditorWindow
{
    private ResourcePath selectedPath;
    private Vector2 scrollPosition;
    private Vector2 pathScrollPosition;
    private string searchFilter = "";
    private UnityEngine.Object objectToAdd;
    private bool showAdvanced = false;
    private bool showPathSelection = true;

    // Window state
    private List<ResourcePath> availablePaths;
    private string[] pathNames;
    private int selectedPathIndex = 0;

    // UI Settings
    private const float WINDOW_MIN_WIDTH = 600f;
    private const float WINDOW_MIN_HEIGHT = 400f;

    [MenuItem("Tools/Resource Path %&r")]
    public static void OpenWindow()
    {
        var window = GetWindow<ResourceManageEditorWindow>("Resource Path");
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        window.Show();
    }

    // Context menu for ScriptableObject
    [MenuItem("CONTEXT/ResourcePath/Open in Window")]
    public static void OpenWindowWithPath(MenuCommand command)
    {
        var path = (ResourcePath)command.context;
        var window = GetWindow<ResourceManageEditorWindow>("GUID Resource Path");
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        window.SetSelectedPath(path);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshPathList();

        // Subscribe to selection changes
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        // Auto-select path if selected in Project window
        if (Selection.activeObject is ResourcePath path)
        {
            SetSelectedPath(path);
        }
    }

    public void SetSelectedPath(ResourcePath path)
    {
        selectedPath = path;
        if (availablePaths != null && availablePaths.Contains(path))
        {
            selectedPathIndex = availablePaths.IndexOf(path);
        }
        showPathSelection = false;
        Repaint();
    }

    private void RefreshPathList()
    {
        // Find all GuidResourcePath assets
        string[] guids = AssetDatabase.FindAssets("t:ResourcePath");
        availablePaths = new List<ResourcePath>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var resourcePath = AssetDatabase.LoadAssetAtPath<ResourcePath>(path);
            if (resourcePath != null)
            {
                availablePaths.Add(resourcePath);
            }
        }

        pathNames = availablePaths.Select(m => m.name).ToArray();

        // Auto-select first path if none selected
        if (selectedPath == null && availablePaths.Count > 0)
        {
            selectedPath = availablePaths[0];
            selectedPathIndex = 0;
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (selectedPath == null)
        {
            DrawNoPathSelected();
            return;
        }

        EditorGUILayout.Space(5);

        if (showPathSelection)
        {
            DrawPathSelection();
        }

        DrawPathInterface();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Title
        GUILayout.Label("GUID Resource Path", EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        // Path selection toggle
        string toggleText = showPathSelection ? "Hide Selection" : "Show Selection";
        if (GUILayout.Button(toggleText, EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            showPathSelection = !showPathSelection;
        }

        // Refresh button
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshPathList();
        }

        // Create new path
        if (GUILayout.Button("Create New", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            CreateNewPath();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawNoPathSelected()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("No GUID Resource Path Selected", EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Path", GUILayout.Width(150), GUILayout.Height(30)))
        {
            CreateNewPath();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (availablePaths?.Count > 0)
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Or select an existing path:", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawPathList();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawPathSelection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Path Selection", EditorStyles.boldLabel);

        if (availablePaths.Count == 0)
        {
            EditorGUILayout.HelpBox("No GUID Resource Paths found in project.", MessageType.Info);
            if (GUILayout.Button("Create New Path"))
            {
                CreateNewPath();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            selectedPathIndex = EditorGUILayout.Popup("Current Path", selectedPathIndex, pathNames);
            if (EditorGUI.EndChangeCheck())
            {
                selectedPath = availablePaths[selectedPathIndex];
            }

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(selectedPath);
            }

            EditorGUILayout.EndHorizontal();

            if (selectedPath != null)
            {
                EditorGUILayout.ObjectField("Selected Path", selectedPath, typeof(ResourcePath), false);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawPathList()
    {
        pathScrollPosition = EditorGUILayout.BeginScrollView(pathScrollPosition, GUILayout.Height(150));

        foreach (var path in availablePaths)
        {
            EditorGUILayout.BeginHorizontal("box");

            EditorGUILayout.LabelField(path.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"({path.Count} resources)", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                SetSelectedPath(path);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPathInterface()
    {
        if (selectedPath == null) return;

        // Stats and actions bar
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"Total Resources: {selectedPath.Count}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Validate References", GUILayout.Width(120)))
        {
            selectedPath.ValidateReferences();
        }

        if (GUILayout.Button("Focus Inspector", GUILayout.Width(100)))
        {
            Selection.activeObject = selectedPath;
            EditorGUIUtility.PingObject(selectedPath);
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
            if (selectedPath.AddResourceFromObject(objectToAdd))
            {
                objectToAdd = null;
                EditorUtility.SetDirty(selectedPath);
            }
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        // Quick action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add from Selected Folder"))
        {
            AddFromSelectedFolder();
        }

        if (GUILayout.Button("Add Selected Assets"))
        {
            AddSelectedAssets();
        }

        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Clear All Resources",
                "Are you sure you want to clear all resource entries?", "Yes", "Cancel"))
            {
                selectedPath.Clear();
                EditorUtility.SetDirty(selectedPath);
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

            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate Path"))
            {
                DuplicatePath();
            }

            if (GUILayout.Button("Merge with Another"))
            {
                MergeWithAnother();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);

        // Resource list
        DrawResourceList();
    }

    private void DrawResourceList()
    {
        var entries = selectedPath.GetAllEntries();
        var filteredEntries = string.IsNullOrEmpty(searchFilter)
            ? entries
            : entries.Where(e => e.DisplayName.ToLower().Contains(searchFilter.ToLower()) ||
                                e.AssetPath.ToLower().Contains(searchFilter.ToLower())).ToList();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Resources ({filteredEntries.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        // Sort options
        if (GUILayout.Button("Sort by Name", EditorStyles.miniButton, GUILayout.Width(80)))
        {
            filteredEntries = filteredEntries.OrderBy(e => e.DisplayName).ToList();
        }
        if (GUILayout.Button("Sort by Path", EditorStyles.miniButton, GUILayout.Width(80)))
        {
            filteredEntries = filteredEntries.OrderBy(e => e.AssetPath).ToList();
        }
        EditorGUILayout.EndHorizontal();

        if (filteredEntries.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < filteredEntries.Count; i++)
            {
                var entry = filteredEntries[i];
                DrawResourceEntry(entry, i % 2 == 0);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("No resources found.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
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

        if (GUILayout.Button("Select", GUILayout.Height(16)))
        {
            if (asset != null)
            {
                Selection.activeObject = asset;
            }
        }

        if (GUILayout.Button("Remove", GUILayout.Height(16)))
        {
            if (EditorUtility.DisplayDialog("Remove Resource",
                $"Remove {entry.DisplayName} from the resource path?", "Remove", "Cancel"))
            {
                selectedPath.RemoveResource(entry.Guid);
                EditorUtility.SetDirty(selectedPath);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // Handle asset changes
        if (newAsset != asset && newAsset != null)
        {
            string newPath = AssetDatabase.GetAssetPath(newAsset);
            string newGuid = AssetDatabase.AssetPathToGUID(newPath);

            if (newGuid != entry.Guid)
            {
                selectedPath.RemoveResource(entry.Guid);
                selectedPath.AddResourceFromObject(newAsset);
                EditorUtility.SetDirty(selectedPath);
            }
        }

        EditorGUILayout.EndVertical();
    }

    #region Helper Methods

    private void CreateNewPath()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create GUID Resource Path",
            "New Resource Path",
            "asset",
            "Choose location for new GUID Resource Path");

        if (!string.IsNullOrEmpty(path))
        {
            var newPath = CreateInstance<ResourcePath>();
            AssetDatabase.CreateAsset(newPath, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshPathList();
            SetSelectedPath(newPath);

            EditorGUIUtility.PingObject(newPath);
        }
    }

    private void AddFromSelectedFolder()
    {
        string[] selectedGUIDs = Selection.assetGUIDs;
        if (selectedGUIDs.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(selectedGUIDs[0]);
            if (AssetDatabase.IsValidFolder(path))
            {
                selectedPath.PopulateFromFolder(path);
                EditorUtility.SetDirty(selectedPath);
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

    private void AddSelectedAssets()
    {
        var selectedObjects = Selection.objects;
        int addedCount = 0;

        foreach (var obj in selectedObjects)
        {
            if (selectedPath.AddResourceFromObject(obj))
            {
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.SetDirty(selectedPath);
            ShowNotification(new GUIContent($"Added {addedCount} assets"));
        }
        else
        {
            ShowNotification(new GUIContent("No new assets added"));
        }
    }

    private void ExportToJSON()
    {
        string path = EditorUtility.SaveFilePanel("Export Resource Path", "", selectedPath.name + ".json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            var data = new ResourcePathData();
            data.entries = selectedPath.GetAllEntries().ToArray();

            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);

            ShowNotification(new GUIContent("Exported successfully"));
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
                    if (selectedPath.AddResource(entry.Guid, entry.AssetPath, entry.DisplayName))
                    {
                        importedCount++;
                    }
                }

                EditorUtility.SetDirty(selectedPath);
                ShowNotification(new GUIContent($"Imported {importedCount} resources"));
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Failed", $"Failed to import JSON file:\n{e.Message}", "OK");
            }
        }
    }

    private void ShowMissingReferences()
    {
        var entries = selectedPath.GetAllEntries();
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
            ShowNotification(new GUIContent("All references are valid!"));
        }
    }

    private void DuplicatePath()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Duplicate Resource Path",
            selectedPath.name + "_Copy",
            "asset",
            "Choose location for duplicated path");

        if (!string.IsNullOrEmpty(path))
        {
            var duplicate = Instantiate(selectedPath);
            AssetDatabase.CreateAsset(duplicate, path);
            AssetDatabase.SaveAssets();

            RefreshPathList();
            ShowNotification(new GUIContent("Path duplicated"));
        }
    }

    private void MergeWithAnother()
    {
        // Simple implementation - could be expanded
        var otherPath = EditorGUILayout.ObjectField("Merge with", null, typeof(ResourcePath), false) as ResourcePath;
        if (otherPath != null && otherPath != selectedPath)
        {
            var otherEntries = otherPath.GetAllEntries();
            int mergedCount = 0;

            foreach (var entry in otherEntries)
            {
                if (selectedPath.AddResource(entry.Guid, entry.AssetPath, entry.DisplayName))
                {
                    mergedCount++;
                }
            }

            EditorUtility.SetDirty(selectedPath);
            ShowNotification(new GUIContent($"Merged {mergedCount} resources"));
        }
    }

    [System.Serializable]
    private class ResourcePathData
    {
        public ResourcePath.ResourceEntry[] entries;
    }

    #endregion
}
#endif