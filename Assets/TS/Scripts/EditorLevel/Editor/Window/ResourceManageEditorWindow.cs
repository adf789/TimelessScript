#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

public class ResourceManageEditorWindow : EditorWindow
{
    private ResourcesPath selectedManager;
    private ResourcesTypeRegistry selectedRegistry;

    // UI State
    private Vector2 scrollPosition;
    private Vector2 managerScrollPosition;
    private Vector2 registryScrollPosition;
    private string searchFilter = "";
    private UnityEngine.Object objectToAdd;
    private bool showAdvanced = false;
    private bool showManagerSelection = true;
    private bool showRegistryPanel = true;
    private bool showAddNewType = false;

    // Window state
    private List<ResourcesPath> availableManagers;
    private List<ResourcesTypeRegistry> availableRegistries;
    private string[] managerNames;
    private string[] registryNames;
    private int selectedManagerIndex = 0;
    private int selectedRegistryIndex = 0;

    // Add new type state
    private System.Type selectedType;
    private int selectedTypeIndex = 0;
    private string customDisplayName = "";

    // Common Unity types for dropdown
    private readonly System.Type[] commonUnityTypes = {
        typeof(GameObject),
        typeof(Texture2D),
        typeof(Texture),
        typeof(Sprite),
        typeof(Material),
        typeof(Mesh),
        typeof(AudioClip),
        typeof(AnimationClip),
        typeof(RuntimeAnimatorController),
        typeof(ScriptableObject),
        typeof(TextAsset),
        typeof(Shader),
        typeof(Font),
        typeof(Avatar),
        typeof(BaseTable),
    };

    private string[] typeNames;

    // UI Settings
    private const float WINDOW_MIN_WIDTH = 800f;
    private const float WINDOW_MIN_HEIGHT = 500f;

    private enum ViewMode
    {
        ResourcesPath,
        TypeRegistry,
        Combined
    }

    private ViewMode currentViewMode = ViewMode.Combined;

    [MenuItem("Tools/Resource Manager %&r")]
    public static void OpenWindow()
    {
        var window = GetWindow<ResourceManageEditorWindow>("Resource Manager");
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        window.Show();
    }

    // Context menu for ResourcesPath
    [MenuItem("CONTEXT/ResourcesPath/Open in Window")]
    public static void OpenWindowWithManager(MenuCommand command)
    {
        var manager = (ResourcesPath)command.context;
        var window = GetWindow<ResourceManageEditorWindow>("Resource Manager");
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        window.SetSelectedManager(manager);
        window.Show();
    }

    // Open with ResourcesTypeRegistry
    public static void OpenWindowWithRegistry(ResourcesTypeRegistry registry)
    {
        var window = GetWindow<ResourceManageEditorWindow>("Resource Manager");
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        window.SetSelectedRegistry(registry);
        window.currentViewMode = ViewMode.TypeRegistry;
        window.Show();
    }

    private void OnEnable()
    {
        RefreshAssetLists();

        // Initialize type names for dropdown
        typeNames = commonUnityTypes.Select(t => t.Name).ToArray();

        // Subscribe to selection changes
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        // Auto-select manager if selected in Project window
        if (Selection.activeObject is ResourcesPath manager)
        {
            SetSelectedManager(manager);
        }
        else if (Selection.activeObject is ResourcesTypeRegistry registry)
        {
            SetSelectedRegistry(registry);
        }
    }

    public void SetSelectedManager(ResourcesPath manager)
    {
        selectedManager = manager;
        if (availableManagers != null && availableManagers.Contains(manager))
        {
            selectedManagerIndex = availableManagers.IndexOf(manager);
        }
        showManagerSelection = false;
        Repaint();
    }

    public void SetSelectedRegistry(ResourcesTypeRegistry registry)
    {
        selectedRegistry = registry;
        if (availableRegistries != null && availableRegistries.Contains(registry))
        {
            selectedRegistryIndex = availableRegistries.IndexOf(registry);
        }
        showRegistryPanel = false;
        Repaint();
    }

