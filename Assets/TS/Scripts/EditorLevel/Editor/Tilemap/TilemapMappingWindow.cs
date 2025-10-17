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
    private PatternDirection _newPortDirection = PatternDirection.Right;
    private int _newPortPosition = 0;

    private GUIStyle _headerStyle;
    private GUIStyle _subHeaderStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _selectedBoxStyle;

    [MenuItem("TS/Tilemap/Pattern Editor")]
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
            if (!string.IsNullOrEmpty(pattern.SubSceneName))
            {
                EditorGUILayout.LabelField($"SubScene: {pattern.SubSceneName}", EditorStyles.miniLabel);
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
        EditorGUILayout.LabelField($"Display Name: {pattern.DisplayName}");
        EditorGUILayout.LabelField($"Grid Size: {pattern.GridSize.x} x {pattern.GridSize.y}");

        EditorGUILayout.Space(10);

        // SubScene setting
        EditorGUILayout.LabelField("SubScene Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        var newSubSceneName = EditorGUILayout.TextField("SubScene Name", pattern.SubSceneName);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(pattern, "Change SubScene Name");
            pattern.SubSceneName = newSubSceneName;
            EditorUtility.SetDirty(pattern);
        }

        EditorGUILayout.Space(10);

        // Port Management Section
        DrawPortManagement(pattern);

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

    /// <summary>
    /// Port 관리 UI 그리기 (추가/활성화/삭제)
    /// </summary>
    private void DrawPortManagement(TilemapPatternData pattern)
    {
        EditorGUILayout.LabelField("Port Management", EditorStyles.boldLabel);

        // 현재 Port 목록
        if (pattern.Connections.Count > 0)
        {
            EditorGUILayout.LabelField($"현재 Port: {pattern.Connections.Count}개", EditorStyles.miniLabel);

            for (int i = 0; i < pattern.Connections.Count; i++)
            {
                var conn = pattern.Connections[i];

                EditorGUILayout.BeginHorizontal(_boxStyle);

                // Port 정보
                string icon = GetConnectionIcon(conn);
                EditorGUILayout.LabelField($"{icon} {GetDirectionDisplayName(conn.Direction)}", GUILayout.Width(100));

                // 삭제 버튼
                if (GUILayout.Button("삭제", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Port 삭제",
                        $"{GetDirectionDisplayName(conn.Direction)} Port를 삭제하시겠습니까?\n연결된 패턴도 함께 연결 해제됩니다.",
                        "삭제", "취소"))
                    {
                        Undo.RecordObject(pattern, "Delete Port");
                        pattern.Connections.RemoveAt(i);
                        EditorUtility.SetDirty(pattern);
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Port가 없습니다. 아래에서 새 Port를 추가하세요.", MessageType.Info);
        }

        EditorGUILayout.Space(5);

        // Port 추가 섹션
        _showAddPortSection = EditorGUILayout.Foldout(_showAddPortSection, "➕ 새 Port 추가", true);

        if (_showAddPortSection)
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            _newPortDirection = (PatternDirection) EditorGUILayout.EnumPopup("방향", _newPortDirection);
            _newPortPosition = EditorGUILayout.IntField("Position", _newPortPosition);

            // 위치 제한 지정
            int maxPosition = 0;
            if (_newPortDirection == PatternDirection.Left
            || _newPortDirection == PatternDirection.Right)
                maxPosition = (int) pattern.GridSize.y - 1;
            else
                maxPosition = (int) pattern.GridSize.x - 1;

            _newPortPosition = Mathf.Clamp(_newPortPosition, 0, maxPosition);

            // 중복 검사
            bool isDuplicate = pattern.Connections.Any(c => c.Direction == _newPortDirection);
            if (isDuplicate)
            {
                EditorGUILayout.HelpBox($"⚠️ {GetDirectionDisplayName(_newPortDirection)} 방향 Port가 이미 존재합니다.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(isDuplicate);
            if (GUILayout.Button("Port 추가", GUILayout.Height(30)))
            {
                AddNewPort(pattern, _newPortDirection, _newPortPosition);

                // 초기화
                _newPortDirection = PatternDirection.Right;
                _newPortPosition = 0;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 새 Port 추가
    /// </summary>
    private void AddNewPort(TilemapPatternData pattern, PatternDirection direction, int pos)
    {
        Undo.RecordObject(pattern, "Add New Port");

        var newConnection = new ConnectionPoint
        {
            Direction = direction,
            Position = pos,
        };

        pattern.Connections.Add(newConnection);
        EditorUtility.SetDirty(pattern);

        Debug.Log($"[TilemapMappingWindow] Port 추가: {pattern.PatternID} - {GetDirectionDisplayName(direction)}");
    }

    /// <summary>
    /// Port 양방향 연결
    /// </summary>
    private void ConnectPortBidirectional(TilemapPatternData sourcePattern, int sourcePortIndex, TilemapPatternData targetPattern)
    {
        var sourceConn = sourcePattern.Connections[sourcePortIndex];
        var oppositeDirection = GetOppositeDirection(sourceConn.Direction);

        // Source → Target 연결
        Undo.RecordObject(sourcePattern, "Connect Port Bidirectional");
        var updatedSourceConn = sourceConn;
        sourcePattern.Connections[sourcePortIndex] = updatedSourceConn;
        EditorUtility.SetDirty(sourcePattern);

        // Target에서 반대 방향 Port 찾기 또는 생성
        int targetPortIndex = targetPattern.Connections.FindIndex(c => c.Direction == oppositeDirection);

        if (targetPortIndex >= 0)
        {
            // 기존 Port가 있으면 활성화 및 연결
            Undo.RecordObject(targetPattern, "Connect Port Bidirectional");
            var targetConn = targetPattern.Connections[targetPortIndex];
            targetPattern.Connections[targetPortIndex] = targetConn;
            EditorUtility.SetDirty(targetPattern);

            Debug.Log($"[TilemapMappingWindow] 양방향 연결 완료: {sourcePattern.PatternID}.{sourceConn.Direction} ↔ {targetPattern.PatternID}.{oppositeDirection}");
        }
        else
        {
            // 반대 Port가 없으면 자동 생성
            Undo.RecordObject(targetPattern, "Create Opposite Port");

            var newTargetConn = new ConnectionPoint
            {
                Direction = oppositeDirection,
                Position = 0, // 기본값
            };

            targetPattern.Connections.Add(newTargetConn);
            EditorUtility.SetDirty(targetPattern);

            Debug.Log($"[TilemapMappingWindow] 반대 Port 자동 생성 및 연결: {targetPattern.PatternID}.{oppositeDirection} → {sourcePattern.PatternID}");
        }
    }

    /// <summary>
    /// 반대 방향 Port 계산
    /// TopLeft ↔ BottomRight, TopRight ↔ BottomLeft, Left ↔ Right
    /// </summary>
    private PatternDirection GetOppositeDirection(PatternDirection direction)
    {
        return direction switch
        {
            PatternDirection.TopLeft => PatternDirection.BottomRight,
            PatternDirection.TopRight => PatternDirection.BottomLeft,
            PatternDirection.Left => PatternDirection.Right,
            PatternDirection.Right => PatternDirection.Left,
            PatternDirection.BottomLeft => PatternDirection.TopRight,
            PatternDirection.BottomRight => PatternDirection.TopLeft,
            _ => direction
        };
    }

    /// <summary>
    /// 특정 Port 방향과 연결 가능한 패턴 목록 반환
    /// </summary>
    private List<TilemapPatternData> GetCompatiblePatterns(TilemapPatternData currentPattern, PatternDirection portDirection)
    {
        var oppositeDirection = GetOppositeDirection(portDirection);
        var compatiblePatterns = new List<TilemapPatternData>();

        foreach (var pattern in _registry.AllPatterns)
        {
            if (pattern == null || pattern == currentPattern) continue;

            // 반대 방향 Port를 가진 패턴만 연결 가능
            if (pattern.HasActiveConnection(oppositeDirection))
            {
                compatiblePatterns.Add(pattern);
            }
        }

        return compatiblePatterns;
    }

    /// <summary>
    /// Port 연결 상태 표시 아이콘
    /// </summary>
    private string GetConnectionIcon(ConnectionPoint connection)
    {
        return "⚪"; // 활성, 미연결
    }

    /// <summary>
    /// Port 방향 이름을 한글로 변환
    /// </summary>
    private string GetDirectionDisplayName(PatternDirection direction)
    {
        return direction switch
        {
            PatternDirection.TopLeft => "좌상단",
            PatternDirection.TopRight => "우상단",
            PatternDirection.Left => "좌측",
            PatternDirection.Right => "우측",
            PatternDirection.BottomLeft => "좌하단",
            PatternDirection.BottomRight => "우하단",
            _ => direction.ToString()
        };
    }
}
#endif
