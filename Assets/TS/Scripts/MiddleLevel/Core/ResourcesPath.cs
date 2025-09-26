using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Resources Path", menuName = "Scriptable Objects/Resources Path")]
public class ResourcesPath : ScriptableObject
{
    [System.Serializable]
    public class ResourceEntry
    {
        [SerializeField] private string guid;
        [SerializeField] private string assetPath;
        [SerializeField] private string displayName;

        public string Guid => guid;
        public string AssetPath => assetPath;
        public string DisplayName => displayName;

        public ResourceEntry(string guid, string assetPath, string displayName = "")
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

    [SerializeField] private List<ResourceEntry> resourceEntries = new List<ResourceEntry>();

    // Dictionary for fast GUID lookup (not serialized)
    private Dictionary<string, ResourceEntry> guidLookup;
    private Dictionary<string, ResourceEntry> pathLookup;

    private void OnEnable()
    {
        RefreshLookupTables();
    }

    private void RefreshLookupTables()
    {
        guidLookup = new Dictionary<string, ResourceEntry>();
        pathLookup = new Dictionary<string, ResourceEntry>();

        foreach (var entry in resourceEntries)
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
    /// Add a resource entry with GUID and path
    /// </summary>
    public bool AddResource(string guid, string assetPath, string displayName = "")
    {
        if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("GUID and asset path cannot be null or empty");
            return false;
        }

        if (HasGuid(guid))
        {
            return false;
        }

        var entry = new ResourceEntry(guid, assetPath, displayName);
        resourceEntries.Add(entry);
        RefreshLookupTables();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        return true;
    }

    /// <summary>
    /// Remove a resource by GUID
    /// </summary>
    public bool RemoveResource(string guid)
    {
        if (guidLookup == null) RefreshLookupTables();

        if (guidLookup.TryGetValue(guid, out var entry))
        {
            resourceEntries.Remove(entry);
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
    /// Get resource entry by GUID
    /// </summary>
    public ResourceEntry GetResourceEntry(string guid)
    {
        if (guidLookup == null) RefreshLookupTables();

        return guidLookup.TryGetValue(guid, out var entry) ? entry : null;
    }

    /// <summary>
    /// Load asset by GUID
    /// </summary>
    public async UniTask<T> Load<T>(string guid) where T : UnityEngine.Object
    {
        var assetPath = GetAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"No asset path found for GUID: {guid}");
            return null;
        }

        var request = Resources.LoadAsync<T>(ConvertToResourcesPath(assetPath));
        var loadedAsset = await request.ToUniTask(cancellationToken: TokenPool.Get(GetHashCode())) as T;

        if (loadedAsset == null)
            Debug.LogError($"Not found asset by path: {assetPath}");

        return loadedAsset;
    }

    /// <summary>
    /// Load asset by GUID
    /// </summary>
    public T[] LoadAll<T>(string guid) where T : UnityEngine.Object
    {
        var assetPath = GetAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"No asset path found for GUID: {guid}");
            return null;
        }

        return Resources.LoadAll<T>(ConvertToResourcesPath(assetPath));
    }

    /// <summary>
    /// Load asset by GUID (non-generic)
    /// </summary>
    public async UniTask<UnityEngine.Object> Load(string guid, System.Type type)
    {
        var assetPath = GetAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"No asset path found for GUID: {guid}");
            return default;
        }

        var request = Resources.LoadAsync(ConvertToResourcesPath(assetPath), type);
        var loadedAsset = await request.ToUniTask(cancellationToken: TokenPool.Get(GetHashCode()));

        if (loadedAsset == null)
            Debug.LogError($"Not found asset by path: {assetPath}");

        return loadedAsset;
    }

    /// <summary>
    /// Load asset by Name
    /// </summary>
    public async UniTask<T> LoadByNameAsync<T>(string name) where T : UnityEngine.Object
    {
        var resourcesEntry = resourceEntries.Find(entry => entry.DisplayName == name);

        if (resourcesEntry == null)
            return null;

        var request = Resources.LoadAsync<T>(ConvertToResourcesPath(resourcesEntry.AssetPath));
        var loadedAsset = await request.ToUniTask(cancellationToken: TokenPool.Get(GetHashCode())) as T;

        if (loadedAsset == null)
            Debug.LogError($"Not found asset by path: {resourcesEntry.AssetPath}");

        return loadedAsset;
    }

     /// <summary>
    /// Load asset by Name
    /// </summary>
    public T LoadByName<T>(string name) where T : UnityEngine.Object
    {
        var resourcesEntry = resourceEntries.Find(entry => entry.DisplayName == name);

        if (resourcesEntry == null)
            return null;

        var loadedAsset = Resources.Load<T>(ConvertToResourcesPath(resourcesEntry.AssetPath));

        if (loadedAsset == null)
            Debug.LogError($"Not found asset by path: {resourcesEntry.AssetPath}");

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
    /// Get all resource entries
    /// </summary>
    public List<ResourceEntry> GetAllEntries()
    {
        return new List<ResourceEntry>(resourceEntries);
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
        resourceEntries.Clear();
        RefreshLookupTables();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    #endregion

    #region Editor Only Methods

#if UNITY_EDITOR

    /// <summary>
    /// Add resource from Unity Object (Editor only)
    /// </summary>
    public bool AddResourceFromObject(UnityEngine.Object obj, string displayName = "")
    {
        if (obj == null)
        {
            Debug.LogWarning("Cannot add null object");
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(obj);
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        return AddResource(guid, assetPath, displayName);
    }

    /// <summary>
    /// Validate and fix broken references (Editor only)
    /// </summary>
    public void ValidateReferences()
    {
        bool hasChanges = false;

        for (int i = resourceEntries.Count - 1; i >= 0; i--)
        {
            var entry = resourceEntries[i];
            string actualPath = AssetDatabase.GUIDToAssetPath(entry.Guid);

            if (string.IsNullOrEmpty(actualPath))
            {
                Debug.LogWarning($"Removing entry with invalid GUID: {entry.Guid}");
                resourceEntries.RemoveAt(i);
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
                AddResource(guid, assetPath);
                addedCount++;
            }
        }

        Debug.Log($"Added {addedCount} resources from folder: {folderPath}");
    }

#endif

    #endregion

    #region Utility Methods

    private string ConvertToResourcesPath(string assetPath)
    {
        // Convert asset path to Resources path for runtime loading
        const string resourcesFolder = "Resources/";
        int resourcesIndex = assetPath.IndexOf(resourcesFolder);

        if (resourcesIndex >= 0)
        {
            string resourcePath = assetPath.Substring(resourcesIndex + resourcesFolder.Length);
            return System.IO.Path.ChangeExtension(resourcePath, null);
        }

        return assetPath;
    }

    /// <summary>
    /// Get resource count
    /// </summary>
    public int Count => resourceEntries.Count;

    #endregion
}