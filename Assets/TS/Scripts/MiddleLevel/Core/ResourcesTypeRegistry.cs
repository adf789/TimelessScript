using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Resource Type Registry", menuName = "Scriptable Objects/Resource Type Registry")]
public class ResourcesTypeRegistry : ScriptableObject
{
    [System.Serializable]
    public class TypeMapping
    {
        [SerializeField] private string typeName;
        [SerializeField] private string displayName;
        [SerializeField] private ResourcesPath resourcesPath;
        [SerializeField] private Color typeColor = Color.white;
        [SerializeField] private bool isActive = true;

        public string TypeName => typeName;
        public string DisplayName => displayName;
        public ResourcesPath ResourcesPath => resourcesPath;
        public Color TypeColor => typeColor;
        public bool IsActive => isActive;

        public TypeMapping(Type type, ResourcesPath resourcesPath)
        {
            this.typeName = type.AssemblyQualifiedName;
            this.displayName = type.Name;
            this.resourcesPath = resourcesPath;
            this.typeColor = GenerateColorFromTypeName(type.Name);
        }

        public TypeMapping(string typeName, string displayName, ResourcesPath resourcesPath)
        {
            this.typeName = typeName;
            this.displayName = displayName;
            this.resourcesPath = resourcesPath;
            this.typeColor = GenerateColorFromTypeName(displayName);
        }

