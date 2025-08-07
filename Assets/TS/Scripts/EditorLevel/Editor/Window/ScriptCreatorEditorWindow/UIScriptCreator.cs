
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

        // Unit �� ���
        if(selectedUIType == UIScriptType.Unit)
        {
            CreateScript(modelPath, createModelName, GenerateUnitModelCode(assetName));
            CreateScript(viewPath, createViewName, GenerateUnitCode(assetName));
        }
        // View, Popup �� ���
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

            // ���� ������ UI�� ���
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
        Debug.Log("���� ���� �Ϸ�: " + typeEnumPath);
    }

    private void AddScriptToPrefab(string prefabPath, string scriptName, UIScriptType uiScriptType)
    {
        // ������ �ε�
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"�������� ã�� �� �����ϴ�: {prefabPath}");
            return;
        }

        // ������Ʈ Ÿ�� ã��
        Type scriptType = GetTypeFromUnityAssembly(scriptName, uiScriptType);
        if (scriptType == null)
        {
            Debug.LogError($"��ũ��Ʈ '{scriptName}'��(��) ã�� �� �����ϴ�.");
            return;
        }

        // �����տ� ��ũ��Ʈ �߰� (�̹� �߰��Ǿ� ������ ����)
        if (prefab.GetComponent(scriptType) == null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.AddComponent(scriptType);

            // ������ ���� �� ����
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            GameObject.DestroyImmediate(instance);

            Debug.Log($"'{scriptName}' ��ũ��Ʈ�� ������ '{prefab.name}'�� �߰��߽��ϴ�.");
        }
        else
        {
            Debug.Log($"������ '{prefab.name}'���� �̹� '{scriptName}' ��ũ��Ʈ�� �߰��Ǿ� �ֽ��ϴ�.");
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
        Debug.Log("���� ���� �Ϸ�: " + typeEnumPath);
    }

    private Type GetTypeFromUnityAssembly(string typeName, UIScriptType uiScriptType)
    {
        var unityAssembly = uiScriptType == UIScriptType.Unit ? typeof(BaseUnit).Assembly : typeof(BaseView).Assembly; // UnityEngine ������� �˻�
        var types = unityAssembly.GetTypes();

        return types.FirstOrDefault(t => t.Name == typeName);
    }
}