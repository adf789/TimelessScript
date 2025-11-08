
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TableScriptCreator : BaseScriptCreator
{
    private readonly string PATH_TABLE = "MiddleLevel/Table";
    private readonly string PATH_DATA = "LowLevel/TableData";
    private readonly string SUFFIX_TABLE = "Table";
    private readonly string SUFFIX_DATA = "TableData";

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        string tablePath = string.Format(StringDefine.PATH_SCRIPT, PATH_TABLE);
        string tableDatapath = string.Format(StringDefine.PATH_SCRIPT, PATH_DATA);

        if (!string.IsNullOrEmpty(addPath))
            tablePath = Path.Combine(tablePath, addPath);

        if (!string.IsNullOrEmpty(addPath))
            tableDatapath = Path.Combine(tableDatapath, addPath);

        CreateDirectoryIfNotExist(tablePath);
        CreateDirectoryIfNotExist(tableDatapath);

        CreateScript(tablePath, $"{assetName}{SUFFIX_TABLE}", GenerateTableCode(assetName));
        CreateScript(tableDatapath, $"{assetName}{SUFFIX_DATA}", GenerateTableDataCode(assetName));
    }

    public override List<string> GetFinalPaths(string addPath, string assetName)
    {
        var paths = new List<string>();

        // 테이블 경로
        string tablePath = string.Format(StringDefine.PATH_SCRIPT, PATH_TABLE);

        if (!string.IsNullOrEmpty(addPath))
            tablePath = Path.Combine(tablePath, addPath);
        paths.Add($"{tablePath.Replace("\\", "/")}{assetName}{SUFFIX_TABLE}.cs");

        // 테이블 데이터 경로
        string tableDataPath = string.Format(StringDefine.PATH_SCRIPT, PATH_DATA);

        if (!string.IsNullOrEmpty(addPath))
            tableDataPath = Path.Combine(tableDataPath, addPath);
        paths.Add($"{tableDataPath.Replace("\\", "/")}{assetName}{SUFFIX_DATA}.cs");

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

    private string GenerateTableCode(string name)
    {
        return $@"
using UnityEngine;

[CreateAssetMenu(fileName = ""{name}Table"", menuName = ""TS/Table/{name}Table"")]
public class {name}{SUFFIX_TABLE} : BaseTable<{name}TableData>
{{

}}
";
    }

    private string GenerateTableDataCode(string name)
    {
        return $@"
using UnityEngine;

public class {name}{SUFFIX_DATA} : BaseTableData
{{
    
}}
";
    }
}