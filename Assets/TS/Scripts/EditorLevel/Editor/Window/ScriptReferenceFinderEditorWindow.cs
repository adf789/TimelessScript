#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ScriptReferenceFinderWindow : EditorWindow
{
    [MenuItem("TS/Finder/Script Reference Finder", false, 1)]
    public static void OpenWindow()
    {
        ScriptReferenceFinderWindow window = (ScriptReferenceFinderWindow) GetWindow(typeof(ScriptReferenceFinderWindow));
        window.titleContent.text = "Script Reference Finder";
    }

    #region Coding rule : Property
    #endregion Coding rule : Property

    #region Coding rule : Value
    private MonoScript _selectedScript;
    private List<SearchResult> _searchResults = null;
    private Vector2 _scrollPosition;
    private bool _searchInPrefabs = true;
    private bool _searchInScenes = true;
    private SearchType _currentSearchType = SearchType.None;

    private enum SearchType
    {
        None,
        Prefabs,
        Scenes,
        Both
    }

    private class SearchResult
    {
        public string Path;
        public string Name;
        public Object Asset;

        public SearchResult(string path, string name, Object asset)
        {
            Path = path;
            Name = name;
            Asset = asset;
        }
    }
    #endregion Coding rule : Value

    #region Coding rule : Function
    private void OnGUI()
    {
        GUILayout.Label("Find Script References", EditorStyles.boldLabel);

        // Script ObjectField
        EditorGUI.BeginChangeCheck();
        {
            _selectedScript = (MonoScript) EditorGUILayout.ObjectField("Script:", _selectedScript, typeof(MonoScript), false);

            if (_selectedScript == null)
            {
                EditorGUILayout.HelpBox("Select a C# script to search for references.", MessageType.Info);
                return;
            }
        }
        bool scriptChanged = EditorGUI.EndChangeCheck();

        if (scriptChanged)
        {
            _searchResults = null;
            _currentSearchType = SearchType.None;
        }

        GUILayout.Space(10);

        // Search Options
        GUILayout.Label("Search Options", EditorStyles.boldLabel);
        _searchInPrefabs = EditorGUILayout.Toggle("Search in Prefabs", _searchInPrefabs);
        _searchInScenes = EditorGUILayout.Toggle("Search in Scenes", _searchInScenes);

        GUILayout.Space(10);

        // Search Button
        EditorGUI.BeginDisabledGroup(!_searchInPrefabs && !_searchInScenes);
        {
            if (GUILayout.Button("Find References", GUILayout.Height(30)))
            {
                FindScriptReferences();
            }
        }
        EditorGUI.EndDisabledGroup();

        // Display Results
        if (_searchResults != null)
        {
            GUILayout.Space(10);
            DisplayResults();
        }
    }

    private void FindScriptReferences()
    {
        if (_selectedScript == null)
            return;

        _searchResults = new List<SearchResult>();
        string scriptGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_selectedScript));
        string scriptName = _selectedScript.name;

        // Progress bar setup
        int totalAssets = 0;
        int currentAsset = 0;

        if (_searchInPrefabs)
            totalAssets += AssetDatabase.FindAssets("t:Prefab").Length;
        if (_searchInScenes)
            totalAssets += AssetDatabase.FindAssets("t:Scene").Length;

        try
        {
            // Search in Prefabs
            if (_searchInPrefabs)
            {
                string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
                foreach (string guid in prefabGUIDs)
                {
                    currentAsset++;
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Searching References",
                        $"Checking Prefabs... ({currentAsset}/{totalAssets})\n{path}",
                        (float) currentAsset / totalAssets))
                    {
                        break;
                    }

                    if (IsScriptReferencedInAsset(path, scriptGUID, scriptName))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        _searchResults.Add(new SearchResult(path, System.IO.Path.GetFileNameWithoutExtension(path), prefab));
                    }
                }
            }

            // Search in Scenes
            if (_searchInScenes)
            {
                string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
                foreach (string guid in sceneGUIDs)
                {
                    currentAsset++;
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Searching References",
                        $"Checking Scenes... ({currentAsset}/{totalAssets})\n{path}",
                        (float) currentAsset / totalAssets))
                    {
                        break;
                    }

                    if (IsScriptReferencedInAsset(path, scriptGUID, scriptName))
                    {
                        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        _searchResults.Add(new SearchResult(path, System.IO.Path.GetFileNameWithoutExtension(path), scene));
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // Set search type for display
        if (_searchInPrefabs && _searchInScenes)
            _currentSearchType = SearchType.Both;
        else if (_searchInPrefabs)
            _currentSearchType = SearchType.Prefabs;
        else if (_searchInScenes)
            _currentSearchType = SearchType.Scenes;

        Debug.Log($"Script Reference Search Complete: Found {_searchResults.Count} references to '{scriptName}'");
    }

    private bool IsScriptReferencedInAsset(string assetPath, string scriptGUID, string scriptName)
    {
        try
        {
            string content = System.IO.File.ReadAllText(assetPath);

            // Check for GUID reference (more reliable)
            string guidPattern = $"guid: {scriptGUID}";
            if (content.Contains(guidPattern))
                return true;

            // Check for script name reference (fallback)
            string scriptPattern = $"m_Script: {{fileID: 11500000, guid: {scriptGUID}";
            if (content.Contains(scriptPattern))
                return true;

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to read asset: {assetPath}\n{e.Message}");
            return false;
        }
    }

    private void DisplayResults()
    {
        GUILayout.Space(5);

        string searchTypeText = _currentSearchType switch
        {
            SearchType.Prefabs => "in Prefabs",
            SearchType.Scenes => "in Scenes",
            SearchType.Both => "in Prefabs and Scenes",
            _ => ""
        };

        if (_searchResults.Count > 0)
        {
            GUILayout.Label($"Found {_searchResults.Count} References {searchTypeText}", EditorStyles.boldLabel);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            {
                foreach (var result in _searchResults)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    {
                        // Asset icon
                        Texture icon = AssetDatabase.GetCachedIcon(result.Path);
                        GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

                        // Asset name button
                        if (GUILayout.Button(result.Name, EditorStyles.label))
                        {
                            if (result.Asset != null)
                            {
                                EditorGUIUtility.PingObject(result.Asset);
                                Selection.activeObject = result.Asset;
                            }
                        }

                        // Path label
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(result.Path, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Export results button
            if (GUILayout.Button("Export Results to Console"))
            {
                ExportResultsToConsole();
            }
        }
        else
        {
            GUILayout.Label($"No References Found {searchTypeText}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"No prefabs or scenes reference the script '{_selectedScript.name}'.", MessageType.Info);
        }
    }

    private void ExportResultsToConsole()
    {
        if (_searchResults == null || _searchResults.Count == 0)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Script Reference Results for '{_selectedScript.name}' ===");
        sb.AppendLine($"Total References: {_searchResults.Count}");
        sb.AppendLine();

        foreach (var result in _searchResults)
        {
            sb.AppendLine($"- {result.Name}");
            sb.AppendLine($"  Path: {result.Path}");
        }

        Debug.Log(sb.ToString());
    }
    #endregion Coding rule : Function
}
#endif
