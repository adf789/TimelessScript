using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor.Compilation;
using System.Linq;

public class ScriptCreatorEditorWindow : EditorWindow
{
    private const string basePath = "Assets/TS/Scripts/{0}/";
    private const string basePrefabPath = "Assets/TS/ResourcesAddressable/Prefabs/";
    private const string controllerTypeFormat = "{0}Controller, HighLevel, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
    private string typeEnumPath = "Assets/TS/Scripts/LowLevel/Enum/UIEnum.cs";
    private string objectName = "";
    private string[] tabTitles = { "Script Creator", "Script Deletor" };
    private int selectedTab = 0;
    private List<string> objectAddPaths = null;
    private Action onEventAddPrefab = null;

    [MenuItem("Tools/Create Script %e")] // Ctrl + E 단축키 설정
    public static void ShowWindow()
    {
        GetWindow<ScriptCreatorEditorWindow>("Script Generate");
    }

    private void OnEnable()
    {
        objectAddPaths = new List<string>();
    }

    private void OnGUI()
    {
        // 탭 그리기
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

                if (index < objectAddPaths.Count - 1)
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
            if (TryGetSeperatePath(ref objectName, out string path))
            {
                objectAddPaths.Add(path);

                if (!string.IsNullOrEmpty(objectName))
                    objectName = char.ToUpper(objectName[0]) + objectName.Substring(1);
            }
        }

