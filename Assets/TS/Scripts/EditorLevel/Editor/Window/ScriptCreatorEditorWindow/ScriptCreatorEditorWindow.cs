using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor.Compilation;
using System.Linq;

public class ScriptCreatorEditorWindow : EditorWindow
{
    private enum CreateScriptType
    {
        UI,
        ECS,
        Editor
    }

    private string objectName = "";
    private string[] tabTitles = { "Script Creator", "Script Deletor" };
    private int selectedTab = 0;
    private CreateScriptType selectedScriptType = CreateScriptType.UI;
    private List<string> objectAddPaths = null;
    private Dictionary<CreateScriptType, BaseScriptCreator> creators = null;
    private static ScriptCreatorEditorWindow instance;

    [MenuItem("Tools/Create Script %e")] // Ctrl + E 단축키 설정
    public static void ShowWindow()
    {
        GetWindow<ScriptCreatorEditorWindow>("Script Generate");
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

            case CreateScriptType.ECS:
                creator = new ECSScriptCreator();
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
        bool change = DrawScriptTypes();

        if (change)
            ResetObjectName();

        selectedTab = GUILayout.Toolbar(selectedTab, tabTitles);

        GUILayout.Space(10);

        // 각 탭에 대한 내용 표시
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

    private void DrawScriptGenerator()
    {
        bool change = DrawInputName();

        if (change)
            ConvertToObjectNameFromPath();

        DrawCustomOptionByType();

        if (GUILayout.Button("Create"))
        {
            Create();
        }
    }

    private void DrawScriptDeletor()
    {
        GetCurrentCreator()?.DrawScriptDeletor();
    }

    private bool DrawScriptTypes()
    {
        CreateScriptType type = (CreateScriptType)EditorGUILayout.EnumPopup("Select Type", selectedScriptType);
        bool change = type != selectedScriptType;
        selectedScriptType = type;

        return change;
    }

    private bool DrawInputName()
    {
        GUIStyle layoutStyle = new GUIStyle();
        layoutStyle.alignment = TextAnchor.MiddleLeft;
        layoutStyle.fixedWidth = CalculateTotalWidth(objectAddPaths);

        if (objectAddPaths.Count > 0)
            GUILayout.Label("Addable Path");

        EditorGUILayout.BeginHorizontal(layoutStyle);
        {
            string slash = "/";

            int index = 0;

            foreach (string path in objectAddPaths)
            {
                // 버튼 문자열 길이에 맞는 폭 계산
                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(path));
                float buttonWidth = size.x + 10; // 여유 공간 추가

                if (GUILayout.Button(path, GUILayout.Width(buttonWidth)))
                {
                    objectAddPaths.Remove(path);
                    break;
                }

                if (index < objectAddPaths.Count - 1)
                    GUILayout.Label(slash);

                index++;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        {
            objectName = EditorGUILayout.TextField("Script Name", objectName);
            objectName = objectName.Trim();
        }
        return EditorGUI.EndChangeCheck();
    }

    private void DrawCustomOptionByType()
    {
        GetCurrentCreator()?.DrawCustomOptions();
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

    private float CalculateTotalWidth(List<string> paths)
    {
        string slash = "/";
        float totalWidth = 0f;

        foreach (string path in paths)
        {
            Vector2 buttonSize = GUI.skin.label.CalcSize(new GUIContent(path));
            totalWidth += buttonSize.x + 10; // 버튼 너비 계산

            // 슬래시의 폭 추가
            if (path != paths[^1]) // 마지막 아이템이 아니면 슬래시 추가
            {
                Vector2 slashSize = GUI.skin.label.CalcSize(new GUIContent(slash));
                totalWidth += slashSize.x;
            }
        }

        return totalWidth;
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
        Debug.Log("즉시 컴파일 시작!");

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

        Debug.Log("컴파일 요청 완료");
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
        
        Debug.Log("컴파일 완료!");
    }
}