        public Type GetSystemType()
        {
            try
            {
                return Type.GetType(typeName);
            }
            catch
            {
                return null;
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        public void SetDisplayName(string name)
        {
            displayName = name;
        }

        public void SetTypeColor(Color color)
        {
            typeColor = color;
        }

        private static Color GenerateColorFromTypeName(string name)
        {
            // Ÿ�� �̸��� ������� ���� ����
            int hash = name.GetHashCode();
            var random = new System.Random(hash);

            return new Color(
                0.3f + random.Next(0, 70) / 100f,
                0.3f + random.Next(0, 70) / 100f,
                0.3f + random.Next(0, 70) / 100f,
                1.0f
            );
        }
    }

    [SerializeField] private List<TypeMapping> typeMappings = new List<TypeMapping>();
    [SerializeField] private ResourcesPath defaultResourcesPath;

    // Runtime lookup cache
    private Dictionary<Type, ResourcesPath> typeToPathCache;
    private Dictionary<string, ResourcesPath> typeNameToPathCache;

    private static ResourcesTypeRegistry registry = null;

    public static ResourcesTypeRegistry Get()
    {
        if (registry)
            return registry;

        registry = Resources.Load<ResourcesTypeRegistry>(StringDefine.PATH_RESOURCES_REGISTRY);

        return registry;
    }

    private void OnEnable()
    {
        RefreshCache();
    }

    private void RefreshCache()
    {
        typeToPathCache = new Dictionary<Type, ResourcesPath>();
        typeNameToPathCache = new Dictionary<string, ResourcesPath>();

        foreach (var mapping in typeMappings.Where(m => m.IsActive && m.ResourcesPath != null))
        {
            var systemType = mapping.GetSystemType();
            if (systemType != null)
            {
                typeToPathCache[systemType] = mapping.ResourcesPath;
                typeNameToPathCache[mapping.TypeName] = mapping.ResourcesPath;
            }
        }
    }

    #region Public API

    /// <summary>
    /// Get ResourcesPath for a specific type
    /// </summary>
    public ResourcesPath GetResourcesPath<T>() where T : UnityEngine.Object
    {
        return GetResourcesPath(typeof(T));
    }

    /// <summary>
    /// Get ResourcesPath for a specific type
    /// </summary>
    public ResourcesPath GetResourcesPath(Type type)
    {
        if (typeToPathCache == null) RefreshCache();

        if (typeToPathCache.TryGetValue(type, out var resourcesPath))
        {
            return resourcesPath;
        }

        // Check base types and interfaces
        foreach (var kvp in typeToPathCache)
        {
            if (kvp.Key.IsAssignableFrom(type))
            {
                return kvp.Value;
            }
        }

        return defaultResourcesPath;
    }

    /// <summary>
    /// Get ResourcesPath by type name
    /// </summary>
    public ResourcesPath GetResourcesPath(string typeName)
    {
        if (typeNameToPathCache == null) RefreshCache();

        return typeNameToPathCache.TryGetValue(typeName, out var resourcesPath)
            ? resourcesPath
            : defaultResourcesPath;
    }

    /// <summary>
    /// Load asset using type-specific ResourcesPath
    /// </summary>
    public async UniTask<T> Load<T>(string guid) where T : UnityEngine.Object
    {
        var resourcesPath = GetResourcesPath<T>();

        if (resourcesPath == null)
            return null;

        return await resourcesPath.Load<T>(guid);
    }

    /// <summary>
    /// Load asset using type-specific ResourcesPath
    /// </summary>
    public async UniTask<UnityEngine.Object> Load(string guid, Type type)
    {
        var resourcesPath = GetResourcesPath(type);

        if (resourcesPath == null)
            return null;

        return await resourcesPath.Load(guid, type);
    }

    /// <summary>
    /// Add type mapping
    /// </summary>
    public bool AddTypeMapping(Type type, ResourcesPath resourcesPath, string customDisplayName = null)
    {
        if (type == null || resourcesPath == null)
        {
            Debug.LogWarning("Type and ResourcesPath cannot be null");
            return false;
        }

        // Check if mapping already exists
        if (typeMappings.Any(m => m.TypeName == type.AssemblyQualifiedName))
        {
            Debug.LogWarning($"Type mapping already exists: {type.Name}");
            return false;
        }

        var mapping = new TypeMapping(type, resourcesPath);
        if (!string.IsNullOrEmpty(customDisplayName))
        {
            mapping.SetDisplayName(customDisplayName);
        }

        typeMappings.Add(mapping);
        RefreshCache();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        return true;
    }

    /// <summary>
    /// Remove type mapping
    /// </summary>
    public bool RemoveTypeMapping(Type type)
    {
        var mapping = typeMappings.FirstOrDefault(m => m.GetSystemType() == type);
        if (mapping != null)
        {
            typeMappings.Remove(mapping);
            RefreshCache();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            return true;
        }

        return false;
    }

    /// <summary>
    /// Get all type mappings
    /// </summary>
    public List<TypeMapping> GetAllMappings()
    {
        return new List<TypeMapping>(typeMappings);
    }

    /// <summary>
    /// Get active type mappings
    /// </summary>
    public List<TypeMapping> GetActiveMappings()
    {
        return typeMappings.Where(m => m.IsActive).ToList();
    }

    /// <summary>
    /// Set default ResourcesPath
    /// </summary>
    public void SetDefaultResourcesPath(ResourcesPath path)
    {
        defaultResourcesPath = path;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Get supported types
    /// </summary>
    public Type[] GetSupportedTypes()
    {
        return typeMappings
            .Where(m => m.IsActive)
            .Select(m => m.GetSystemType())
            .Where(t => t != null)
            .ToArray();
    }

    #endregion

    #region Editor Methods

#if UNITY_EDITOR

    /// <summary>
    /// Auto-detect and register common Unity types
    /// </summary>
    public void RegisterCommonUnityTypes()
    {
        var commonTypes = new Type[]
        {
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
            typeof(PhysicsMaterial),
            typeof(Avatar)
        };

        int registeredCount = 0;

        foreach (var type in commonTypes)
        {
            if (!typeMappings.Any(m => m.GetSystemType() == type))
            {
                // Create a new ResourcesPath for this type
                string resourcesPathName = $"ResourcesPath_{type.Name}";
                var newResourcesPath = CreateInstance<ResourcesPath>();
                newResourcesPath.name = resourcesPathName;

                // Save as sub-asset
                AssetDatabase.AddObjectToAsset(newResourcesPath, this);

                var mapping = new TypeMapping(type, newResourcesPath);
                typeMappings.Add(mapping);
                registeredCount++;
            }
        }

        if (registeredCount > 0)
        {
            RefreshCache();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"Registered {registeredCount} common Unity types");
        }
        else
        {
            Debug.Log("All common Unity types are already registered");
        }
    }

    /// <summary>
    /// Validate all mappings and ResourcesPaths
    /// </summary>
    public void ValidateAllMappings()
    {
        int fixedCount = 0;

        for (int i = typeMappings.Count - 1; i >= 0; i--)
        {
            var mapping = typeMappings[i];

            // Check if type exists
            if (mapping.GetSystemType() == null)
            {
                Debug.LogWarning($"Removing mapping with invalid type: {mapping.TypeName}");
                typeMappings.RemoveAt(i);
                fixedCount++;
                continue;
            }

            // Validate ResourcesPath
            if (mapping.ResourcesPath != null)
            {
                mapping.ResourcesPath.ValidateReferences();
            }
        }

        if (fixedCount > 0)
        {
            RefreshCache();
            EditorUtility.SetDirty(this);
            Debug.Log($"Fixed {fixedCount} invalid mappings");
        }

        // Validate default ResourcesPath
        if (defaultResourcesPath != null)
        {
            defaultResourcesPath.ValidateReferences();
        }
    }

    /// <summary>
    /// Create ResourcesPath for a type as sub-asset and add type mapping
    /// </summary>
    public ResourcesPath CreateResourcesPathForType(Type type, bool addTypeMapping = true)
    {
        string resourcesPathName = $"ResourcesPath_{type.Name}";
        var newResourcesPath = CreateInstance<ResourcesPath>();
        newResourcesPath.name = resourcesPathName;

        // Save as sub-asset to this ScriptableObject
        AssetDatabase.AddObjectToAsset(newResourcesPath, this);

        // Automatically add type mapping if requested
        if (addTypeMapping)
        {
            // Check if mapping doesn't already exist
            if (!typeMappings.Any(m => m.GetSystemType() == type))
            {
                var mapping = new TypeMapping(type, newResourcesPath);
                typeMappings.Add(mapping);
                RefreshCache();
            }
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        return newResourcesPath;
    }

    /// <summary>
    /// Get statistics
    /// </summary>
    public string GetStatistics()
    {
        var stats = new System.Text.StringBuilder();
        stats.AppendLine($"Total Mappings: {typeMappings.Count}");
        stats.AppendLine($"Active Mappings: {GetActiveMappings().Count}");
        stats.AppendLine($"Has Default Path: {defaultResourcesPath != null}");

        stats.AppendLine("\nResource Counts by Type:");
        foreach (var mapping in GetActiveMappings())
        {
            int resourceCount = mapping.ResourcesPath?.Count ?? 0;
            stats.AppendLine($"  {mapping.DisplayName}: {resourceCount} resources");
        }

        return stats.ToString();
    }

#endif

    #endregion
}