        if (GUILayout.Button("Generate MVC Structure"))
        {
            GenerateMVCStructure();
        }
    }

    private void DrawScriptDeletor()
    {
        UIBridge bridge = UIBridge.Get();

        if (bridge == null || bridge.Controllers == null)
            return;

        for(int i = 0; i < bridge.Controllers.Count; i++)
        {
            var pair = bridge.Controllers[i];

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button($"Delete {pair.uiType}"))
                {
                    DeleteUI(bridge, pair);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DeleteUI(UIBridge bridge, UIBridge.BridgePair bridgePair)
    {
        string modelPath = string.Format(basePath, "LowLevel/UIModel");
        string viewPath = string.Format(basePath, "MiddleLevel/UIView");
        string controllerPath = string.Format(basePath, "HighLevel/UIController");

        DeleteFileInFolder($"{bridgePair.uiType}Model", "*.cs", modelPath);
        DeleteFileInFolder($"{bridgePair.uiType}View", "*.cs", viewPath);
        DeleteFileInFolder($"{bridgePair.uiType}Controller", "*.cs", controllerPath);
        DeleteFileInFolder($"{bridgePair.uiType}View", "*.prefab", basePrefabPath);

        bridge.Remove(bridgePair.uiType);

        AssetDatabase.SaveAssetIfDirty(bridge);

        DeleteEnum(bridgePair.uiType.ToString());

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

    private bool DeleteFileInFolder(string deleteFileName, string extension, string folderPath)
    {
        if (string.IsNullOrEmpty(deleteFileName))
        {
            Debug.LogWarning("파일 이름이 비어 있습니다.");
            return false;
        }

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("폴더 이름이 비어 있습니다.");
            return false;
        }

        string absolutePath = Path.Combine(Application.dataPath.Replace("Assets", ""), folderPath);

        if (!Directory.Exists(absolutePath))
        {
            Debug.LogWarning("폴더 경로가 올바르지 않습니다.");
            return false;
        }

        string[] csFiles = Directory.GetFiles(absolutePath, extension, SearchOption.AllDirectories);

        foreach (string filePath in csFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (fileName == deleteFileName)
            {
                // 삭제
                File.Delete(filePath);
                string metaFile = filePath + ".meta";
                if (File.Exists(metaFile))
                    File.Delete(metaFile);

                Debug.Log($"파일 '{deleteFileName}' 삭제됨: {filePath}");

                // 빈 폴더 자동 삭제
                string fileFolder = Path.GetDirectoryName(filePath);
                DeleteIfEmptyFolder(fileFolder);

                AssetDatabase.Refresh();
                return true;
            }
        }

        Debug.LogWarning($"'{deleteFileName}' 파일을 해당 폴더 내에서 찾을 수 없습니다.");
        return false;
    }

    private void DeleteIfEmptyFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return;

        // 파일이 없고, 서브 폴더도 없으면 삭제
        bool isEmpty = Directory.GetFiles(folderPath).Length == 0 &&
                       Directory.GetDirectories(folderPath).Length == 0;

        if (isEmpty)
        {
            Directory.Delete(folderPath);
            string metaFile = folderPath + ".meta";
            if (File.Exists(metaFile))
                File.Delete(metaFile);

            Debug.Log($"빈 폴더 삭제됨: {folderPath}");
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
        string modelPath = string.Format(basePath, "LowLevel/UIModel");
        string viewPath = string.Format(basePath, "MiddleLevel/UIView");
        string controllerPath = string.Format(basePath, "HighLevel/UIController");
        string createPrefabPath = basePrefabPath;
        string createViewName = $"{objectName}View";

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

        createPrefabPath = $"{Path.Combine(createPrefabPath, createViewName).Replace("\\", "/")}.prefab";

        CreateScript(modelPath, $"{objectName}Model", GenerateModelCode(objectName));
        CreateScript(viewPath, createViewName, GenerateViewCode(objectName));
        CreateScript(controllerPath, $"{objectName}Controller", GenerateControllerCode(objectName));
        AddEnum(objectName);
        CreatePrefab(createPrefabPath, createViewName);

        EditorPrefs.SetString("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH", createPrefabPath);
        EditorPrefs.SetString("EDITOR_PREFS_KEY_CREATE_OBJECT_NAME", objectName);
        EditorPrefs.SetString("EDITOR_PREFS_KEY_CREATE_SCRIPT_NAME", createViewName);
        EditorPrefs.SetString("EDITOR_PREFS_KEY_CREATE_SCRIPT_CONTROLLER_TYPE", string.Format(controllerTypeFormat, objectName));

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
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

public class {name}Controller : BaseController<{name}View, {name}Model>
{{
    public override UIType UIType {{ get => UIType.{name}; }}
}}";
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void AttachScriptToPrefab()
    {
        string path = EditorPrefs.GetString("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH");
        string objectName = EditorPrefs.GetString("EDITOR_PREFS_KEY_CREATE_OBJECT_NAME");
        string scriptName = EditorPrefs.GetString("EDITOR_PREFS_KEY_CREATE_SCRIPT_NAME");
        string controllerTypeName = EditorPrefs.GetString("EDITOR_PREFS_KEY_CREATE_SCRIPT_CONTROLLER_TYPE");

        if (string.IsNullOrEmpty(path) ||
            string.IsNullOrEmpty(objectName) ||
            string.IsNullOrEmpty(scriptName) ||
            string.IsNullOrEmpty(controllerTypeName))
            return;

        Debug.Log($"AttachScriptToPrefab Start, path: {path}");

        AddScriptToPrefab(path, scriptName);

        AddTypeToBridge(objectName, controllerTypeName);

        EditorPrefs.DeleteKey("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH");
        EditorPrefs.DeleteKey("EDITOR_PREFS_KEY_CREATE_SCRIPT_NAME");
        EditorPrefs.DeleteKey("EDITOR_PREFS_KEY_CREATE_SCRIPT_CONTROLLER_TYPE");
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

    private static void AddTypeToBridge(string objectName, string typeName)
    {
        UIBridge bridge = UIBridge.Get();

        if (bridge == null)
            return;

        if(Enum.TryParse(typeof(UIType), objectName, out object result) &&
            result is UIType uiType)
        {
            if (uiType == UIType.MaxView || uiType == UIType.MaxPopup)
                return;

            bridge.Add(uiType, typeName);

            EditorUtility.SetDirty(bridge);  // 변경된 데이터 감지
            AssetDatabase.SaveAssets();        // 저장
        }
    }

    private static Type GetTypeFromUnityAssembly(string typeName)
    {
        var unityAssembly = typeof(BaseView).Assembly; // UnityEngine 어셈블리만 검색
        var types = unityAssembly.GetTypes();

        return types.FirstOrDefault(t => t.Name == typeName);
    }

    private void OnCompilationFinished(object context)
    {
        Debug.Log("✅ 컴파일 완료!");

        onEventAddPrefab?.Invoke();
        onEventAddPrefab = null;

        CompilationPipeline.compilationFinished -= OnCompilationFinished;
    }

    public void AddEnum(string insertLine)
    {
        List<string> lines = new List<string>(File.ReadAllLines(typeEnumPath));
        List<string> modifiedLines = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().StartsWith("MaxView"))
            {
                modifiedLines.Add($"    {insertLine},"); // 새로운 줄 추가
                modifiedLines.Add(lines[i]); // 기존 MaxView 한 줄 아래로 내리기
            }
            //else if (lines[i].Trim().StartsWith("MaxPopup"))
            //{
            //    modifiedLines.Add(insertLine); // MaxPopup 한 줄 아래로 내리기 위해 빈 줄 추가
            //    modifiedLines.Add(lines[i]);
            //}
            else
            {
                modifiedLines.Add(lines[i]);
            }
        }

        File.WriteAllLines(typeEnumPath, modifiedLines);
        Debug.Log("파일 수정 완료: " + typeEnumPath);
    }

    public void DeleteEnum(string deleteType)
    {
        List<string> lines = new List<string>(File.ReadAllLines(typeEnumPath));
        List<string> modifiedLines = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Contains(deleteType))
                continue;

            modifiedLines.Add(lines[i]);
        }

        File.WriteAllLines(typeEnumPath, modifiedLines);
        Debug.Log("파일 수정 완료: " + typeEnumPath);
    }
}
