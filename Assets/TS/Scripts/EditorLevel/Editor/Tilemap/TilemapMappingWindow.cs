#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TS.LowLevel.Data.Config;

namespace TS.EditorLevel.Editor.Tilemap
{
    /// <summary>
    /// 타일맵 패턴 매핑 관리 도구
    /// SubScene과 패턴 간의 매핑을 시각적으로 관리
    /// </summary>
    public class TilemapMappingWindow : EditorWindow
    {
        private TilemapPatternRegistry _registry;
        private Vector2 _scrollPosition;
        private Vector2 _patternScrollPosition;

        private string _newSubSceneName = "";
        private int _selectedMappingIndex = -1;
        private List<TilemapPatternData> _availablePatterns = new List<TilemapPatternData>();

        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxStyle;

        private bool _showAvailablePatterns = true;
        private string _patternSearchFilter = "";

        [MenuItem("TS/Tilemap/Mapping Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<TilemapMappingWindow>("Mapping Manager");
            window.minSize = new Vector2(600, 500);
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

            // Left panel: SubScene mappings
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));
            DrawSubSceneMappings();
            EditorGUILayout.EndVertical();

            // Right panel: Available patterns
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
            DrawAvailablePatterns();
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
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Tilemap Mapping Manager", _headerStyle);
            EditorGUILayout.LabelField("SubScene ↔ Pattern 매핑 관리", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "SubScene별 초기 로드 패턴을 설정합니다. 좌측에서 SubScene을 선택하고 우측에서 패턴을 추가하세요.",
                MessageType.Info
            );
            EditorGUILayout.Space(10);
        }

        private void DrawRegistrySelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Registry:", GUILayout.Width(70));

            var newRegistry = (TilemapPatternRegistry)EditorGUILayout.ObjectField(
                _registry,
                typeof(TilemapPatternRegistry),
                false
            );

            if (newRegistry != _registry)
            {
                _registry = newRegistry;
                _selectedMappingIndex = -1;
                RefreshAvailablePatterns();
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
            }

