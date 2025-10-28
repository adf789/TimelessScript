#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Mathematics;

/// <summary>
/// 타일맵 패턴 프리뷰 도구
/// Scene View에서 패턴 배치 미리보기 및 연결 시각화
/// </summary>
public class TilemapPatternPreview : EditorWindow
{
    private TilemapPatternRegistry _registry;
    private Vector2 _scrollPosition;
    private Vector2 _previewOffset = Vector2.zero;
    private float _previewScale = 1f;

    private List<PreviewPattern> _previewPatterns = new List<PreviewPattern>();
    private PreviewPattern _selectedPattern;

    private bool _showGrid = true;
    private bool _showLabels = true;
    private bool _enableSnapping = true;

    private GUIStyle _headerStyle;
    private Color _gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    private Color _connectionColor = new Color(0f, 1f, 0f, 0.5f);
    private Color _selectedColor = new Color(1f, 1f, 0f, 0.8f);

    [MenuItem("TS/Tilemap/Pattern Preview")]
    public static void ShowWindow()
    {
        var window = GetWindow<TilemapPatternPreview>("Pattern Preview");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        FindRegistry();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        InitializeStyles();

        DrawHeader();
        DrawRegistrySelection();
        DrawPreviewControls();
        DrawPatternList();
        DrawPreviewList();
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
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tilemap Pattern Preview", _headerStyle);
        EditorGUILayout.LabelField("Scene View 패턴 배치 미리보기", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "패턴을 선택하고 Scene View에서 배치를 미리볼 수 있습니다. 연결 지점도 시각화됩니다.",
            MessageType.Info
        );
        EditorGUILayout.Space(10);
    }

    private void DrawRegistrySelection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Registry:", GUILayout.Width(70));
        _registry = (TilemapPatternRegistry) EditorGUILayout.ObjectField(
            _registry,
            typeof(TilemapPatternRegistry),
            false
        );

        if (GUILayout.Button("Find", GUILayout.Width(60)))
        {
            FindRegistry();
        }
        EditorGUILayout.EndHorizontal();

        if (_registry == null)
        {
            EditorGUILayout.HelpBox("레지스트리를 선택하거나 'Find' 버튼을 눌러 자동으로 찾으세요.", MessageType.Warning);
        }

