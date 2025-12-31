#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class EditorWindowNavigator : EditorWindow
{
    private Dictionary<string, List<WindowInfo>> _windowsByCategory;
    private Vector2 _scrollPosition;
    private string _searchFilter = "";
    private bool _showAllCategories = true;
    private Dictionary<string, bool> _categoryFoldouts;

    private const float BUTTON_HEIGHT = 30f;
    private const float CATEGORY_SPACING = 10f;

    [MenuItem("TS/Window Navigator %&w")]
    public static void OpenNavigator()
    {
        var window = GetWindow<EditorWindowNavigator>("Window Navigator");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshWindowList();
    }

    private void RefreshWindowList()
    {
        _windowsByCategory = new Dictionary<string, List<WindowInfo>>();
        _categoryFoldouts = new Dictionary<string, bool>();

        // 모든 어셈블리에서 EditorWindow를 상속받은 타입 탐색
        var editorWindowType = typeof(EditorWindow);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // EditorWindow를 상속받고, CustomEditorWindowAttribute가 있는 타입만 필터링
                    if (editorWindowType.IsAssignableFrom(type) &&
                        !type.IsAbstract &&
                        type != typeof(EditorWindowNavigator))
                    {
                        var attribute = type.GetCustomAttribute<CustomEditorWindowAttribute>();
                        if (attribute != null)
                        {
                            var windowInfo = new WindowInfo
                            {
                                Type = type,
                                DisplayName = attribute.DisplayName,
                                Category = attribute.Category,
                                Order = attribute.Order,
                                Description = attribute.Description
                            };

                            if (!_windowsByCategory.ContainsKey(attribute.Category))
                            {
                                _windowsByCategory[attribute.Category] = new List<WindowInfo>();
                                _categoryFoldouts[attribute.Category] = true;
                            }

                            _windowsByCategory[attribute.Category].Add(windowInfo);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ReflectionTypeLoadException 등 무시
                continue;
            }
        }

        // 각 카테고리 내부 정렬
        foreach (var category in _windowsByCategory.Keys.ToList())
        {
            _windowsByCategory[category] = _windowsByCategory[category]
                .OrderBy(w => w.Order)
                .ThenBy(w => w.DisplayName)
                .ToList();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.Space(5);

        // 검색 필터
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        _searchFilter = EditorGUILayout.TextField(_searchFilter);
        if (GUILayout.Button("×", GUILayout.Width(20)))
        {
            _searchFilter = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 윈도우 목록
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        if (_windowsByCategory.Count == 0)
        {
            EditorGUILayout.HelpBox("No custom editor windows found.\nAdd [CustomEditorWindow(\"Name\", \"Category\")] attribute to your EditorWindow classes.", MessageType.Info);
        }
        else
        {
            DrawWindowList();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Window Navigator", EditorStyles.toolbarButton);

        GUILayout.FlexibleSpace();

        // 전체 카테고리 펼치기/접기
        string toggleText = _showAllCategories ? "Collapse All" : "Expand All";
        if (GUILayout.Button(toggleText, EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            _showAllCategories = !_showAllCategories;
            foreach (var category in _categoryFoldouts.Keys.ToList())
            {
                _categoryFoldouts[category] = _showAllCategories;
            }
        }

        // 새로고침 버튼
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshWindowList();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWindowList()
    {
        var categories = _windowsByCategory.Keys.OrderBy(c => c).ToList();

        foreach (var category in categories)
        {
            var windows = _windowsByCategory[category];

            // 검색 필터 적용
            var filteredWindows = string.IsNullOrEmpty(_searchFilter)
                ? windows
                : windows.Where(w =>
                    w.DisplayName.ToLower().Contains(_searchFilter.ToLower()) ||
                    w.Category.ToLower().Contains(_searchFilter.ToLower()) ||
                    (!string.IsNullOrEmpty(w.Description) && w.Description.ToLower().Contains(_searchFilter.ToLower()))
                ).ToList();

            if (filteredWindows.Count == 0)
                continue;

            // 카테고리 헤더
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            _categoryFoldouts[category] = EditorGUILayout.Foldout(
                _categoryFoldouts[category],
                $"{category} ({filteredWindows.Count})",
                true,
                EditorStyles.foldoutHeader
            );

            if (GUILayout.Button("Open All", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                foreach (var window in filteredWindows)
                {
                    OpenWindow(window.Type);
                }
            }

            EditorGUILayout.EndHorizontal();

            // 윈도우 버튼들
            if (_categoryFoldouts[category])
            {
                EditorGUILayout.Space(5);

                foreach (var window in filteredWindows)
                {
                    DrawWindowButton(window);
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(CATEGORY_SPACING);
        }
    }

    private void DrawWindowButton(WindowInfo windowInfo)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        // 윈도우 이름과 설명
        EditorGUILayout.BeginVertical();

        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField(windowInfo.DisplayName, nameStyle);

        if (!string.IsNullOrEmpty(windowInfo.Description))
        {
            GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel);
            descStyle.wordWrap = true;
            EditorGUILayout.LabelField(windowInfo.Description, descStyle);
        }

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Open 버튼
        if (GUILayout.Button("Open", GUILayout.Width(60), GUILayout.Height(BUTTON_HEIGHT)))
        {
            OpenWindow(windowInfo.Type);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void OpenWindow(Type windowType)
    {
        try
        {
            var window = GetWindow(windowType);
            window.Show();
            window.Focus();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to open window {windowType.Name}: {e.Message}");
        }
    }

    private class WindowInfo
    {
        public Type Type { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public int Order { get; set; }
        public string Description { get; set; }
    }
}
#endif
