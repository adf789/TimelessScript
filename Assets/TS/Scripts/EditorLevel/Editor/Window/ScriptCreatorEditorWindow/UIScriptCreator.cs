
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UIScriptCreator : BaseScriptCreator
{
    private enum UIScriptType
    {
        View,
        Popup,
        Unit,
    }

    private UIScriptType selectedUIType;
    private string typeEnumPath = "Assets/TS/Scripts/LowLevel/Enum/UIEnum.cs";

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        string modelPath = string.Format(StringDefine.PATH_SCRIPT, $"LowLevel/Model/{selectedUIType}");
        string viewPath = string.Format(StringDefine.PATH_SCRIPT, $"MiddleLevel/View/{selectedUIType}");
        string createPrefabPath = string.Format(StringDefine.PATH_VIEW_PREFAB, selectedUIType);
        string createViewName = $"{assetName}{selectedUIType}";
        string createModelName = $"{assetName}{selectedUIType}Model";

        if(!string.IsNullOrEmpty(addPath))
            modelPath = Path.Combine(modelPath, addPath);

        if (!string.IsNullOrEmpty(addPath))
            viewPath = Path.Combine(viewPath, addPath);

        if (!string.IsNullOrEmpty(addPath))
            createPrefabPath = Path.Combine(createPrefabPath, addPath);

        CreateDirectoryIfNotExist(modelPath);
        CreateDirectoryIfNotExist(viewPath);
        CreateDirectoryIfNotExist(createPrefabPath);

        // Unit 의 경우
        if(selectedUIType == UIScriptType.Unit)
        {
            CreateScript(modelPath, createModelName, GenerateUnitModelCode(assetName));
            CreateScript(viewPath, createViewName, GenerateUnitCode(assetName));
        }
        // View, Popup 의 경우
        else
        {
            string controllerPath = string.Format(StringDefine.PATH_SCRIPT, $"HighLevel/Controller/{selectedUIType}");

            if (!string.IsNullOrEmpty(addPath))
                controllerPath = Path.Combine(controllerPath, addPath);

            CreateDirectoryIfNotExist(controllerPath);
            CreateScript(controllerPath, $"{assetName}Controller", GenerateControllerCode(createViewName, selectedUIType == UIScriptType.Popup));

            AddEnum(createViewName);

            CreateScript(modelPath, createModelName, GenerateModelCode(createViewName));
            CreateScript(viewPath, createViewName, GenerateViewCode(createViewName));
        }

        createPrefabPath = $"{Path.Combine(createPrefabPath, createViewName).Replace("\\", "/")}.prefab";

        CreatePrefab(createPrefabPath, createViewName);

        EditorPrefs.SetString("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH", createPrefabPath);
        EditorPrefs.SetString("EDITOR_PREFS_KEY_ATTACH_SCRIPT_NAME", createViewName);
        EditorPrefs.SetInt("EDITOR_PREFS_KEY_CRETE_UI_TYPE", (int)selectedUIType);
    }

    public override void OnAfterReload()
    {
        string path = EditorPrefs.GetString("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH");
        string scriptName = EditorPrefs.GetString("EDITOR_PREFS_KEY_ATTACH_SCRIPT_NAME");
        UIScriptType uiScriptType = (UIScriptType)EditorPrefs.GetInt("EDITOR_PREFS_KEY_ATTACH_SCRIPT_NAME");

        if (string.IsNullOrEmpty(path) ||
            string.IsNullOrEmpty(scriptName))
            return;

        Debug.Log($"AttachScriptToPrefab Start, path: {path}");

        AddScriptToPrefab(path, scriptName, uiScriptType);

        EditorPrefs.DeleteKey("EDITOR_PREFS_KEY_CREATE_PREFAB_PATH");
        EditorPrefs.DeleteKey("EDITOR_PREFS_KEY_ATTACH_SCRIPT_NAME");
    }

    public override void DrawScriptDeletor()
    {
        var uiTypes = Enum.GetValues(typeof(UIType));
        int uiCount = uiTypes.GetLength(0);

        if (uiCount == 0)
            return;

        for (int i = 0; i < uiCount; i++)
        {
            UIType uiType = (UIType)uiTypes.GetValue(i);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button($"Delete {uiType}"))
                {
                    DeleteUI(uiType);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    public override void DrawCustomOptions()
    {
        selectedUIType = (UIScriptType)EditorGUILayout.EnumPopup("Select UI Type", selectedUIType);
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

public class {name} : BaseView<{name}Model>
{{

}}";
    }

    private string GenerateUnitModelCode(string name)
    {
        return $@"
public class {name}UnitModel : BaseModel
{{
    
}}";
    }

    private string GenerateUnitCode(string name)
    {
        return $@"
using UnityEngine;

public class {name}Unit : BaseUnit<{name}UnitModel>
{{

}}";
    }

    private string GenerateControllerCode(string name, bool isPopup)
    {
        return $@"
using UnityEngine;

public class {name}Controller : BaseController<{name}, {name}Model>
{{
    public override UIType UIType => UIType.{name};
    public override bool IsPopup => {(isPopup ? "true" : "false")};
}}";
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

    public void AddEnum(string insertLine)
    {
        List<string> lines = new List<string>(File.ReadAllLines(typeEnumPath));

        lines.Remove("public enum UIType");
        lines.Remove("{");
        lines.Remove("}");

        List<string> modifiedLines = new List<string>(lines)
        {
            $"    {insertLine},"
        };

        modifiedLines.Sort((item1, item2) =>
        {
            bool isFirstPopup = item1.Contains("Popup");
            bool isSecondPopup = item2.Contains("Popup");

            // 같은 종류의 UI인 경우
            if ((isFirstPopup && isSecondPopup) ||
            (!isFirstPopup && !isSecondPopup))
            {
                return item1.CompareTo(item2);
            }

            return isFirstPopup ? 1 : -1;
        });

        modifiedLines.Insert(0, "{");
        modifiedLines.Insert(0, "public enum UIType");
        modifiedLines.Add("}");

        File.WriteAllLines(typeEnumPath, modifiedLines);
        Debug.Log("파일 수정 완료: " + typeEnumPath);
    }

    private void AddScriptToPrefab(string prefabPath, string scriptName, UIScriptType uiScriptType)
    {
        // 프리팹 로드
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }

        // 컴포넌트 타입 찾기
        Type scriptType = GetTypeFromUnityAssembly(scriptName, uiScriptType);
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

    private void DeleteUI(UIType uiType)
    {
        bool isPopup = uiType.ToString().Contains("Popup");
        string uiTypeText = isPopup ? UIScriptType.Popup.ToString() : UIScriptType.View.ToString();
        string modelPath = string.Format(StringDefine.PATH_SCRIPT, $"LowLevel/Model/{uiTypeText}");
        string viewPath = string.Format(StringDefine.PATH_SCRIPT, $"MiddleLevel/View/{uiTypeText}");
        string controllerPath = string.Format(StringDefine.PATH_SCRIPT, $"HighLevel/Controller/{uiTypeText}");
        string prefabPath = string.Format(StringDefine.PATH_VIEW_PREFAB, uiTypeText);
        string originName = uiType.ToString().Replace(uiTypeText, "");

        DeleteFileInFolder($"{uiType}Model", "*.cs", modelPath);
        DeleteFileInFolder($"{uiType}", "*.cs", viewPath);
        DeleteFileInFolder($"{originName}Controller", "*.cs", controllerPath);
        DeleteFileInFolder($"{uiType}", "*.prefab", prefabPath);

        DeleteEnum(uiType.ToString());

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

    private void DeleteEnum(string deleteType)
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

    private Type GetTypeFromUnityAssembly(string typeName, UIScriptType uiScriptType)
    {
        var unityAssembly = uiScriptType == UIScriptType.Unit ? typeof(BaseUnit).Assembly : typeof(BaseView).Assembly; // UnityEngine 어셈블리만 검색
        var types = unityAssembly.GetTypes();

        return types.FirstOrDefault(t => t.Name == typeName);
    }
}