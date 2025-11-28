using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor.Compilation;

public class ScriptCreatorEditorWindow : EditorWindow
{
    private enum CreateScriptType
    {
        UI,
        Data,
        Addon,
        ECS,
        Table,
        Manager,
        Observer,
        Editor,
    }

    private string _objectName = "";
    private string[] _tabTitles = { "스크립트 생성", "스크립트 삭제" };
    private int _selectedTab = 0;
    private CreateScriptType _selectedScriptType = CreateScriptType.UI;
    private List<string> _objectAddPaths = null;
    private Dictionary<CreateScriptType, BaseScriptCreator> _creators = null;
    private static ScriptCreatorEditorWindow _instance;

    [MenuItem("TS/Create Script %&e")] // Ctrl + Shift + E 단축키 설정
    public static void ShowWindow()
    {
        var window = GetWindow<ScriptCreatorEditorWindow>("Unity Script Creator");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    // CreateScriptType 타입 추가 시 수정 필요
    private BaseScriptCreator GetCurrentCreator()
    {
        if (_creators.TryGetValue(_selectedScriptType, out var creator))
            return creator;

        switch (_selectedScriptType)
        {
            case CreateScriptType.UI:
                creator = new UIScriptCreator();
                break;

            case CreateScriptType.Data:
                creator = new DataScriptCreator();
                break;

            case CreateScriptType.Addon:
                creator = new AddonScriptCreator();
                break;

            case CreateScriptType.ECS:
                creator = new ECSScriptCreator();
                break;

            case CreateScriptType.Table:
                creator = new TableScriptCreator();
                break;

            case CreateScriptType.Manager:
                creator = new ManagerScriptCreator();
                break;

            case CreateScriptType.Observer:
                creator = new ObserverScriptCreator();
                break;

            case CreateScriptType.Editor:
                creator = new EditorScriptCreator();
                break;

            default:
                throw new Exception($"Wrong script type: {_selectedScriptType}");
        }

        if (creator != null)
            _creators.Add(_selectedScriptType, creator);

        return creator;
    }

    private void OnEnable()
    {
        _objectAddPaths = new List<string>();
        _creators = new Dictionary<CreateScriptType, BaseScriptCreator>();
        _instance = this;
    }

    private void OnDisable()
    {
        _instance = null;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical("box");
        {
            // 헤더 영역
            EditorGUILayout.LabelField("Unity Script Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            bool change = DrawScriptTypes();

            if (change)
                ResetObjectName();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // 탭 영역
        _selectedTab = GUILayout.Toolbar(_selectedTab, _tabTitles);

        EditorGUILayout.Space();

        // 각 탭에 대한 내용 표시
        EditorGUILayout.BeginVertical("box");
        {
            switch (_selectedTab)
            {
                case 0:
                    DrawScriptGenerator();
                    break;
                case 1:
                    DrawScriptDeletor();
                    break;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawScriptGenerator()
    {
        bool change = DrawInputName();

        if (change)
            ConvertToObjectNameFromPath();

        EditorGUILayout.Space();

        DrawCustomOptionByType();

        EditorGUILayout.Space();

        // 경로 미리보기 섹션
        DrawPathPreviewSection();

        EditorGUILayout.Space();

        // 생성 버튼
        GUI.enabled = !string.IsNullOrEmpty(_objectName);
        if (GUILayout.Button("스크립트 생성", GUILayout.Height(30)))
        {
            Create();
        }
        GUI.enabled = true;
    }

    private void DrawScriptDeletor()
    {
        GetCurrentCreator()?.DrawScriptDeletor();
    }

    private bool DrawScriptTypes()
    {
        EditorGUILayout.LabelField("스크립트 타입", EditorStyles.boldLabel);
        CreateScriptType type = (CreateScriptType) EditorGUILayout.EnumPopup("타입 선택", _selectedScriptType);
        bool change = type != _selectedScriptType;
        _selectedScriptType = type;

        return change;
    }

    private bool DrawInputName()
    {
        // 경로 표시 영역
        if (_objectAddPaths.Count > 0)
        {
            EditorGUILayout.LabelField("추가 경로", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("helpbox");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("경로: ", GUILayout.Width(40));

                    foreach (string path in _objectAddPaths.ToArray())
                    {
                        if (GUILayout.Button(path, "label", GUILayout.ExpandWidth(false)))
                        {
                            _objectAddPaths.Remove(path);
                            break;
                        }

                        if (path != _objectAddPaths[^1])
                            EditorGUILayout.LabelField("/", GUILayout.Width(10));
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("경로 부분을 클릭하면 제거됩니다.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        // 스크립트 이름 입력
        EditorGUILayout.LabelField("스크립트 설정", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        {
            _objectName = EditorGUILayout.TextField("스크립트 이름", _objectName);
            _objectName = _objectName.Trim();
        }
        return EditorGUI.EndChangeCheck();
    }

    private void DrawCustomOptionByType()
    {
        GetCurrentCreator()?.DrawCustomOptions();
    }

    private void DrawPathPreviewSection()
    {
        if (string.IsNullOrEmpty(_objectName))
            return;

        string addPath = null;
        if (_objectAddPaths.Count > 0)
            addPath = $"{string.Join('/', _objectAddPaths)}/";

        GetCurrentCreator()?.DrawPathPreview(addPath, _objectName);
    }

    private void ConvertToObjectNameFromPath()
    {
        if (TryGetSeperatePath(ref _objectName, out string path))
        {
            _objectAddPaths.Add(path);

            if (!string.IsNullOrEmpty(_objectName))
                _objectName = char.ToUpper(_objectName[0]) + _objectName.Substring(1);
        }
    }

    private void ResetObjectName()
    {
        _objectAddPaths.Clear();
        _objectName = string.Empty;
    }

    // 문자열 중 경로가 될 수 있는 부분이 있으면 분리.
    private bool TryGetSeperatePath(ref string name, out string extractedPath)
    {
        extractedPath = null;

        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Length > 0 && name[0] == '/')
        {
            name = name.Substring(1, name.Length - 1);
        }
        else
        {
            for (int i = name.Length - 1; i >= 0; i--)
            {
                if (name[i] == '/')
                {
                    extractedPath = name.Substring(0, i);

                    if (i + 1 < name.Length)
                        name = name.Substring(i + 1, name.Length - (i + 1));
                    else
                        name = string.Empty;

                    return true;
                }
            }
        }

        return false;
    }

    private void Create()
    {
        if (string.IsNullOrEmpty(_objectName))
            return;

        string addPath = null;

        if (_objectAddPaths.Count > 0)
            addPath = $"{string.Join('/', _objectAddPaths)}/";

        GetCurrentCreator()?.Create(addPath, _objectName);

        FullRefresh();
    }

    private void FullRefresh()
    {
        Debug.Log("스크립트 생성 완료 - 컴파일 시작!");

        // 컴파일 완료 콜백 등록
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;

        // 1. 에셋 데이터베이스 새로고침
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        // 2. 스크립트 컴파일 요청
        CompilationPipeline.RequestScriptCompilation();

        // 3. 도메인 리로드
        EditorUtility.RequestScriptReload();

        // 4. 에셋 저장
        AssetDatabase.SaveAssets();

        Debug.Log("컴파일 및 새로고침 요청 완료");
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void AttachScriptToPrefab()
    {
        if (_instance == null)
            return;

        _instance.GetCurrentCreator()?.OnAfterReload();
    }

    static void OnCompilationStarted(object obj)
    {

    }

    static void OnCompilationFinished(object obj)
    {
        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationFinished -= OnCompilationFinished;

        Debug.Log("스크립트 컴파일 완료!");
    }
}
