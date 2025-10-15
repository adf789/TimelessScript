#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TS.LowLevel.Data.Config;

namespace TS.EditorLevel.Editor.Tilemap
{
    /// <summary>
    /// 타일맵 패턴 검증 도구
    /// 패턴 데이터의 무결성을 검사하고 문제를 보고
    /// </summary>
    public class TilemapPatternValidator : EditorWindow
    {
        private TilemapPatternRegistry _registry;
        private Vector2 _scrollPosition;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo = true;

        private List<ValidationResult> _validationResults = new List<ValidationResult>();
        private GUIStyle _headerStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _infoStyle;

        [MenuItem("TS/Tilemap/Pattern Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<TilemapPatternValidator>("Pattern Validator");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 자동으로 레지스트리 찾기
            FindRegistry();
        }

        private void OnGUI()
        {
            InitializeStyles();

            DrawHeader();
            DrawRegistrySelection();
            DrawValidationControls();
            DrawValidationResults();
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

                _errorStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(1f, 0.3f, 0.3f) }
                };

                _warningStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0f) }
                };

                _infoStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
                };
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Tilemap Pattern Validator", _headerStyle);
            EditorGUILayout.LabelField("패턴 데이터 무결성 검사 도구", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "패턴 ID 중복, Addressable 참조, Connection 유효성 등을 자동으로 검증합니다.",
                MessageType.Info
            );
            EditorGUILayout.Space(10);
        }

        private void DrawRegistrySelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Registry:", GUILayout.Width(70));
            _registry = (TilemapPatternRegistry)EditorGUILayout.ObjectField(
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

        private void DrawValidationControls()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _registry != null;
            if (GUILayout.Button("🔍 Validate All", GUILayout.Height(30)))
            {
                ValidateAll();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(30)))
            {
                _validationResults.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 필터 옵션
            EditorGUILayout.BeginHorizontal();
            _showErrors = GUILayout.Toggle(_showErrors, "❌ Errors", GUILayout.Width(80));
            _showWarnings = GUILayout.Toggle(_showWarnings, "⚠️ Warnings", GUILayout.Width(100));
            _showInfo = GUILayout.Toggle(_showInfo, "ℹ️ Info", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        private void DrawValidationResults()
        {
            if (_validationResults.Count == 0)
            {
                EditorGUILayout.HelpBox("검증 결과가 없습니다. 'Validate All' 버튼을 눌러 검증을 시작하세요.", MessageType.Info);
                return;
            }

            // 통계 표시
            int errorCount = _validationResults.Count(r => r.Type == ValidationResultType.Error);
            int warningCount = _validationResults.Count(r => r.Type == ValidationResultType.Warning);
            int infoCount = _validationResults.Count(r => r.Type == ValidationResultType.Info);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Total: {_validationResults.Count}", GUILayout.Width(100));
            if (errorCount > 0)
                EditorGUILayout.LabelField($"❌ {errorCount}", GUILayout.Width(60));
            if (warningCount > 0)
                EditorGUILayout.LabelField($"⚠️ {warningCount}", GUILayout.Width(60));
            if (infoCount > 0)
                EditorGUILayout.LabelField($"ℹ️ {infoCount}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 결과 목록
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var result in _validationResults)
            {
                if (!ShouldShowResult(result)) continue;

                GUIStyle style = GetStyleForType(result.Type);
                string icon = GetIconForType(result.Type);

                EditorGUILayout.BeginVertical(style);
                EditorGUILayout.LabelField($"{icon} {result.Message}", EditorStyles.wordWrappedLabel);

                if (!string.IsNullOrEmpty(result.Details))
                {
                    EditorGUILayout.LabelField(result.Details, EditorStyles.miniLabel);
                }

                if (result.RelatedObject != null)
                {
                    EditorGUILayout.ObjectField("Related:", result.RelatedObject, result.RelatedObject.GetType(), false);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool ShouldShowResult(ValidationResult result)
        {
            return result.Type switch
            {
                ValidationResultType.Error => _showErrors,
                ValidationResultType.Warning => _showWarnings,
                ValidationResultType.Info => _showInfo,
                _ => true
            };
        }

        private GUIStyle GetStyleForType(ValidationResultType type)
        {
            return type switch
            {
                ValidationResultType.Error => _errorStyle,
                ValidationResultType.Warning => _warningStyle,
                ValidationResultType.Info => _infoStyle,
                _ => EditorStyles.helpBox
            };
        }

        private string GetIconForType(ValidationResultType type)
        {
            return type switch
            {
                ValidationResultType.Error => "❌",
                ValidationResultType.Warning => "⚠️",
                ValidationResultType.Info => "ℹ️",
                _ => "•"
            };
        }

        private void FindRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:TilemapPatternRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _registry = AssetDatabase.LoadAssetAtPath<TilemapPatternRegistry>(path);
                Debug.Log($"[TilemapPatternValidator] Found registry: {path}");
            }
            else
            {
                Debug.LogWarning("[TilemapPatternValidator] No TilemapPatternRegistry found in project.");
            }
        }

        private void ValidateAll()
        {
            _validationResults.Clear();

            if (_registry == null)
            {
                AddResult(ValidationResultType.Error, "레지스트리가 선택되지 않았습니다.", null);
                return;
            }

            AddResult(ValidationResultType.Info, $"검증 시작: {_registry.name}", _registry);

            ValidateDuplicateIDs();
            ValidateAddressableReferences();
            ValidateConnections();
            ValidateSubSceneMappings();
            ValidatePatternCategories();

            int errorCount = _validationResults.Count(r => r.Type == ValidationResultType.Error);
            int warningCount = _validationResults.Count(r => r.Type == ValidationResultType.Warning);

            if (errorCount == 0 && warningCount == 0)
            {
                AddResult(ValidationResultType.Info, "✅ 모든 검증 통과!", null);
            }
            else
            {
                AddResult(
                    ValidationResultType.Warning,
                    $"검증 완료: {errorCount} errors, {warningCount} warnings",
                    null
                );
            }

            Debug.Log($"[TilemapPatternValidator] Validation complete: {_validationResults.Count} results");
        }

        private void ValidateDuplicateIDs()
        {
            var idGroups = _registry.AllPatterns
                .Where(p => p != null)
                .GroupBy(p => p.PatternID)
                .Where(g => g.Count() > 1);

            foreach (var group in idGroups)
            {
                AddResult(
                    ValidationResultType.Error,
                    $"중복된 PatternID: '{group.Key}'",
                    $"{group.Count()}개의 패턴이 같은 ID를 사용하고 있습니다.",
                    group.First()
                );
            }
        }

        private void ValidateAddressableReferences()
        {
            foreach (var pattern in _registry.AllPatterns)
            {
                if (pattern == null) continue;

                if (pattern.TilemapPrefab == null || !pattern.TilemapPrefab.RuntimeKeyIsValid())
                {
                    AddResult(
                        ValidationResultType.Warning,
                        $"패턴 '{pattern.PatternID}': Addressable 참조가 설정되지 않았습니다.",
                        "TilemapPrefab이 null이거나 유효하지 않은 Addressable 키입니다.",
                        pattern
                    );
                }
            }
        }

        private void ValidateConnections()
        {
            foreach (var pattern in _registry.AllPatterns)
            {
                if (pattern == null) continue;

                foreach (var connection in pattern.Connections)
                {
                    foreach (var nextPatternID in connection.ValidNextPatterns)
                    {
                        if (string.IsNullOrEmpty(nextPatternID))
                        {
                            AddResult(
                                ValidationResultType.Warning,
                                $"패턴 '{pattern.PatternID}': 빈 연결 패턴 ID",
                                $"{connection.Direction} 방향의 ValidNextPatterns에 빈 문자열이 있습니다.",
                                pattern
                            );
                            continue;
                        }

                        if (!_registry.AllPatterns.Any(p => p != null && p.PatternID == nextPatternID))
                        {
                            AddResult(
                                ValidationResultType.Error,
                                $"패턴 '{pattern.PatternID}': 존재하지 않는 연결 패턴 '{nextPatternID}'",
                                $"{connection.Direction} 방향으로 연결된 패턴이 레지스트리에 없습니다.",
                                pattern
                            );
                        }
                    }
                }
            }
        }

        private void ValidateSubSceneMappings()
        {
            foreach (var mapping in _registry.InitialMappings)
            {
                if (string.IsNullOrEmpty(mapping.SubSceneName))
                {
                    AddResult(
                        ValidationResultType.Warning,
                        "SubScene 매핑: 빈 SubSceneName",
                        "SubScene 이름이 비어있는 매핑이 있습니다.",
                        null
                    );
                    continue;
                }

                foreach (var pattern in mapping.InitialPatterns)
                {
                    if (pattern == null)
                    {
                        AddResult(
                            ValidationResultType.Warning,
                            $"SubScene '{mapping.SubSceneName}': null 패턴 참조",
                            "InitialPatterns에 null 참조가 있습니다.",
                            null
                        );
                    }
                }

                if (mapping.InitialPatterns.Count == 0)
                {
                    AddResult(
                        ValidationResultType.Info,
                        $"SubScene '{mapping.SubSceneName}': 초기 패턴이 없습니다.",
                        "이 SubScene은 타일맵 패턴을 로드하지 않습니다.",
                        null
                    );
                }
            }
        }

        private void ValidatePatternCategories()
        {
            foreach (var category in _registry.Categories)
            {
                if (string.IsNullOrEmpty(category.CategoryName))
                {
                    AddResult(
                        ValidationResultType.Warning,
                        "카테고리: 빈 이름",
                        "이름이 비어있는 카테고리가 있습니다.",
                        null
                    );
                }

                foreach (var pattern in category.Patterns)
                {
                    if (pattern == null)
                    {
                        AddResult(
                            ValidationResultType.Warning,
                            $"카테고리 '{category.CategoryName}': null 패턴 참조",
                            null,
                            null
                        );
                    }
                }
            }
        }

        private void AddResult(ValidationResultType type, string message, Object relatedObject)
        {
            AddResult(type, message, null, relatedObject);
        }

        private void AddResult(ValidationResultType type, string message, string details, Object relatedObject)
        {
            _validationResults.Add(new ValidationResult
            {
                Type = type,
                Message = message,
                Details = details,
                RelatedObject = relatedObject
            });
        }

        private class ValidationResult
        {
            public ValidationResultType Type;
            public string Message;
            public string Details;
            public Object RelatedObject;
        }

        private enum ValidationResultType
        {
            Error,
            Warning,
            Info
        }
    }
}
#endif
