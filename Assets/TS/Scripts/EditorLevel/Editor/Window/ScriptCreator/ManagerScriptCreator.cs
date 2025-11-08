
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ManagerScriptCreator : BaseScriptCreator
{
    private enum ScriptType
    {
        Manager,
        SubManager,
    }

    private ScriptType selectedType;

    private readonly string PATH_MANAGER = "HighLevel/Manager";
    private readonly string PATH_SUBMANAGER = "MiddleLevel/SubManager";
    private readonly string SUFFIX_MANAGER = "Manager";
    private readonly string SUFFIX_SUBMANAGER = "SubManager";

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        string path = null;

        switch (selectedType)
        {
            case ScriptType.Manager:
                path = string.Format(StringDefine.PATH_SCRIPT, PATH_MANAGER);
                break;
            case ScriptType.SubManager:
                path = string.Format(StringDefine.PATH_SCRIPT, PATH_SUBMANAGER);
                break;
            default:
                Debug.Log("정의되지 않은 타입입니다.");
                return;
        }

        if (!string.IsNullOrEmpty(addPath))
            path = Path.Combine(path, addPath);

        CreateDirectoryIfNotExist(path);

        switch (selectedType)
        {
            case ScriptType.Manager:
                CreateScript(path, $"{assetName}{SUFFIX_MANAGER}", GenerateManagerCode(assetName));
                break;
            case ScriptType.SubManager:
                CreateScript(path, $"{assetName}{SUFFIX_SUBMANAGER}", GenerateSubManagerCode(assetName));
                break;
        }
    }

    public override List<string> GetFinalPaths(string addPath, string assetName)
    {
        var paths = new List<string>();

        switch (selectedType)
        {
            case ScriptType.Manager:
                {
                    string path = string.Format(StringDefine.PATH_SCRIPT, PATH_MANAGER);

                    if (!string.IsNullOrEmpty(addPath))
                        path = Path.Combine(path, addPath);
                    paths.Add($"{path.Replace("\\", "/")}{assetName}{SUFFIX_MANAGER}.cs");
                }
                break;

            case ScriptType.SubManager:
                {
                    string path = string.Format(StringDefine.PATH_SCRIPT, PATH_SUBMANAGER);

                    if (!string.IsNullOrEmpty(addPath))
                        path = Path.Combine(path, addPath);
                    paths.Add($"{path.Replace("\\", "/")}{assetName}{SUFFIX_SUBMANAGER}.cs");
                }
                break;
        }

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
                EditorGUILayout.LabelField($"데이터 스크립트가 생성됩니다:", EditorStyles.miniLabel);
                EditorGUILayout.Space();

                for (int i = 0; i < finalPaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        string normalizedPath = finalPaths[i].Replace("\\", "/");
                        string folderPath = Path.GetDirectoryName(normalizedPath);
                        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                        Color pathColor = GetPathColor(i);

                        labelStyle.normal.textColor = pathColor;

                        // C# 스크립트 아이콘
                        GUIContent content = EditorGUIUtility.IconContent("cs Script Icon");
                        EditorGUILayout.LabelField(content, GUILayout.Width(20), GUILayout.Height(16));

                        // 에디터 타입별 라벨 스타일
                        EditorGUILayout.LabelField(normalizedPath, labelStyle, GUILayout.ExpandWidth(true));

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

    public override void DrawCustomOptions()
    {
        EditorGUILayout.LabelField("옵션 설정", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("helpbox");
        {
            selectedType = (ScriptType) EditorGUILayout.EnumPopup("Manager 타입 선택", selectedType);

            // UI 타입에 따른 설명 제공
            string description = selectedType switch
            {
                ScriptType.Manager => "Manager: 게임 오브젝트를 가지는 매니저 (예: 카메라, UI)",
                ScriptType.SubManager => "SubManager: 오브젝트 없이 데이터 및 함수만 가지는 매니저 (예: 미션, 네트워크)",
                _ => ""
            };

            if (!string.IsNullOrEmpty(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private string GenerateManagerCode(string name)
    {
        return $@"
public class {name}{SUFFIX_MANAGER} : BaseManager<{name}Manager>
{{

}}
";
    }

    private string GenerateSubManagerCode(string name)
    {
        return $@"
public class {name}{SUFFIX_SUBMANAGER} : SubBaseManager<{name}SubManager>
{{

}}
";
    }
}