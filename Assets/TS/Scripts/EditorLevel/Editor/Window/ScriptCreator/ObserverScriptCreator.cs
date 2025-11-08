
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ObserverScriptCreator : BaseScriptCreator
{
    private readonly string PATH = "LowLevel/Observer";
    private readonly string SUFFIX = "Param";

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        string path = string.Format(StringDefine.PATH_SCRIPT, PATH);

        if (!string.IsNullOrEmpty(addPath))
            path = Path.Combine(path, addPath);

        CreateDirectoryIfNotExist(path);
        CreateScript(path, $"{assetName}{SUFFIX}", GenerateObserverParamCode(assetName));
    }

    public override List<string> GetFinalPaths(string addPath, string assetName)
    {
        var paths = new List<string>();

        string path = string.Format(StringDefine.PATH_SCRIPT, PATH);

        if (!string.IsNullOrEmpty(addPath))
            path = Path.Combine(path, addPath);
        paths.Add($"{path.Replace("\\", "/")}{assetName}{SUFFIX}.cs");

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
                EditorGUILayout.LabelField($"옵저버 스크립트가 생성됩니다:", EditorStyles.miniLabel);
                EditorGUILayout.Space();

                for (int i = 0; i < finalPaths.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        string normalizedPath = finalPaths[i].Replace("\\", "/");
                        string folderPath = Path.GetDirectoryName(normalizedPath);
                        Color pathColor = GetPathColor(i);
                        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);

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

    private string GenerateObserverParamCode(string name)
    {
        return $@"
public struct {name}{SUFFIX} : IObserverParam
{{

}}
";
    }
}