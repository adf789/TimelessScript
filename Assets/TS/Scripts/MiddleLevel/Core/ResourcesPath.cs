using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Resourcess Path", menuName = "Scriptable Objects/Resourcess Path")]
public class ResourcesPath : ScriptableObject
{
    [System.Serializable]
    public class ResourcesEntry
    {
        [SerializeField] private string guid;
        [SerializeField] private string assetPath;
        [SerializeField] private string displayName;

        public string Guid => guid;
        public string AssetPath => assetPath;
        public string DisplayName => displayName;

        public ResourcesEntry(string guid, string assetPath, string displayName = "")
        {
            this.guid = guid;
            this.assetPath = assetPath;
            this.displayName = string.IsNullOrEmpty(displayName) ? System.IO.Path.GetFileNameWithoutExtension(assetPath) : displayName;
        }

        public void UpdatePath(string newPath)
        {
            this.assetPath = newPath;
        }

        public void UpdateDisplayName(string newDisplayName)
        {
            this.displayName = newDisplayName;
        }
    }

    [SerializeField] private List<ResourcesEntry> ResourcesEntries = new List<ResourcesEntry>();

    // Dictionary for fast GUID lookup (not serialized)
    private Dictionary<string, ResourcesEntry> guidLookup;
    private Dictionary<string, ResourcesEntry> pathLookup;

    private void OnEnable()
    {
        RefreshLookupTables();
    }

    private void OnDisable()
    {
        TokenPool.Cancel(GetHashCode());
    }

    private void RefreshLookupTables()
    {
        guidLookup = new Dictionary<string, ResourcesEntry>();
        pathLookup = new Dictionary<string, ResourcesEntry>();

        foreach (var entry in ResourcesEntries)
        {
            if (!string.IsNullOrEmpty(entry.Guid))
            {
                guidLookup[entry.Guid] = entry;
            }
            if (!string.IsNullOrEmpty(entry.AssetPath))
            {
                pathLookup[entry.AssetPath] = entry;
            }
        }
    }

    #region Public API

    /// <summary>
    /// Add a Resources entry with GUID and path
    /// </summary>
    public bool AddResources(string guid, string assetPath, string displayName = "")
    {
        if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("GUID and asset path cannot be null or empty");
            return false;
        }

        if (HasGuid(guid))
        {
            Debug.LogWarning($"Resources with GUID {guid} already exists");
            return false;
        }

