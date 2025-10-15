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
        ECS,
        Table,
        Manager,
        Observer,
        Editor,
    }

    private string objectName = "";
    private string[] tabTitles = { "스크립트 생성", "스크립트 삭제" };
    private int selectedTab = 0;
    private CreateScriptType selectedScriptType = CreateScriptType.UI;
    private List<string> objectAddPaths = null;
    private Dictionary<CreateScriptType, BaseScriptCreator> creators = null;
    private static ScriptCreatorEditorWindow instance;

    [MenuItem("TS/Create Script %e")] // Ctrl + E 단축키 설정
    public static void ShowWindow()
    {
        var window = GetWindow<ScriptCreatorEditorWindow>("Unity Script Creator");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    // CreateScriptType 타입 추가 시 수정 필요
    private BaseScriptCreator GetCurrentCreator()
    {
        if (creators.TryGetValue(selectedScriptType, out var creator))
            return creator;

        switch (selectedScriptType)
        {
            case CreateScriptType.UI:
                creator = new UIScriptCreator();
                break;

            case CreateScriptType.Data:
                creator = new DataScriptCreator();
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
                throw new Exception($"Wrong script type: {selectedScriptType}");
        }

        if (creator != null)
            creators.Add(selectedScriptType, creator);

        return creator;
    }

    private void OnEnable()
    {
        objectAddPaths = new List<string>();
        creators = new Dictionary<CreateScriptType, BaseScriptCreator>();
        instance = this;
    }

    private void OnDisable()
    {
        instance = null;
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
        selectedTab = GUILayout.Toolbar(selectedTab, tabTitles);

        EditorGUILayout.Space();

        // 각 탭에 대한 내용 표시
        EditorGUILayout.BeginVertical("box");
        {
            switch (selectedTab)
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
        GUI.enabled = !string.IsNullOrEmpty(objectName);
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
        CreateScriptType type = (CreateScriptType) EditorGUILayout.EnumPopup("타입 선택", selectedScriptType);
        bool change = type != selectedScriptType;
        selectedScriptType = type;

        return change;
    }

    private bool DrawInputName()
    {
        // 경로 표시 영역
        if (objectAddPaths.Count > 0)
        {
            EditorGUILayout.LabelField("추가 경로", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("helpbox");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("경로: ", GUILayout.Width(40));

                    foreach (string path in objectAddPaths.ToArray())
                    {
                        if (GUILayout.Button(path, "label", GUILayout.ExpandWidth(false)))
                        {
                            objectAddPaths.Remove(path);
                            break;
                        }

                        if (path != objectAddPaths[^1])
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
            objectName = EditorGUILayout.TextField("스크립트 이름", objectName);
            objectName = objectName.Trim();
        }
        return EditorGUI.EndChangeCheck();
    }

    private void DrawCustomOptionByType()
    {
        GetCurrentCreator()?.DrawCustomOptions();
    }

    private void DrawPathPreviewSection()
    {
        if (string.IsNullOrEmpty(objectName))
            return;

        string addPath = null;
        if (objectAddPaths.Count > 0)
            addPath = $"{string.Join('/', objectAddPaths)}/";

        GetCurrentCreator()?.DrawPathPreview(addPath, objectName);
    }

    private void ConvertToObjectNameFromPath()
    {
        if (TryGetSeperatePath(ref objectName, out string path))
        {
            objectAddPaths.Add(path);

            if (!string.IsNullOrEmpty(objectName))
                objectName = char.ToUpper(objectName[0]) + objectName.Substring(1);
        }
    }

    private void ResetObjectName()
    {
        objectAddPaths.Clear();
        objectName = string.Empty;
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
        if (string.IsNullOrEmpty(objectName))
            return;

        string addPath = null;

        if (objectAddPaths.Count > 0)
            addPath = $"{string.Join('/', objectAddPaths)}/";

        GetCurrentCreator()?.Create(addPath, objectName);

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
        if (instance == null)
            return;

        instance.GetCurrentCreator()?.OnAfterReload();
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
