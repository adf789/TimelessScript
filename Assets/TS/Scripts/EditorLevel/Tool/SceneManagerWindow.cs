#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SceneManagerWindow : EditorWindow
{
    [MenuItem("Tools/Scene Manager %&s")]
    public static void ShowWindow()
    {
        GetWindow<SceneManagerWindow>("Scene Manager");
    }

    private Vector2 scrollPosition;
    private string searchFilter = "";
    private bool showBuildSettingsOnly = false;
    private SceneAsset playModeStartScene;

    private class SceneInfo
    {
        public string path;
        public string name;
        public bool inBuildSettings;
        public int buildIndex;
        public bool enabled;
    }

    private List<SceneInfo> allScenes = new List<SceneInfo>();

    private void OnEnable()
    {
        RefreshSceneList();

        // Load current play mode start scene
        var currentStartScene = EditorSceneManager.playModeStartScene;
        if (currentStartScene != null)
        {
            playModeStartScene = currentStartScene;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        // Header
        GUILayout.Label("Scene Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Play Mode Start Scene Setting
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Play Mode Start Scene:", GUILayout.Width(150));

        EditorGUI.BeginChangeCheck();
        playModeStartScene = (SceneAsset)EditorGUILayout.ObjectField(
            playModeStartScene,
            typeof(SceneAsset),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            EditorSceneManager.playModeStartScene = playModeStartScene;
        }

        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            EditorSceneManager.playModeStartScene = null;
            playModeStartScene = null;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Play Mode Start Scene으로 설정하면 플레이 버튼을 눌렀을 때 해당 씬부터 시작됩니다.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshSceneList();
        }

        showBuildSettingsOnly = GUILayout.Toggle(
            showBuildSettingsOnly,
            "Build Only",
            EditorStyles.toolbarButton,
            GUILayout.Width(80)
        );

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Scene List
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var filteredScenes = allScenes
            .Where(s => string.IsNullOrEmpty(searchFilter) ||
                       s.name.ToLower().Contains(searchFilter.ToLower()))
            .Where(s => !showBuildSettingsOnly || s.inBuildSettings)
            .OrderBy(s => s.inBuildSettings ? s.buildIndex : 9999)
            .ThenBy(s => s.name);

        foreach (var scene in filteredScenes)
        {
            DrawSceneItem(scene);
        }

        EditorGUILayout.EndScrollView();

        // Footer Info
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            $"Total: {allScenes.Count} scenes | In Build Settings: {allScenes.Count(s => s.inBuildSettings)}",
            EditorStyles.miniLabel
        );
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSceneItem(SceneInfo scene)
    {
        EditorGUILayout.BeginHorizontal("box");

        // Build Settings Badge
        if (scene.inBuildSettings)
        {
            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel);
            badgeStyle.normal.textColor = scene.enabled ? Color.green : Color.gray;
            badgeStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(
                $"[{scene.buildIndex}]",
                badgeStyle,
                GUILayout.Width(30)
            );
        }
        else
        {
            EditorGUILayout.LabelField("", GUILayout.Width(30));
        }

        // Scene Name
        EditorGUILayout.LabelField(scene.name, GUILayout.MinWidth(150));

        // Current Scene Indicator
        if (SceneManager.GetActiveScene().path == scene.path)
        {
            GUIStyle currentStyle = new GUIStyle(EditorStyles.miniLabel);
            currentStyle.normal.textColor = Color.cyan;
            EditorGUILayout.LabelField("● Current", currentStyle, GUILayout.Width(70));
        }

        GUILayout.FlexibleSpace();

        // Buttons
        if (GUILayout.Button("Open", GUILayout.Width(50)))
        {
            OpenScene(scene.path);
        }

        if (GUILayout.Button("Play", GUILayout.Width(50)))
        {
            PlayFromScene(scene.path);
        }

        if (GUILayout.Button("Ping", GUILayout.Width(50)))
        {
            PingScene(scene.path);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void RefreshSceneList()
    {
        allScenes.Clear();

        // Get all scene GUIDs from Assets folder only
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

        // Build settings scenes
        var buildScenes = EditorBuildSettings.scenes
            .Select((s, i) => new { scene = s, index = i })
            .ToDictionary(x => x.scene.path, x => (x.index, x.scene.enabled));

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Double check path is in Assets folder
            if (!path.StartsWith("Assets/"))
                continue;

            string name = Path.GetFileNameWithoutExtension(path);

            bool inBuild = buildScenes.ContainsKey(path);
            int buildIndex = inBuild ? buildScenes[path].index : -1;
            bool enabled = inBuild ? buildScenes[path].enabled : false;

            allScenes.Add(new SceneInfo
            {
                path = path,
                name = name,
                inBuildSettings = inBuild,
                buildIndex = buildIndex,
                enabled = enabled
            });
        }
    }

    private void OpenScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }

    private void PlayFromScene(string scenePath)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // Set as play mode start scene temporarily
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;

            // Open and play
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }

    private void PingScene(string scenePath)
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        EditorGUIUtility.PingObject(sceneAsset);
        Selection.activeObject = sceneAsset;
    }
}
#endif
