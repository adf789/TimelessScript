
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TableScriptCreator : BaseScriptCreator
{
    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        string tablePath = string.Format(StringDefine.PATH_SCRIPT, $"MiddleLevel/Table");
        string tableDatapath = string.Format(StringDefine.PATH_SCRIPT, $"LowLevel/TableData");

        if (!string.IsNullOrEmpty(addPath))
            tablePath = Path.Combine(tablePath, addPath);

        if (!string.IsNullOrEmpty(addPath))
            tableDatapath = Path.Combine(tableDatapath, addPath);

        CreateDirectoryIfNotExist(tablePath);
        CreateDirectoryIfNotExist(tableDatapath);

        CreateScript(tablePath, $"{assetName}Table", GenerateTableCode(assetName));
        CreateScript(tableDatapath, $"{assetName}TableData", GenerateTableDataCode(assetName));
    }

    public override List<string> GetFinalPaths(string addPath, string assetName)
    {
        var paths = new List<string>();

        // 테이블 경로
        string tablePath = string.Format(StringDefine.PATH_SCRIPT, $"MiddleLevel/Table");

        if (!string.IsNullOrEmpty(addPath))
            tablePath = Path.Combine(tablePath, addPath);
        paths.Add($"{tablePath.Replace("\\", "/")}{assetName}Table.cs");

        // 테이블 데이터 경로
        string tableDataPath = string.Format(StringDefine.PATH_SCRIPT, $"LowLevel/TableData");

        if (!string.IsNullOrEmpty(addPath))
            tableDataPath = Path.Combine(tableDataPath, addPath);
        paths.Add($"{tableDataPath.Replace("\\", "/")}{assetName}TableData.cs");

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

                foreach (string path in finalPaths)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        string normalizedPath = path.Replace("\\", "/");

                        // C# 스크립트 아이콘
                        GUIContent content = EditorGUIUtility.IconContent("cs Script Icon");
                        EditorGUILayout.LabelField(content, GUILayout.Width(20), GUILayout.Height(16));

                        // 에디터 타입별 라벨 스타일
                        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                        string fileName = Path.GetFileNameWithoutExtension(normalizedPath);
                        if (fileName.EndsWith("Table"))
                            labelStyle.normal.textColor = Color.cyan;
                        else if (fileName.EndsWith("TableData"))
                            labelStyle.normal.textColor = Color.aquamarine;

                        EditorGUILayout.LabelField(normalizedPath, labelStyle, GUILayout.ExpandWidth(true));

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

    private string GenerateTableCode(string name)
    {
        return $@"
using UnityEngine;

[CreateAssetMenu(fileName = ""{name}Table"", menuName = ""Scriptable Objects/Table/{name}Table"")]
public class {name}Table : BaseTable<{name}TableData>
{{

}}
";
    }

    private string GenerateTableDataCode(string name)
    {
        return $@"
using UnityEngine;

public class {name}TableData : BaseTableData
{{
    
}}
";
    }
}