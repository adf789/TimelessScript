#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

[CustomEditor(typeof(ResourcesTypeRegistry))]
public class ResourcesTypeRegistryEditor : Editor
{
    private ResourcesTypeRegistry registry;
    private Vector2 scrollPosition;
    private bool showAddNewType = false;
    private Type selectedType;
    private ResourcesPath selectedResourcesPath;
    private string customDisplayName = "";

    // Common Unity types for dropdown
    private readonly Type[] commonUnityTypes = {
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
    private int selectedTypeIndex = 0;

    private void OnEnable()
    {
        registry = (ResourcesTypeRegistry)target;

        // Prepare type names for dropdown
        typeNames = commonUnityTypes.Select(t => t.Name).ToArray();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space(10);

        // Header
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Resource Type Registry", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open in Window", GUILayout.Width(100)))
        {
            ResourceManageEditorWindow.OpenWindowWithRegistry(registry);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Default ResourcesPath
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Default Configuration", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        var newDefaultPath = EditorGUILayout.ObjectField("Default Resources Path",
            registry.GetAllMappings().FirstOrDefault()?.ResourcesPath,
            typeof(ResourcesPath), false) as ResourcesPath;

        if (EditorGUI.EndChangeCheck() && newDefaultPath != null)
        {
            registry.SetDefaultResourcesPath(newDefaultPath);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Quick actions
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Register Common Types"))
        {
            registry.RegisterCommonUnityTypes();
        }

        if (GUILayout.Button("Validate All"))
        {
            registry.ValidateAllMappings();
        }

        if (GUILayout.Button("Show Statistics"))
        {
            EditorUtility.DisplayDialog("Registry Statistics", registry.GetStatistics(), "OK");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

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

            // ResourcesPath selection
            selectedResourcesPath = EditorGUILayout.ObjectField("Resources Path",
                selectedResourcesPath, typeof(ResourcesPath), false) as ResourcesPath;

            // Custom display name
            customDisplayName = EditorGUILayout.TextField("Display Name (Optional)", customDisplayName);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(selectedType == null || selectedResourcesPath == null);
            if (GUILayout.Button("Add Mapping"))
            {
                if (registry.AddTypeMapping(selectedType, selectedResourcesPath,
                    string.IsNullOrEmpty(customDisplayName) ? null : customDisplayName))
                {
                    customDisplayName = "";
                    EditorUtility.SetDirty(registry);
                    showAddNewType = false; // Close section after successful addition
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(selectedType == null);
            if (GUILayout.Button("Create & Add Mapping"))
            {
                if (selectedType != null)
                {
                    // Check if mapping already exists
                    if (registry.GetAllMappings().Any(m => m.GetSystemType() == selectedType))
                    {
                        EditorUtility.DisplayDialog("Type Already Exists",
                            $"A mapping for {selectedType.Name} already exists.", "OK");
                    }
                    else
                    {
                        var newPath = registry.CreateResourcesPathForType(selectedType, true);
                        selectedResourcesPath = newPath;

                        // Apply custom display name if provided
                        if (!string.IsNullOrEmpty(customDisplayName))
                        {
                            var mapping = registry.GetAllMappings().FirstOrDefault(m => m.GetSystemType() == selectedType);
                            if (mapping != null)
                            {
                                mapping.SetDisplayName(customDisplayName);
                                EditorUtility.SetDirty(registry);
                            }
                        }

                        customDisplayName = "";
                        EditorGUIUtility.PingObject(newPath);

                        // Close the add section since we added the mapping
                        showAddNewType = false;
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // Type mappings list
        var mappings = registry.GetAllMappings();
        EditorGUILayout.LabelField($"Type Mappings ({mappings.Count})", EditorStyles.boldLabel);

        if (mappings.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box");

            for (int i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                DrawMappingEntry(mapping, i % 2 == 0);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("No type mappings found.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Register Common Unity Types", GUILayout.Width(200)))
            {
                registry.RegisterCommonUnityTypes();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);
    }

    private void DrawMappingEntry(ResourcesTypeRegistry.TypeMapping mapping, bool isEven)
    {
        var backgroundColor = isEven ? new Color(0.8f, 0.8f, 0.8f, 0.1f) : Color.clear;
        var originalColor = GUI.backgroundColor;
        GUI.backgroundColor = backgroundColor;

        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = originalColor;

        EditorGUILayout.BeginHorizontal();

        // Type color indicator
        var colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
        EditorGUI.DrawRect(colorRect, mapping.TypeColor);

        // Type info
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(mapping.DisplayName, EditorStyles.boldLabel);

        // Active toggle
        EditorGUI.BeginChangeCheck();
        bool isActive = EditorGUILayout.Toggle(mapping.IsActive, GUILayout.Width(20));
        if (EditorGUI.EndChangeCheck())
        {
            mapping.SetActive(isActive);
            EditorUtility.SetDirty(registry);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Type: {mapping.GetSystemType()?.Name ?? "Unknown"}", EditorStyles.miniLabel);

        int resourceCount = mapping.ResourcesPath?.Count ?? 0;
        EditorGUILayout.LabelField($"Resources: {resourceCount}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        // ResourcesPath field
        EditorGUI.BeginChangeCheck();
        var newPath = EditorGUILayout.ObjectField(mapping.ResourcesPath,
            typeof(ResourcesPath), false, GUILayout.Width(150)) as ResourcesPath;
        if (EditorGUI.EndChangeCheck())
        {
            // Update the mapping (would need to modify ResourceTypeRegistry to support this)
            EditorUtility.SetDirty(registry);
        }

        GUILayout.FlexibleSpace();

        // Actions
        EditorGUILayout.BeginVertical(GUILayout.Width(80));

        if (GUILayout.Button("Open", GUILayout.Height(16)))
        {
            if (mapping.ResourcesPath != null)
            {
                ResourceManageEditorWindow.OpenWindowWithManager(new MenuCommand(mapping.ResourcesPath));
            }
        }

        if (GUILayout.Button("Ping", GUILayout.Height(16)))
        {
            if (mapping.ResourcesPath != null)
            {
                EditorGUIUtility.PingObject(mapping.ResourcesPath);
            }
        }

        if (GUILayout.Button("Remove", GUILayout.Height(16)))
        {
            var systemType = mapping.GetSystemType();
            if (systemType != null && EditorUtility.DisplayDialog("Remove Type Mapping",
                $"Remove mapping for {mapping.DisplayName}?", "Remove", "Cancel"))
            {
                registry.RemoveTypeMapping(systemType);
                EditorUtility.SetDirty(registry);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
}
#endif