            EditorGUILayout.Space(5);
        }

        private void DrawSubSceneMappings()
        {
            EditorGUILayout.LabelField("SubScene Mappings", _subHeaderStyle);
            EditorGUILayout.Space(5);

            // Add new SubScene mapping
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("새 SubScene 추가", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newSubSceneName = EditorGUILayout.TextField(_newSubSceneName);

            GUI.enabled = !string.IsNullOrEmpty(_newSubSceneName) &&
                          !_registry.InitialMappings.Any(m => m.SubSceneName == _newSubSceneName);

            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                AddNewSubSceneMapping();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // List of existing mappings
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _registry.InitialMappings.Count; i++)
            {
                var mapping = _registry.InitialMappings[i];
                bool isSelected = i == _selectedMappingIndex;

                GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f) : Color.white;

                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.BeginHorizontal();

                // SubScene name
                if (GUILayout.Button($"{mapping.SubSceneName} ({mapping.InitialPatterns.Count})",
                    GUILayout.Height(30)))
                {
                    _selectedMappingIndex = i;
                }

                // Remove button
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("×", GUILayout.Width(30), GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog(
                        "SubScene 매핑 삭제",
                        $"'{mapping.SubSceneName}' 매핑을 삭제하시겠습니까?",
                        "삭제", "취소"))
                    {
                        RemoveSubSceneMapping(i);
                        break;
                    }
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // Show patterns if selected
                if (isSelected)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("패턴 목록:", EditorStyles.miniLabel);

                    for (int j = 0; j < mapping.InitialPatterns.Count; j++)
                    {
                        var pattern = mapping.InitialPatterns[j];
                        if (pattern == null)
                        {
                            EditorGUILayout.LabelField($"  {j + 1}. [Missing Pattern]", EditorStyles.miniLabel);
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  {j + 1}. {pattern.PatternID} ({pattern.Type})",
                            EditorStyles.miniLabel, GUILayout.Height(18));

                        // Move up
                        GUI.enabled = j > 0;
                        if (GUILayout.Button("↑", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            MovePattern(i, j, j - 1);
                        }
                        GUI.enabled = true;

                        // Move down
                        GUI.enabled = j < mapping.InitialPatterns.Count - 1;
                        if (GUILayout.Button("↓", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            MovePattern(i, j, j + 1);
                        }
                        GUI.enabled = true;

                        // Remove
                        if (GUILayout.Button("−", GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            RemovePatternFromMapping(i, j);
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if (mapping.InitialPatterns.Count == 0)
                    {
                        EditorGUILayout.LabelField("  (패턴 없음)", EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAvailablePatterns()
        {
            EditorGUILayout.LabelField("Available Patterns", _subHeaderStyle);
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

            if (_selectedMappingIndex < 0)
            {
                EditorGUILayout.HelpBox("좌측에서 SubScene을 선택하세요.", MessageType.Info);
                return;
            }

            // Refresh button
            if (GUILayout.Button("Refresh Pattern List"))
            {
                RefreshAvailablePatterns();
            }

            EditorGUILayout.Space(5);

            // Pattern list
            _patternScrollPosition = EditorGUILayout.BeginScrollView(_patternScrollPosition);

            var filteredPatterns = string.IsNullOrEmpty(_patternSearchFilter)
                ? _availablePatterns
                : _availablePatterns.Where(p => p.PatternID.ToLower().Contains(_patternSearchFilter.ToLower()) ||
                                                 p.Type.ToString().ToLower().Contains(_patternSearchFilter.ToLower()))
                                    .ToList();

            foreach (var pattern in filteredPatterns)
            {
                EditorGUILayout.BeginHorizontal(_boxStyle);

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(pattern.PatternID, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Type: {pattern.Type} | Size: {pattern.GridSize.x}x{pattern.GridSize.y}",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Add →", GUILayout.Width(80), GUILayout.Height(35)))
                {
                    AddPatternToMapping(_selectedMappingIndex, pattern);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            if (filteredPatterns.Count == 0)
            {
                EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void FindRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:TilemapPatternRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _registry = AssetDatabase.LoadAssetAtPath<TilemapPatternRegistry>(path);
                RefreshAvailablePatterns();
                Debug.Log($"[TilemapMappingWindow] Found registry: {path}");
            }
            else
            {
                Debug.LogWarning("[TilemapMappingWindow] No TilemapPatternRegistry found in project.");
            }
        }

        private void RefreshAvailablePatterns()
        {
            if (_registry == null) return;

            _availablePatterns.Clear();
            _availablePatterns.AddRange(_registry.AllPatterns.Where(p => p != null));
        }

        private void AddNewSubSceneMapping()
        {
            if (string.IsNullOrEmpty(_newSubSceneName)) return;

            var newMapping = new SubScenePatternMapping
            {
                SubSceneName = _newSubSceneName,
                InitialPatterns = new List<TilemapPatternData>()
            };

            _registry.InitialMappings.Add(newMapping);
            _selectedMappingIndex = _registry.InitialMappings.Count - 1;
            _newSubSceneName = "";

            EditorUtility.SetDirty(_registry);
            Debug.Log($"[TilemapMappingWindow] Added new SubScene mapping: {newMapping.SubSceneName}");
        }

        private void RemoveSubSceneMapping(int index)
        {
            if (index < 0 || index >= _registry.InitialMappings.Count) return;

            string subSceneName = _registry.InitialMappings[index].SubSceneName;
            _registry.InitialMappings.RemoveAt(index);

            if (_selectedMappingIndex == index)
            {
                _selectedMappingIndex = -1;
            }
            else if (_selectedMappingIndex > index)
            {
                _selectedMappingIndex--;
            }

            EditorUtility.SetDirty(_registry);
            Debug.Log($"[TilemapMappingWindow] Removed SubScene mapping: {subSceneName}");
        }

        private void AddPatternToMapping(int mappingIndex, TilemapPatternData pattern)
        {
            if (mappingIndex < 0 || mappingIndex >= _registry.InitialMappings.Count) return;
            if (pattern == null) return;

            var mapping = _registry.InitialMappings[mappingIndex];

            // Check if pattern already exists
            if (mapping.InitialPatterns.Contains(pattern))
            {
                EditorUtility.DisplayDialog(
                    "중복된 패턴",
                    $"패턴 '{pattern.PatternID}'는 이미 '{mapping.SubSceneName}'에 추가되어 있습니다.",
                    "확인"
                );
                return;
            }

            mapping.InitialPatterns.Add(pattern);
            EditorUtility.SetDirty(_registry);
            Debug.Log($"[TilemapMappingWindow] Added pattern '{pattern.PatternID}' to '{mapping.SubSceneName}'");
        }

        private void RemovePatternFromMapping(int mappingIndex, int patternIndex)
        {
            if (mappingIndex < 0 || mappingIndex >= _registry.InitialMappings.Count) return;

            var mapping = _registry.InitialMappings[mappingIndex];
            if (patternIndex < 0 || patternIndex >= mapping.InitialPatterns.Count) return;

            var pattern = mapping.InitialPatterns[patternIndex];
            mapping.InitialPatterns.RemoveAt(patternIndex);

            EditorUtility.SetDirty(_registry);
            Debug.Log($"[TilemapMappingWindow] Removed pattern '{pattern?.PatternID}' from '{mapping.SubSceneName}'");
        }

        private void MovePattern(int mappingIndex, int fromIndex, int toIndex)
        {
            if (mappingIndex < 0 || mappingIndex >= _registry.InitialMappings.Count) return;

            var mapping = _registry.InitialMappings[mappingIndex];
            if (fromIndex < 0 || fromIndex >= mapping.InitialPatterns.Count) return;
            if (toIndex < 0 || toIndex >= mapping.InitialPatterns.Count) return;

            var pattern = mapping.InitialPatterns[fromIndex];
            mapping.InitialPatterns.RemoveAt(fromIndex);
            mapping.InitialPatterns.Insert(toIndex, pattern);

            EditorUtility.SetDirty(_registry);
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
}
#endif