        var entry = new ResourcesEntry(guid, assetPath, displayName);
        ResourcesEntries.Add(entry);
        RefreshLookupTables();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        return true;
    }

    /// <summary>
    /// Remove a Resources by GUID
    /// </summary>
    public bool RemoveResources(string guid)
    {
        if (guidLookup == null) RefreshLookupTables();

        if (guidLookup.TryGetValue(guid, out var entry))
        {
            ResourcesEntries.Remove(entry);
            RefreshLookupTables();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            return true;
        }

        return false;
    }

    /// <summary>
    /// Get asset path by GUID
    /// </summary>
    public string GetAssetPath(string guid)
    {
        if (guidLookup == null) RefreshLookupTables();

        return guidLookup.TryGetValue(guid, out var entry) ? entry.AssetPath : null;
    }

    /// <summary>
    /// Get GUID by asset path
    /// </summary>
    public string GetGuid(string assetPath)
    {
        if (pathLookup == null) RefreshLookupTables();

        return pathLookup.TryGetValue(assetPath, out var entry) ? entry.Guid : null;
    }

    /// <summary>
    /// Get Resources entry by GUID
    /// </summary>
    public ResourcesEntry GetResourcesEntry(string guid)
    {
        if (guidLookup == null) RefreshLookupTables();

        return guidLookup.TryGetValue(guid, out var entry) ? entry : null;
    }

    /// <summary>
    /// Load asset by GUID
    /// </summary>
    public async UniTask<T> LoadAsset<T>(string guid) where T : UnityEngine.Object
    {
        var assetPath = GetAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"No asset path found for GUID: {guid}");
            return null;
        }

        var request = Resources.LoadAsync<T>(ConvertToResourcessPath(assetPath));
        var loadedAsset = await request.ToUniTask(cancellationToken: TokenPool.Get(GetHashCode())) as T;

        if (loadedAsset == null)
            Debug.LogError($"Not found asset by path: {assetPath}");

        return loadedAsset;
    }

    /// <summary>
    /// Load asset by GUID (non-generic)
    /// </summary>
    public async UniTask<UnityEngine.Object> LoadAsset(string guid, System.Type type)
    {
        var assetPath = GetAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"No asset path found for GUID: {guid}");
            return default;
        }

        var request = Resources.LoadAsync(ConvertToResourcessPath(assetPath), type);
        var loadedAsset = await request.ToUniTask(cancellationToken: TokenPool.Get(GetHashCode()));

        if (loadedAsset == null)
            Debug.LogError($"Not found asset by path: {assetPath}");

        return loadedAsset;
    }

    /// <summary>
    /// Load asset by Name
    /// </summary>
    public async UniTask<T> LoadAssetByName<T>(string name) where T : UnityEngine.Object
    {
        if (ResourcesEntries == null)
            return null;

        var ResourcesEntry = ResourcesEntries.Find(entry => entry.DisplayName == name);

        if (ResourcesEntry == null)
            return null;

        var request = Resources.LoadAsync<T>(ConvertToResourcessPath(ResourcesEntry.AssetPath));
        var loadedAsset = await request.ToUniTask(cancellationToken: TokenPool.Get(GetHashCode())) as T;

        if (loadedAsset == null)
            Debug.LogError($"Not found asset by path: {ResourcesEntry.AssetPath}");

        return loadedAsset;
    }

    /// <summary>
    /// Check if GUID exists
    /// </summary>
    public bool HasGuid(string guid)
    {
        if (guidLookup == null) RefreshLookupTables();
        return guidLookup.ContainsKey(guid);
    }

    /// <summary>
    /// Check if asset path exists
    /// </summary>
    public bool HasAssetPath(string assetPath)
    {
        if (pathLookup == null) RefreshLookupTables();
        return pathLookup.ContainsKey(assetPath);
    }

    /// <summary>
    /// Get all Resources entries
    /// </summary>
    public List<ResourcesEntry> GetAllEntries()
    {
        return new List<ResourcesEntry>(ResourcesEntries);
    }

    /// <summary>
    /// Update asset path for existing GUID
    /// </summary>
    public bool UpdateAssetPath(string guid, string newAssetPath)
    {
        if (guidLookup == null) RefreshLookupTables();

        if (guidLookup.TryGetValue(guid, out var entry))
        {
            entry.UpdatePath(newAssetPath);
            RefreshLookupTables();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            return true;
        }

        return false;
    }

    /// <summary>
    /// Clear all entries
    /// </summary>
    public void Clear()
    {
        ResourcesEntries.Clear();
        RefreshLookupTables();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    #endregion

    #region Editor Only Methods

#if UNITY_EDITOR

    /// <summary>
    /// Add Resources from Unity Object (Editor only)
    /// </summary>
    public bool AddResourcesFromObject(UnityEngine.Object obj, string displayName = "")
    {
        if (obj == null)
        {
            Debug.LogWarning("Cannot add null object");
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(obj);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        return AddResources(guid, assetPath, displayName);
    }

    /// <summary>
    /// Validate and fix broken references (Editor only)
    /// </summary>
    public void ValidateReferences()
    {
        bool hasChanges = false;

        for (int i = ResourcesEntries.Count - 1; i >= 0; i--)
        {
            var entry = ResourcesEntries[i];
            string actualPath = AssetDatabase.GUIDToAssetPath(entry.Guid);

            if (string.IsNullOrEmpty(actualPath))
            {
                Debug.LogWarning($"Removing entry with invalid GUID: {entry.Guid}");
                ResourcesEntries.RemoveAt(i);
                hasChanges = true;
            }
            else if (actualPath != entry.AssetPath)
            {
                Debug.Log($"Updating path for {entry.Guid}: {entry.AssetPath} -> {actualPath}");
                entry.UpdatePath(actualPath);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            RefreshLookupTables();
            EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// Auto-populate from a folder (Editor only)
    /// </summary>
    public void PopulateFromFolder(string folderPath, bool includeSubfolders = true)
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        int addedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (!includeSubfolders && System.IO.Path.GetDirectoryName(assetPath) != folderPath)
                continue;

            if (!AssetDatabase.IsValidFolder(assetPath) && !HasGuid(guid))
            {
                AddResources(guid, assetPath);
                addedCount++;
            }
        }

        Debug.Log($"Added {addedCount} Resourcess from folder: {folderPath}");
    }

#endif

    #endregion

    #region Utility Methods

    private string ConvertToResourcessPath(string assetPath)
    {
        // Convert asset path to Resourcess path for runtime loading
        const string ResourcessFolder = "Resourcess/";
        int ResourcessIndex = assetPath.IndexOf(ResourcessFolder);

        if (ResourcessIndex >= 0)
        {
            string ResourcesPath = assetPath.Substring(ResourcessIndex + ResourcessFolder.Length);
            return System.IO.Path.ChangeExtension(ResourcesPath, null);
        }

        return assetPath;
    }

    /// <summary>
    /// Get Resources count
    /// </summary>
    public int Count => ResourcesEntries.Count;

    #endregion
}