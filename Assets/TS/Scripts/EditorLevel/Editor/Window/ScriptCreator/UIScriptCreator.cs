
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UIScriptCreator : BaseScriptCreator
{
    private enum ScriptType
    {
        View,
        Popup,
        Unit,
    }

    private ScriptType _selectedType;

    private readonly string PATH_UI_ENUM = "Assets/TS/Scripts/LowLevel/Enum/UIEnum.cs";
    private readonly string[] PATHS_VIEW =
    {
        "MiddleLevel/View/View",
        "MiddleLevel/View/Popup",
        "MiddleLevel/View/Unit",
    };
    private readonly string[] PATHS_MODEL =
    {
        "LowLevel/Model/View",
        "LowLevel/Model/Popup",
        "LowLevel/Model/Unit",
    };
    private readonly string[] PATHS_CONTROLLER =
    {
        "HighLevel/Controller/View",
        "HighLevel/Controller/Popup",
        "",
    };
    private readonly string[] SUFFIXS =
    {
        nameof(ScriptType.View),
        nameof(ScriptType.Popup),
        nameof(ScriptType.Unit),
    };
    private readonly string SUFFIX_CONTROLLER = "Controller";
    private readonly string PREFS_KEY_CREATE_PREFAB_PATH = "EDITOR_PREFS_KEY_CREATE_PREFAB_PATH";
    private readonly string PREFS_KEY_ATTACH_SCRIPT_NAME = "EDITOR_PREFS_KEY_ATTACH_SCRIPT_NAME";
    private readonly string PREFS_KEY_CRETE_UI_TYPE = "EDITOR_PREFS_KEY_CRETE_UI_TYPE";

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        string createPrefabPath = string.Format(StringDefine.PATH_VIEW_PREFAB, _selectedType, assetName);
        string createViewName = $"{assetName}{_selectedType}";
        string createModelName = $"{assetName}{_selectedType}Model";
        string modelPath = string.Format(StringDefine.PATH_SCRIPT, PATHS_MODEL[(int) _selectedType]);
        string viewPath = string.Format(StringDefine.PATH_SCRIPT, PATHS_VIEW[(int) _selectedType]);

        if (!string.IsNullOrEmpty(addPath))
            modelPath = Path.Combine(modelPath, addPath);

        if (!string.IsNullOrEmpty(addPath))
            viewPath = Path.Combine(viewPath, addPath);

        CreateDirectoryIfNotExist(modelPath);
        CreateDirectoryIfNotExist(viewPath);

        // Unit �� ���
        if (_selectedType == ScriptType.Unit)
        {
            CreateScript(modelPath, createModelName, GenerateUnitModelCode(assetName));
            CreateScript(viewPath, createViewName, GenerateUnitCode(assetName));
        }
        // View, Popup �� ���
        else
        {
            string controllerPath = string.Format(StringDefine.PATH_SCRIPT, PATHS_CONTROLLER[(int) _selectedType]);

            if (!string.IsNullOrEmpty(addPath))
                controllerPath = Path.Combine(controllerPath, addPath);

            CreateDirectoryIfNotExist(controllerPath);
            CreateScript(controllerPath, $"{assetName}{SUFFIX_CONTROLLER}", GenerateControllerCode(createViewName, _selectedType == ScriptType.Popup));

            AddEnum(createViewName);

            CreateScript(modelPath, createModelName, GenerateModelCode(createViewName));
            CreateScript(viewPath, createViewName, GenerateViewCode(createViewName));
        }

        createPrefabPath = $"{createPrefabPath}{_selectedType}.prefab";

        CreatePrefab(createPrefabPath, createViewName);

        EditorPrefs.SetString(PREFS_KEY_CREATE_PREFAB_PATH, createPrefabPath);
        EditorPrefs.SetString(PREFS_KEY_ATTACH_SCRIPT_NAME, createViewName);
        EditorPrefs.SetInt(PREFS_KEY_CRETE_UI_TYPE, (int) _selectedType);
    }

    public override void OnAfterReload()
    {
        string path = EditorPrefs.GetString(PREFS_KEY_CREATE_PREFAB_PATH);
        string scriptName = EditorPrefs.GetString(PREFS_KEY_ATTACH_SCRIPT_NAME);
        ScriptType uiScriptType = (ScriptType) EditorPrefs.GetInt(PREFS_KEY_CRETE_UI_TYPE);

        if (string.IsNullOrEmpty(path) ||
            string.IsNullOrEmpty(scriptName))
            return;

        Debug.Log($"AttachScriptToPrefab Start, path: {path}");

        AddScriptToPrefab(path, scriptName, uiScriptType);

        EditorPrefs.DeleteKey(PREFS_KEY_CREATE_PREFAB_PATH);
        EditorPrefs.DeleteKey(PREFS_KEY_ATTACH_SCRIPT_NAME);
        EditorPrefs.DeleteKey(PREFS_KEY_CRETE_UI_TYPE);
    }

    public override void DrawScriptDeletor()
    {
        var uiTypes = Enum.GetValues(typeof(UIType));
        int uiCount = uiTypes.GetLength(0);

        if (uiCount == 0)
        {
            EditorGUILayout.HelpBox("삭제할 UI 요소가 없습니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("UI 스크립트 삭제", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("주의: 삭제된 UI는 복구할 수 없습니다.", MessageType.Warning);
        EditorGUILayout.Space();

        for (int i = 0; i < uiCount; i++)
        {
            UIType uiType = (UIType) uiTypes.GetValue(i);

            EditorGUILayout.BeginHorizontal("box");
            {
                EditorGUILayout.LabelField(uiType.ToString(), GUILayout.ExpandWidth(true));

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("삭제", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("확인", $"{uiType} 스크립트를 삭제하시겠습니까?", "삭제", "취소"))
                    {
                        DeleteUI(uiType);
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    public override void DrawCustomOptions()
    {
        EditorGUILayout.LabelField("옵션 설정", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("helpbox");
        {
            _selectedType = (ScriptType) EditorGUILayout.EnumPopup("UI 타입 선택", _selectedType);

            // UI 타입에 따른 설명 제공
            string description = _selectedType switch
            {
                ScriptType.View => "View: 전체 화면을 차지하는 UI (예: 메인 메뉴, 게임 화면)",
                ScriptType.Popup => "Popup: 임시로 나타나는 UI (예: 대화상자, 알림창)",
                ScriptType.Unit => "Unit: 재사용 가능한 작은 UI 요소 (예: 리스트 아이템, 버튼)",
                _ => ""
            };

            if (!string.IsNullOrEmpty(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();
    }

    public override List<string> GetFinalPaths(string addPath, string assetName)
    {
        var paths = new List<string>();

        string modelPath = string.Format(StringDefine.PATH_SCRIPT, PATHS_MODEL[(int) _selectedType]);
        string viewPath = string.Format(StringDefine.PATH_SCRIPT, PATHS_VIEW[(int) _selectedType]);
        string createPrefabPath = string.Format(StringDefine.PATH_VIEW_PREFAB, _selectedType, assetName);
        string createViewName = $"{assetName}{_selectedType}";
        string createModelName = $"{assetName}{_selectedType}Model";

        if (!string.IsNullOrEmpty(addPath))
        {
            modelPath = Path.Combine(modelPath, addPath);
            viewPath = Path.Combine(viewPath, addPath);
        }

        // Unit 타입은 다른 구조
        if (_selectedType == ScriptType.Unit)
        {
            paths.Add($"{modelPath.Replace("\\", "/")}{createModelName}.cs");
            paths.Add($"{viewPath.Replace("\\", "/")}{createViewName}.cs");
        }
        else
        {
            // View, Popup 타입
            string controllerPath = string.Format(StringDefine.PATH_SCRIPT, PATHS_CONTROLLER[(int) _selectedType]);
            if (!string.IsNullOrEmpty(addPath))
                controllerPath = Path.Combine(controllerPath, addPath);

            paths.Add($"{controllerPath.Replace("\\", "/")}{assetName}{SUFFIX_CONTROLLER}.cs");
            paths.Add($"{modelPath.Replace("\\", "/")}{createModelName}.cs");
            paths.Add($"{viewPath.Replace("\\", "/")}{createViewName}.cs");
        }

        // 프리팹 경로
        paths.Add($"{createPrefabPath}.prefab");

        return paths;
    }

    public override void DrawPathPreview(string addPath, string assetName)
    {
        EditorGUILayout.LabelField("생성 경로 미리보기", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("helpbox");
        {
            var finalPaths = GetFinalPaths(addPath, assetName);

            if (finalPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("생성될 파일이 없습니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"총 {finalPaths.Count}개 파일이 생성됩니다:", EditorStyles.miniLabel);
                EditorGUILayout.Space();

                for (int i = 0; i < finalPaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        string normalizedPath = finalPaths[i].Replace("\\", "/");
                        string folderPath = Path.GetDirectoryName(normalizedPath);
                        string fileType = Path.GetExtension(normalizedPath);
                        string icon = fileType switch
                        {
                            ".cs" => "cs Script Icon",
                            ".prefab" => "Prefab Icon",
                            _ => "DefaultAsset Icon"
                        };

                        GUIContent content = EditorGUIUtility.IconContent(icon);
                        EditorGUILayout.LabelField(content, GUILayout.Width(20), GUILayout.Height(16));

                        EditorGUILayout.LabelField(normalizedPath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                        // Ping 버튼
                        if (GUILayout.Button("📁", GUILayout.Width(25), GUILayout.Height(16)))
                        {
                            PingFolder(folderPath);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("📁 버튼을 클릭하면 해당 폴더로 이동합니다.", MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
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
        List<string> lines = new List<string>(File.ReadAllLines(PATH_UI_ENUM));

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

        File.WriteAllLines(PATH_UI_ENUM, modifiedLines);
        Debug.Log($"UI 열거형 업데이트 완료: {PATH_UI_ENUM}");
    }

    private void AddScriptToPrefab(string prefabPath, string scriptName, ScriptType uiScriptType)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Not found prefab: {prefabPath}");
            return;
        }

        Type scriptType = GetTypeFromUnityAssembly(scriptName, uiScriptType);
        if (scriptType == null)
        {
            Debug.LogError($"Not found script type: {scriptName}");
            return;
        }

        if (prefab.GetComponent(scriptType) == null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.AddComponent(scriptType);

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
        string suffix_popup = SUFFIXS[(int) ScriptType.Popup];
        bool isPopup = uiType.ToString().Contains(suffix_popup);
        int index = isPopup ? (int) ScriptType.Popup : (int) ScriptType.View;

        string uiTypeText = SUFFIXS[index];
        string modelPath = PATHS_MODEL[index];
        string viewPath = PATHS_VIEW[index];
        string controllerPath = PATHS_CONTROLLER[index];
        string prefabPath = string.Format(StringDefine.PATH_VIEW_PREFAB, uiTypeText, uiType);
        string originName = uiType.ToString().Replace(uiTypeText, "");

        DeleteFileInFolder($"{uiType}Model", "*.cs", modelPath);
        DeleteFileInFolder($"{uiType}", "*.cs", viewPath);
        DeleteFileInFolder($"{originName}{SUFFIX_CONTROLLER}", "*.cs", controllerPath);
        DeleteFileInFolder($"{uiType}", "*.prefab", prefabPath);

        DeleteEnum(uiType.ToString());

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

    private void DeleteEnum(string deleteType)
    {
        List<string> lines = new List<string>(File.ReadAllLines(PATH_UI_ENUM));
        List<string> modifiedLines = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Contains(deleteType))
                continue;

            modifiedLines.Add(lines[i]);
        }

        File.WriteAllLines(PATH_UI_ENUM, modifiedLines);
        Debug.Log($"UI 열거형 업데이트 완료: {PATH_UI_ENUM}");
    }

    private Type GetTypeFromUnityAssembly(string typeName, ScriptType uiScriptType)
    {
        var unityAssembly = uiScriptType == ScriptType.Unit ? typeof(BaseUnit).Assembly : typeof(BaseView).Assembly; // UnityEngine ��������� �˻�
        var types = unityAssembly.GetTypes();

        return types.FirstOrDefault(t => t.Name == typeName);
    }

    private string GenerateModelCode(string name)
    {
        return $@"
public class {name}Model : BaseModel
{{
    
}}
";
    }

    private string GenerateViewCode(string name)
    {
        return $@"
using UnityEngine;

public class {name} : BaseView<{name}Model>
{{
    public override void Show()
    {{

    }}
}}
";
    }

    private string GenerateUnitModelCode(string name)
    {
        return $@"
public struct {name}UnitModel : IUnitModel
{{
    
}}
";
    }

    private string GenerateUnitCode(string name)
    {
        return $@"
using UnityEngine;

public class {name}Unit : BaseUnit<{name}UnitModel>
{{
    public override void Show()
    {{

    }}
}}
";
    }

    private string GenerateControllerCode(string name, bool isPopup)
    {
        return $@"
using Cysharp.Threading.Tasks;
using UnityEngine;

public class {name}{SUFFIX_CONTROLLER} : BaseController<{name}, {name}Model>
{{
    public override UIType UIType => UIType.{name};
    public override bool IsPopup => {(isPopup ? "true" : "false")};

    public override void BeforeEnterProcess()
    {{

    }}

    public override void EnterProcess()
    {{
        view.Show();
    }}

    public override void BeforeExitProcess()
    {{

    }}

    public override void ExitProcess()
    {{

    }}
}}
";
    }
}