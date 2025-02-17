using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor.Compilation;
using System.Linq;
using System.Collections;
using Cysharp.Threading.Tasks;

public class ScriptCreatorEditorWindow : EditorWindow
{
    private string basePath = "Assets/TS/Scripts/{0}/";
    private string basePrefabPath = "Assets/TS/ResourcesAddressable/Prefabs/";
    private string objectName = "";
    private List<string> objectAddPaths = null;
    private Action onEventAddPrefab = null;

    [MenuItem("Tools/Create Script %e")] // Ctrl + E 단축키 설정
    public static void ShowWindow()
    {
        GetWindow<ScriptCreatorEditorWindow>("Script Creator");
    }

    private void OnEnable()
    {
        objectAddPaths = new List<string>();
    }

    private void OnGUI()
    {
        GUILayout.Label("Script Creator", EditorStyles.boldLabel);

        if (objectAddPaths.Count > 0)
            GUILayout.Label("Addable Path");

        GUIStyle layoutStyle = new GUIStyle();
        layoutStyle.alignment = TextAnchor.MiddleLeft;
        layoutStyle.fixedWidth = CalculateTotalWidth(objectAddPaths);

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

                if(index < objectAddPaths.Count - 1)
                    GUILayout.Label(slash);

                index++;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        {
            objectName = EditorGUILayout.TextField("ObjectName", objectName);
        }
        bool isChangeName = EditorGUI.EndChangeCheck();

        if (isChangeName)
        {
            if(TryGetSeperatePath(ref objectName, out string path))
                objectAddPaths.Add(path);
        }

        if (GUILayout.Button("Generate MVC Structure"))
        {
            GenerateMVCStructure();
        }
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

    private void GenerateMVCStructure()
    {
        if (string.IsNullOrEmpty(objectName))
        {
            Debug.LogError("Object name cannot be empty.");
            return;
        }

        // 경로에서 `/`를 기준으로 폴더 구조 생성
        string modelPath = string.Format(basePath, "LowLevel");
        string viewPath = string.Format(basePath, "MiddleLevel");
        string controllerPath = string.Format(basePath, "HighLevel");
        string createPrefabPath = basePrefabPath;
        string createScriptName = $"{objectName}View";
        string createPrefabName = $"{objectName}Prefab";

        if (objectAddPaths.Count > 0)
        {
            string addPath = $"{string.Join('/', objectAddPaths)}/";

            modelPath = Path.Combine(modelPath, addPath);
            viewPath = Path.Combine(viewPath, addPath);
            controllerPath = Path.Combine(controllerPath, addPath);
            createPrefabPath = Path.Combine(createPrefabPath, addPath);
        }

        CreateDirectoryIfNotExist(modelPath);
        CreateDirectoryIfNotExist(viewPath);
        CreateDirectoryIfNotExist(controllerPath);
        CreateDirectoryIfNotExist(createPrefabPath);

        createPrefabPath = $"{Path.Combine(createPrefabPath, createPrefabName).Replace("\\", "/")}.prefab";

        CreateScript(modelPath, $"{objectName}Model", GenerateModelCode(objectName));
        CreateScript(viewPath, createScriptName, GenerateViewCode(objectName));
        CreateScript(controllerPath, $"{objectName}Controller", GenerateControllerCode(objectName));
        CreatePrefab(createPrefabPath, createPrefabName);

        EditorPrefs.SetString("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH", createPrefabPath);
        EditorPrefs.SetString("EDITOR_PREFS_KEY_CREATE_SCRIPT_NAME", createScriptName);

        AssetDatabase.Refresh();
    }

    private void CreateDirectoryIfNotExist(string path)
    {
        // 모든 하위 디렉토리 포함하여 생성
        string normalizedPath = path.Replace("\\", "/");
        if (!Directory.Exists(normalizedPath))
        {
            Directory.CreateDirectory(normalizedPath);
        }
    }

    private void CreateScript(string path, string fileName, string content)
    {
        string filePath = Path.Combine(path, $"{fileName.Replace("/", "")}.cs").Replace("\\", "/");
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, content);
        }
    }

    private void CreatePrefab(string prefabPath, string name)
    {
        if (!File.Exists(prefabPath))
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<RectTransform>();
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            DestroyImmediate(obj);

            AssetDatabase.Refresh();
        }
    }

    private string GenerateModelCode(string name)
    {
        return $@"
public class {name}Model : BaseModel
{{
    
}}";
    }

    private string GenerateViewCode(string name)
    {
        return $@"
using UnityEngine;

public class {name}View : BaseView<{name}Model>
{{

}}";
    }

    private string GenerateControllerCode(string name)
    {
        return $@"
using UnityEngine;

public class {name}Controller
{{
    private {name}Model model;
    private {name}View view;
}}";
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void AttachScriptToPrefab()
    {
        string path = EditorPrefs.GetString("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH");
        string name = EditorPrefs.GetString("EDITOR_PREFS_KEY_CREATE_SCRIPT_NAME");

        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name))
            return;

        Debug.Log($"AttachScriptToPrefab Start, path: {path}");

        AddScriptToPrefab(path, name);

        EditorPrefs.DeleteKey("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH");
        EditorPrefs.DeleteKey("EDITOR_PREFS_KEY_CREATE_SCRIPT_NAME");
    }

    private static void AddScriptToPrefab(string prefabPath, string scriptName)
    {
        // 프리팹 로드
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }

        // 컴포넌트 타입 찾기
        Type scriptType = GetTypeFromUnityAssembly(scriptName);
        if (scriptType == null)
        {
            Debug.LogError($"스크립트 '{scriptName}'을(를) 찾을 수 없습니다.");
            return;
        }

        // 프리팹에 스크립트 추가 (이미 추가되어 있으면 무시)
        if (prefab.GetComponent(scriptType) == null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.AddComponent(scriptType);

            // 프리팹 적용 후 삭제
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            GameObject.DestroyImmediate(instance);

            Debug.Log($"'{scriptName}' 스크립트를 프리팹 '{prefab.name}'에 추가했습니다.");
        }
        else
        {
            Debug.Log($"프리팹 '{prefab.name}'에는 이미 '{scriptName}' 스크립트가 추가되어 있습니다.");
        }
    }

    private static Type GetTypeFromUnityAssembly(string typeName)
    {
        var unityAssembly = typeof(BaseView).Assembly; // UnityEngine 어셈블리만 검색
        var types = unityAssembly.GetTypes();

        return types.FirstOrDefault(t => t.Name == typeName);
    }

    private void ForceCompile(Action onEventCompleteCompile)
    {
        CompilationPipeline.RequestScriptCompilation();
        AssetDatabase.Refresh();

        onEventAddPrefab = onEventCompleteCompile;

        CompilationPipeline.compilationFinished += OnCompilationFinished;
        //CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
    }

    private void OnCompilationFinished(object context)
    {
        Debug.Log("✅ 컴파일 완료!");

        onEventAddPrefab?.Invoke();
        onEventAddPrefab = null;

        CompilationPipeline.compilationFinished -= OnCompilationFinished;
    }

    private void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
    {
        Debug.Log($"✅ Assembly 컴파일 완료: {assemblyPath}");

        // 특정 Assembly에서 새로 추가된 타입을 확인 가능
        var newTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(t => !string.IsNullOrEmpty(t.Namespace) && t.Namespace.Contains("MiddleLevel")); // 원하는 네임스페이스만 필터링

        foreach (var type in newTypes)
        {
            Debug.Log($"🔹 Assembly에서 검색 가능해진 타입: {type.FullName}");
        }
    }
}