        EditorGUILayout.Space(5);
    }

    private void DrawPreviewControls()
    {
        EditorGUILayout.LabelField("Preview Options", EditorStyles.boldLabel);

        _showGrid = EditorGUILayout.Toggle("Show Grid", _showGrid);
        _showLabels = EditorGUILayout.Toggle("Show Labels", _showLabels);
        _enableSnapping = EditorGUILayout.Toggle("Enable Snapping", _enableSnapping);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview Offset:", GUILayout.Width(100));
        _previewOffset = EditorGUILayout.Vector2Field("", _previewOffset);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview Scale:", GUILayout.Width(100));
        _previewScale = EditorGUILayout.Slider(_previewScale, 0.1f, 3f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Reset View"))
        {
            _previewOffset = Vector2.zero;
            _previewScale = 1f;
        }

        EditorGUILayout.Space(10);
    }

    private void DrawPatternList()
    {
        if (_registry == null) return;

        EditorGUILayout.LabelField("Available Patterns", EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));

        foreach (var pattern in _registry.AllPatterns)
        {
            if (pattern == null) continue;

            EditorGUILayout.BeginHorizontal();

            bool isInPreview = _previewPatterns.Exists(p => p.Data == pattern);
            GUI.backgroundColor = isInPreview ? Color.green : Color.white;

            if (GUILayout.Button($"{pattern.PatternID}", GUILayout.Height(25)))
            {
                AddPatternToPreview(pattern);
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(10);
    }

    private void DrawPreviewList()
    {
        EditorGUILayout.LabelField($"Preview Patterns ({_previewPatterns.Count})", EditorStyles.boldLabel);

        if (_previewPatterns.Count == 0)
        {
            EditorGUILayout.HelpBox("프리뷰할 패턴을 선택하세요.", MessageType.Info);
            return;
        }

        foreach (var preview in _previewPatterns)
        {
            EditorGUILayout.BeginHorizontal();

            bool isSelected = _selectedPattern == preview;
            GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

            if (GUILayout.Button($"{preview.Data.PatternID}", GUILayout.Height(20)))
            {
                _selectedPattern = preview;
            }

            GUI.backgroundColor = Color.white;

            preview.GridOffset = EditorGUILayout.Vector2IntField("", preview.GridOffset, GUILayout.Width(120));

            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                RemovePatternFromPreview(preview);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            _previewPatterns.Clear();
            _selectedPattern = null;
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Refresh"))
        {
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_registry == null || _previewPatterns.Count == 0) return;

        Handles.BeginGUI();

        // Scene View 상단에 정보 표시
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label($"Preview: {_previewPatterns.Count} patterns", EditorStyles.whiteLargeLabel);
        if (_selectedPattern != null)
        {
            GUILayout.Label($"Selected: {_selectedPattern.Data.PatternID}", EditorStyles.whiteLabel);
        }
        GUILayout.EndArea();

        Handles.EndGUI();

        // 패턴 그리기
        foreach (var preview in _previewPatterns)
        {
            DrawPatternInScene(preview, preview == _selectedPattern);
        }
    }

    private void DrawPatternInScene(PreviewPattern preview, bool isSelected)
    {
        var data = preview.Data;
        var worldPos = new Vector3(
            _previewOffset.x + preview.GridOffset.x * _registry.GridSize.x * _previewScale,
            _previewOffset.y + preview.GridOffset.y * _registry.GridSize.y * _previewScale,
            0
        );

        var size = new Vector3(_registry.GridSize.x * _previewScale, _registry.GridSize.y * _previewScale, 0);

        // 패턴 경계 그리기 (Shape별 색상)
        if (isSelected)
        {
            Handles.color = _selectedColor;
        }
        else
        {
            Handles.color = Color.white;
        }
        Handles.DrawWireCube(worldPos, size);

        // 그리드 그리기
        if (_showGrid)
        {
            Handles.color = _gridColor;
            Vector3 offset = new Vector3(-_registry.GridSize.x * 0.5f, -_registry.GridSize.y * 0.5f, 0f);
            // 가로선
            for (int y = 0; y <= _registry.GridSize.y; y++)
            {
                Vector3 start = worldPos + new Vector3(0, y * _previewScale, 0);
                Vector3 end = worldPos + new Vector3(size.x, y * _previewScale, 0);

                start += offset;
                end += offset;

                Handles.DrawLine(start, end);
            }
            // 세로선
            for (int x = 0; x <= _registry.GridSize.x; x++)
            {
                Vector3 start = worldPos + new Vector3(x * _previewScale, 0, 0);
                Vector3 end = worldPos + new Vector3(x * _previewScale, size.y, 0);

                start += offset;
                end += offset;

                Handles.DrawLine(start, end);
            }
        }

        // 레이블 그리기
        if (_showLabels)
        {
            Handles.Label(worldPos + size * 0.5f, $"{data.PatternID}\n{_registry.GridSize.x}x{_registry.GridSize.y}");
        }
    }

    private void FindRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:TilemapPatternRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _registry = AssetDatabase.LoadAssetAtPath<TilemapPatternRegistry>(path);
        }
    }

    private void AddPatternToPreview(TilemapPatternData pattern)
    {
        if (_previewPatterns.Exists(p => p.Data == pattern))
        {
            Debug.LogWarning($"패턴 '{pattern.PatternID}'는 이미 프리뷰에 추가되어 있습니다.");
            return;
        }

        var preview = new PreviewPattern
        {
            Data = pattern,
            GridOffset = Vector2Int.zero
        };

        // 자동 배치 (마지막 패턴 오른쪽에)
        if (_previewPatterns.Count > 0)
        {
            var lastPattern = _previewPatterns[_previewPatterns.Count - 1];
            preview.GridOffset = lastPattern.GridOffset + new Vector2Int(1, 0);
        }

        _previewPatterns.Add(preview);
        _selectedPattern = preview;
        SceneView.RepaintAll();
    }

    private void RemovePatternFromPreview(PreviewPattern preview)
    {
        _previewPatterns.Remove(preview);
        if (_selectedPattern == preview)
        {
            _selectedPattern = null;
        }
        SceneView.RepaintAll();
    }

    private class PreviewPattern
    {
        public TilemapPatternData Data;
        public Vector2Int GridOffset;
    }
}
#endif