    private void RefreshAssetLists()
    {
        // Find all ResourcesPath assets
        string[] managerGuids = AssetDatabase.FindAssets("t:ResourcesPath");
        availableManagers = new List<ResourcesPath>();

        foreach (string guid in managerGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var manager = AssetDatabase.LoadAssetAtPath<ResourcesPath>(path);
            if (manager != null)
            {
                availableManagers.Add(manager);
            }
        }

        // Find all ResourcesTypeRegistry assets
        string[] registryGuids = AssetDatabase.FindAssets("t:ResourcesTypeRegistry");
        availableRegistries = new List<ResourcesTypeRegistry>();

        foreach (string guid in registryGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var registry = AssetDatabase.LoadAssetAtPath<ResourcesTypeRegistry>(path);
            if (registry != null)
            {
                availableRegistries.Add(registry);
            }
        }

        managerNames = availableManagers.Select(m => m.name).ToArray();
        registryNames = availableRegistries.Select(r => r.name).ToArray();

        // Auto-select first items if none selected
        if (selectedManager == null && availableManagers.Count > 0)
        {
            selectedManager = availableManagers[0];
            selectedManagerIndex = 0;
        }

        if (selectedRegistry == null && availableRegistries.Count > 0)
        {
            selectedRegistry = availableRegistries[0];
            selectedRegistryIndex = 0;
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        switch (currentViewMode)
        {
            case ViewMode.ResourcesPath:
                DrawResourcesPathView();
                break;
            case ViewMode.TypeRegistry:
                DrawTypeRegistryView();
                break;
            case ViewMode.Combined:
                DrawCombinedView();
                break;
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Title
        GUILayout.Label("Resource Manager", EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        // View mode selection
        EditorGUI.BeginChangeCheck();
        currentViewMode = (ViewMode)EditorGUILayout.EnumPopup(currentViewMode, EditorStyles.toolbarPopup, GUILayout.Width(120));
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }

        // Toggle panels
        if (currentViewMode == ViewMode.Combined)
        {
            string toggleText = showRegistryPanel ? "Hide Registry" : "Show Registry";
            if (GUILayout.Button(toggleText, EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                showRegistryPanel = !showRegistryPanel;
            }
        }

        // Refresh button
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshAssetLists();
        }

        // Create new buttons
        if (GUILayout.Button("Create", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
        {
            ShowCreateMenu();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ShowCreateMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Resources Path"), false, CreateNewResourcesPath);
        menu.AddItem(new GUIContent("Type Registry"), false, CreateNewTypeRegistry);
        menu.ShowAsContext();
    }

    private void DrawResourcesPathView()
    {
        if (selectedManager == null)
        {
            DrawNoManagerSelected();
            return;
        }

        EditorGUILayout.Space(5);

        if (showManagerSelection)
        {
            DrawManagerSelection();
        }

        DrawManagerInterface();
    }

    private void DrawTypeRegistryView()
    {
        if (selectedRegistry == null)
        {
            DrawNoRegistrySelected();
            return;
        }

        EditorGUILayout.Space(5);
        DrawRegistryInterface();
    }

    private void DrawCombinedView()
    {
        EditorGUILayout.BeginHorizontal();

        // Left panel - Type Registry
        if (showRegistryPanel)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            DrawRegistryPanel();
            EditorGUILayout.EndVertical();
        }

        // Right panel - ResourcesPath
        EditorGUILayout.BeginVertical();
        if (selectedManager == null)
        {
            DrawNoManagerSelected();
        }
        else
        {
            if (showManagerSelection)
            {
                DrawManagerSelection();
            }
            DrawManagerInterface();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRegistryPanel()
    {
        EditorGUILayout.LabelField("Type Registry", EditorStyles.boldLabel);

        if (availableRegistries.Count == 0)
        {
            EditorGUILayout.HelpBox("No Type Registries found.", MessageType.Info);
            if (GUILayout.Button("Create New Type Registry"))
            {
                CreateNewTypeRegistry();
            }
            return;
        }

        // Registry selection
        EditorGUI.BeginChangeCheck();
        selectedRegistryIndex = EditorGUILayout.Popup("Current Registry", selectedRegistryIndex, registryNames);
        if (EditorGUI.EndChangeCheck())
        {
            selectedRegistry = availableRegistries[selectedRegistryIndex];
        }

        if (selectedRegistry == null) return;

        EditorGUILayout.Space(5);

        // Registry stats
        var mappings = selectedRegistry.GetAllMappings();
        var activeMappings = selectedRegistry.GetActiveMappings();

        EditorGUILayout.LabelField($"Total Types: {mappings.Count}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Active Types: {activeMappings.Count}", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        // Type list
        registryScrollPosition = EditorGUILayout.BeginScrollView(registryScrollPosition, "box", GUILayout.MaxHeight(400));

        foreach (var mapping in activeMappings)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Type color indicator
            var colorRect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
            EditorGUI.DrawRect(colorRect, mapping.TypeColor);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(mapping.DisplayName, EditorStyles.miniLabel);
            int count = mapping.ResourcesPath?.Count ?? 0;
            EditorGUILayout.LabelField($"{count} resources", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                SetSelectedManager(mapping.ResourcesPath);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(5);

        // Add new type mapping
        showAddNewType = EditorGUILayout.Foldout(showAddNewType, "Add New Type Mapping");
        if (showAddNewType)
        {
            EditorGUILayout.BeginVertical("box");

            // Type selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(80));
            selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeNames);
            selectedType = commonUnityTypes[selectedTypeIndex];
            EditorGUILayout.EndHorizontal();

            // Custom display name
            customDisplayName = EditorGUILayout.TextField("Display Name (Optional)", customDisplayName);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(selectedType == null);
            if (GUILayout.Button("Create & Add Mapping"))
            {
                if (selectedType != null && selectedRegistry != null)
                {
                    // Check if mapping already exists
                    if (selectedRegistry.GetAllMappings().Any(m => m.GetSystemType() == selectedType))
                    {
                        EditorUtility.DisplayDialog("Type Already Exists",
                            $"A mapping for {selectedType.Name} already exists.", "OK");
                    }
                    else
                    {
                        var newPath = selectedRegistry.CreateResourcesPathForType(selectedType, true);

                        // Apply custom display name if provided
                        if (!string.IsNullOrEmpty(customDisplayName))
                        {
                            var mapping = selectedRegistry.GetAllMappings().FirstOrDefault(m => m.GetSystemType() == selectedType);
                            if (mapping != null)
                            {
                                mapping.SetDisplayName(customDisplayName);
                                EditorUtility.SetDirty(selectedRegistry);
                            }
                        }

                        customDisplayName = "";
                        EditorGUIUtility.PingObject(newPath);

                        // Auto-select the new ResourcesPath
                        SetSelectedManager(newPath);

                        // Close the add section and switch to ResourcesPath view
                        showAddNewType = false;
                        currentViewMode = ViewMode.Combined;
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);

        // Registry actions
        if (GUILayout.Button("Open Registry Inspector"))
        {
            Selection.activeObject = selectedRegistry;
            EditorGUIUtility.PingObject(selectedRegistry);
        }
    }

    private void DrawRegistryInterface()
    {
        if (selectedRegistry == null) return;

        EditorGUILayout.LabelField("Type Registry Management", EditorStyles.boldLabel);

        var mappings = selectedRegistry.GetAllMappings();
        EditorGUILayout.LabelField($"Total Type Mappings: {mappings.Count}");

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Register Common Types"))
        {
            selectedRegistry.RegisterCommonUnityTypes();
        }

        if (GUILayout.Button("Validate All"))
        {
            selectedRegistry.ValidateAllMappings();
        }

        if (GUILayout.Button("Show Statistics"))
        {
            EditorUtility.DisplayDialog("Registry Statistics", selectedRegistry.GetStatistics(), "OK");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Add new type mapping section
        showAddNewType = EditorGUILayout.Foldout(showAddNewType, "Add New Type Mapping");
        if (showAddNewType)
        {
            EditorGUILayout.BeginVertical("box");

            // Type selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(80));
            selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeNames);
            selectedType = commonUnityTypes[selectedTypeIndex];
            EditorGUILayout.EndHorizontal();

            // Custom display name
            customDisplayName = EditorGUILayout.TextField("Display Name (Optional)", customDisplayName);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(selectedType == null);
            if (GUILayout.Button("Create & Add Mapping"))
            {
                if (selectedType != null && selectedRegistry != null)
                {
                    // Check if mapping already exists
                    if (selectedRegistry.GetAllMappings().Any(m => m.GetSystemType() == selectedType))
                    {
                        EditorUtility.DisplayDialog("Type Already Exists",
                            $"A mapping for {selectedType.Name} already exists.", "OK");
                    }
                    else
                    {
                        var newPath = selectedRegistry.CreateResourcesPathForType(selectedType, true);

                        // Apply custom display name if provided
                        if (!string.IsNullOrEmpty(customDisplayName))
                        {
                            var mapping = selectedRegistry.GetAllMappings().FirstOrDefault(m => m.GetSystemType() == selectedType);
                            if (mapping != null)
                            {
                                mapping.SetDisplayName(customDisplayName);
                                EditorUtility.SetDirty(selectedRegistry);
                            }
                        }

                        customDisplayName = "";
                        EditorGUIUtility.PingObject(newPath);

                        // Close the add section since we added the mapping
                        showAddNewType = false;

                        // Update the mappings list
                        mappings = selectedRegistry.GetAllMappings();
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // Type mappings list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var mapping in mappings)
        {
            DrawTypeMappingEntry(mapping);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTypeMappingEntry(ResourcesTypeRegistry.TypeMapping mapping)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        // Color indicator
        var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
        EditorGUI.DrawRect(colorRect, mapping.TypeColor);

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(mapping.DisplayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Resources: {mapping.ResourcesPath?.Count ?? 0}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Open", GUILayout.Width(50)))
        {
            if (mapping.ResourcesPath != null)
            {
                SetSelectedManager(mapping.ResourcesPath);
                currentViewMode = ViewMode.ResourcesPath;
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }


    private void DrawNoManagerSelected()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("No Resources Path Selected", EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Resources Path", GUILayout.Width(180), GUILayout.Height(30)))
        {
            CreateNewResourcesPath();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (availableManagers?.Count > 0)
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Or select an existing manager:", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            DrawManagerList();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawNoRegistrySelected()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("No Type Registry Selected", EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Type Registry", GUILayout.Width(180), GUILayout.Height(30)))
        {
            CreateNewTypeRegistry();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    private void DrawManagerSelection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Resources Path Selection", EditorStyles.boldLabel);

        if (availableManagers.Count == 0)
        {
            EditorGUILayout.HelpBox("No Resources Path found in project.", MessageType.Info);
            if (GUILayout.Button("Create New Resources Path"))
            {
                CreateNewResourcesPath();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            selectedManagerIndex = EditorGUILayout.Popup("Current Manager", selectedManagerIndex, managerNames);
            if (EditorGUI.EndChangeCheck())
            {
                selectedManager = availableManagers[selectedManagerIndex];
            }

            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                EditorGUIUtility.PingObject(selectedManager);
            }

            EditorGUILayout.EndHorizontal();

            if (selectedManager != null)
            {
                EditorGUILayout.ObjectField("Selected Manager", selectedManager, typeof(ResourcesPath), false);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawManagerList()
    {
        managerScrollPosition = EditorGUILayout.BeginScrollView(managerScrollPosition, GUILayout.Height(150));

        foreach (var manager in availableManagers)
        {
            EditorGUILayout.BeginHorizontal("box");

            EditorGUILayout.LabelField(manager.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"({manager.Count} resources)", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                SetSelectedManager(manager);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawManagerInterface()
    {
        if (selectedManager == null) return;

        // Stats and actions bar
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"Total Resources: {selectedManager.Count}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Validate References", GUILayout.Width(120)))
        {
            selectedManager.ValidateReferences();
        }

        if (GUILayout.Button("Focus Inspector", GUILayout.Width(100)))
        {
            Selection.activeObject = selectedManager;
            EditorGUIUtility.PingObject(selectedManager);
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
            if (selectedManager.AddResourceFromObject(objectToAdd))
            {
                objectToAdd = null;
                EditorUtility.SetDirty(selectedManager);
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
                selectedManager.Clear();
                EditorUtility.SetDirty(selectedManager);
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
            if (GUILayout.Button("Duplicate Manager"))
            {
                DuplicateManager();
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
        var entries = selectedManager.GetAllEntries();
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

    private void DrawResourceEntry(ResourcesPath.ResourceEntry entry, bool isEven)
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
                $"Remove {entry.DisplayName} from the resource manager?", "Remove", "Cancel"))
            {
                selectedManager.RemoveResource(entry.Guid);
                EditorUtility.SetDirty(selectedManager);
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
                selectedManager.RemoveResource(entry.Guid);
                selectedManager.AddResourceFromObject(newAsset);
                EditorUtility.SetDirty(selectedManager);
            }
        }

        EditorGUILayout.EndVertical();
    }

    #region Helper Methods

    private void CreateNewResourcesPath()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Resources Path",
            "New Resources Path",
            "asset",
            "Choose location for new Resources Path");

        if (!string.IsNullOrEmpty(path))
        {
            var newManager = CreateInstance<ResourcesPath>();
            AssetDatabase.CreateAsset(newManager, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshAssetLists();
            SetSelectedManager(newManager);

            EditorGUIUtility.PingObject(newManager);
        }
    }

    private void CreateNewTypeRegistry()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Type Registry",
            "New Type Registry",
            "asset",
            "Choose location for new Type Registry");

        if (!string.IsNullOrEmpty(path))
        {
            var newRegistry = CreateInstance<ResourcesTypeRegistry>();
            AssetDatabase.CreateAsset(newRegistry, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshAssetLists();
            SetSelectedRegistry(newRegistry);

            EditorGUIUtility.PingObject(newRegistry);
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
                selectedManager.PopulateFromFolder(path);
                EditorUtility.SetDirty(selectedManager);
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
            if (selectedManager.AddResourceFromObject(obj))
            {
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.SetDirty(selectedManager);
            ShowNotification(new GUIContent($"Added {addedCount} assets"));
        }
        else
        {
            ShowNotification(new GUIContent("No new assets added"));
        }
    }

    private void ExportToJSON()
    {
        string path = EditorUtility.SaveFilePanel("Export Resource Manager", "", selectedManager.name + ".json", "json");
        if (!string.IsNullOrEmpty(path))
        {
            var data = new ResourceManagerData();
            data.entries = selectedManager.GetAllEntries().ToArray();

            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);

            ShowNotification(new GUIContent("Exported successfully"));
        }
    }

    private void ImportFromJSON()
    {
        string path = EditorUtility.OpenFilePanel("Import Resource Manager", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                var data = JsonUtility.FromJson<ResourceManagerData>(json);

                int importedCount = 0;
                foreach (var entry in data.entries)
                {
                    if (selectedManager.AddResource(entry.Guid, entry.AssetPath, entry.DisplayName))
                    {
                        importedCount++;
                    }
                }

                EditorUtility.SetDirty(selectedManager);
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
        var entries = selectedManager.GetAllEntries();
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

    private void DuplicateManager()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Duplicate Resources Path",
            selectedManager.name + "_Copy",
            "asset",
            "Choose location for duplicated manager");

        if (!string.IsNullOrEmpty(path))
        {
            var duplicate = Instantiate(selectedManager);
            AssetDatabase.CreateAsset(duplicate, path);
            AssetDatabase.SaveAssets();

            RefreshAssetLists();
            ShowNotification(new GUIContent("Manager duplicated"));
        }
    }

    private void MergeWithAnother()
    {
        // Simple implementation - could be expanded
        var otherManager = EditorGUILayout.ObjectField("Merge with", null, typeof(ResourcesPath), false) as ResourcesPath;
        if (otherManager != null && otherManager != selectedManager)
        {
            var otherEntries = otherManager.GetAllEntries();
            int mergedCount = 0;

            foreach (var entry in otherEntries)
            {
                if (selectedManager.AddResource(entry.Guid, entry.AssetPath, entry.DisplayName))
                {
                    mergedCount++;
                }
            }

            EditorUtility.SetDirty(selectedManager);
            ShowNotification(new GUIContent($"Merged {mergedCount} resources"));
        }
    }

    [System.Serializable]
    private class ResourceManagerData
    {
        public ResourcesPath.ResourceEntry[] entries;
    }

    #endregion
}
#endif