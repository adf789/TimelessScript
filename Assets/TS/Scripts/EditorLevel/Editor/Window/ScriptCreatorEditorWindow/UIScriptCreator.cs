
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
        {
            EditorGUILayout.HelpBox("삭제할 UI 요소가 없습니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("UI 스크립트 삭제", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("주의: 삭제된 UI는 복구할 수 없습니다.", MessageType.Warning);
        EditorGUILayout.Space();

        for (int i = 0; i < uiCount; i++)
        {
            UIType uiType = (UIType)uiTypes.GetValue(i);

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
            selectedUIType = (UIScriptType)EditorGUILayout.EnumPopup("UI 타입 선택", selectedUIType);
            
            // UI 타입에 따른 설명 제공
            string description = selectedUIType switch
            {
                UIScriptType.View => "View: 전체 화면을 차지하는 UI (예: 메인 메뉴, 게임 화면)",
                UIScriptType.Popup => "Popup: 임시로 나타나는 UI (예: 대화상자, 알림창)",
                UIScriptType.Unit => "Unit: 재사용 가능한 작은 UI 요소 (예: 리스트 아이템, 버튼)",
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
        
        string modelPath = string.Format(StringDefine.PATH_SCRIPT, $"LowLevel/Model/{selectedUIType}");
        string viewPath = string.Format(StringDefine.PATH_SCRIPT, $"MiddleLevel/View/{selectedUIType}");
        string createPrefabPath = string.Format(StringDefine.PATH_VIEW_PREFAB, selectedUIType);
        string createViewName = $"{assetName}{selectedUIType}";
        string createModelName = $"{assetName}{selectedUIType}Model";
        
        if (!string.IsNullOrEmpty(addPath))
        {
            modelPath = Path.Combine(modelPath, addPath);
            viewPath = Path.Combine(viewPath, addPath);
            createPrefabPath = Path.Combine(createPrefabPath, addPath);
        }
        
        // Unit 타입은 다른 구조
        if (selectedUIType == UIScriptType.Unit)
        {
            paths.Add($"{modelPath.Replace("\\", "/")}{createModelName}.cs");
            paths.Add($"{viewPath.Replace("\\", "/")}{createViewName}.cs");
        }
        else
        {
            // View, Popup 타입
            string controllerPath = string.Format(StringDefine.PATH_SCRIPT, $"HighLevel/Controller/{selectedUIType}");
            if (!string.IsNullOrEmpty(addPath))
                controllerPath = Path.Combine(controllerPath, addPath);
            
            paths.Add($"{controllerPath.Replace("\\", "/")}{assetName}Controller.cs");
            paths.Add($"{modelPath.Replace("\\", "/")}{createModelName}.cs");
            paths.Add($"{viewPath.Replace("\\", "/")}{createViewName}.cs");
        }
        
        // 프리팹 경로
        paths.Add($"{createPrefabPath.Replace("\\", "/")}{createViewName}.prefab");
        
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
                
                foreach (string path in finalPaths)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        string normalizedPath = path.Replace("\\", "/");
                        
                        // 파일 타입 아이콘 표시
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
                        string folderPath = Path.GetDirectoryName(normalizedPath);
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
        Debug.Log($"UI 열거형 업데이트 완료: {typeEnumPath}");
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
        Debug.Log($"UI 열거형 업데이트 완료: {typeEnumPath}");
    }

    private Type GetTypeFromUnityAssembly(string typeName, UIScriptType uiScriptType)
    {
        var unityAssembly = uiScriptType == UIScriptType.Unit ? typeof(BaseUnit).Assembly : typeof(BaseView).Assembly; // UnityEngine ��������� �˻�
        var types = unityAssembly.GetTypes();

        return types.FirstOrDefault(t => t.Name == typeName);
    }
}