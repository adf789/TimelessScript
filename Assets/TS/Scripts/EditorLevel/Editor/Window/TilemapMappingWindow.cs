#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 타일맵 패턴 관리 도구
/// 초기 패턴 설정, SubScene 관리, 6방향 Port 연결 관리
/// </summary>
public class TilemapMappingWindow : EditorWindow
{
    private TilemapPatternRegistry _registry;
    private Vector2 _scrollPosition;

    private int _selectedPatternIndex = -1;
    private string _patternSearchFilter = "";

    // Port 추가 UI 상태
    private bool _showAddPortSection = false;
    private FourDirection _newPortDirection = FourDirection.Right;
    private int _newPortPosition = 0;

    private GUIStyle _headerStyle;
    private GUIStyle _subHeaderStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _selectedBoxStyle;

    [MenuItem("TS/Tilemap/Pattern Editor %&m")]
    public static void ShowWindow()
    {
        var window = GetWindow<TilemapMappingWindow>("Pattern Editor");
        window.minSize = new Vector2(700, 600);
        window.Show();
    }

    private void OnEnable()
    {
        FindRegistry();
    }

    private void OnGUI()
    {
        InitializeStyles();

        DrawHeader();
        DrawRegistrySelection();

        if (_registry == null) return;

        EditorGUILayout.BeginHorizontal();

        // Left panel: Pattern list
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
        DrawPatternList();
        EditorGUILayout.EndVertical();

        // Right panel: Pattern editor
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));
        DrawPatternEditor();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void InitializeStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _selectedBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                normal = { background = MakeTex(2, 2, new Color(0.5f, 0.7f, 1f, 0.3f)) }
            };
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tilemap Pattern Editor", _headerStyle);
        EditorGUILayout.LabelField("초기 패턴 및 Port 연결 관리", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "초기 패턴을 설정하고 각 패턴의 Port를 다른 패턴과 연결할 수 있습니다.\n" +
            "Port는 6방향으로 반대 방향끼리 연결됩니다 (TopLeft ↔ BottomRight 등).",
            MessageType.Info
        );
        EditorGUILayout.Space(10);
    }

    private void DrawRegistrySelection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Registry:", GUILayout.Width(70));

        var newRegistry = (TilemapPatternRegistry) EditorGUILayout.ObjectField(
            _registry,
            typeof(TilemapPatternRegistry),
            false
        );

        if (newRegistry != _registry)
        {
            _registry = newRegistry;
            _selectedPatternIndex = -1;
        }

        if (GUILayout.Button("Find", GUILayout.Width(60)))
        {
            FindRegistry();
        }

        if (GUILayout.Button("Save", GUILayout.Width(60)))
        {
            SaveRegistry();
        }

        EditorGUILayout.EndHorizontal();

        if (_registry == null)
        {
            EditorGUILayout.HelpBox("레지스트리를 선택하거나 'Find' 버튼을 눌러 자동으로 찾으세요.", MessageType.Warning);
            return;
        }

        // Initial Pattern setting
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(_boxStyle);
        EditorGUILayout.LabelField("Initial Pattern (게임 시작 패턴)", EditorStyles.boldLabel);

        var patterns = _registry.AllPatterns.Where(p => p != null).Select(p => p.PatternID).ToArray();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawPatternList()
    {
        EditorGUILayout.LabelField("Patterns", _subHeaderStyle);
        EditorGUILayout.Space(5);

        // Search filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
        _patternSearchFilter = EditorGUILayout.TextField(_patternSearchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            _patternSearchFilter = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Pattern list
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        var filteredPatterns = string.IsNullOrEmpty(_patternSearchFilter)
            ? _registry.AllPatterns.Where(p => p != null).ToList()
            : _registry.AllPatterns.Where(p => p != null &&
                (p.PatternID.ToLower().Contains(_patternSearchFilter.ToLower())))
                .ToList();

        for (int i = 0; i < filteredPatterns.Count; i++)
        {
            var pattern = filteredPatterns[i];
            int actualIndex = _registry.AllPatterns.IndexOf(pattern);
            bool isSelected = actualIndex == _selectedPatternIndex;

            var boxStyle = isSelected ? _selectedBoxStyle : _boxStyle;

            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Pattern info button
            var buttonStyle = new GUIStyle(GUI.skin.button);
            if (isSelected)
            {
                buttonStyle.normal.textColor = Color.green;
                buttonStyle.fontStyle = FontStyle.Bold;
            }

            if (GUILayout.Button($"{pattern.PatternID}", buttonStyle, GUILayout.Height(40)))
            {
                _selectedPatternIndex = actualIndex;
            }

            EditorGUILayout.EndHorizontal();

            // SubScene indicator
            if (pattern.SubScene.IsReferenceValid)
            {
                EditorGUILayout.LabelField($"SubScene: {pattern.SubScene.ToString()}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("SubScene: (None)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (filteredPatterns.Count == 0)
        {
            EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPatternEditor()
    {
        EditorGUILayout.LabelField("Pattern Editor", _subHeaderStyle);
        EditorGUILayout.Space(5);

        if (_selectedPatternIndex < 0 || _selectedPatternIndex >= _registry.AllPatterns.Count)
        {
            EditorGUILayout.HelpBox("좌측에서 패턴을 선택하세요.", MessageType.Info);
            return;
        }

        var pattern = _registry.AllPatterns[_selectedPatternIndex];
        if (pattern == null)
        {
            EditorGUILayout.HelpBox("선택한 패턴이 null입니다.", MessageType.Error);
            return;
        }

        EditorGUILayout.BeginVertical(_boxStyle);

        // Basic info
        EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Pattern ID: {pattern.PatternID}");
        EditorGUILayout.LabelField($"Grid Size: {_registry.GridSize.x} x {_registry.GridSize.y}");

        EditorGUILayout.Space(10);

        // SubScene setting
        EditorGUILayout.LabelField("SubScene Registry:", GUILayout.Width(70));

        EditorGUILayout.Space(10);

        // Quick actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Open in Inspector"))
        {
            Selection.activeObject = pattern;
            EditorGUIUtility.PingObject(pattern);
        }

        EditorGUILayout.EndVertical();
    }

    private void FindRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:TilemapPatternRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _registry = AssetDatabase.LoadAssetAtPath<TilemapPatternRegistry>(path);
            Debug.Log($"[TilemapMappingWindow] Found registry: {path}");
        }
        else
        {
            Debug.LogWarning("[TilemapMappingWindow] No TilemapPatternRegistry found in project.");
        }
    }

    private void SaveRegistry()
    {
        if (_registry == null) return;

        EditorUtility.SetDirty(_registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TilemapMappingWindow] Registry saved: {AssetDatabase.GetAssetPath(_registry)}");
        EditorUtility.DisplayDialog("저장 완료", "레지스트리가 성공적으로 저장되었습니다.", "확인");
    }
}
#endif
