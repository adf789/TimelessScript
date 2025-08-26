
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EditorScriptCreator : BaseScriptCreator
{
    private enum EditorScriptType
    {
        Window,
        Inspector,
    }

    // 선택된 에디터 스크립트 타입
    private EditorScriptType selectedScriptType = EditorScriptType.Window;

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        string path = string.Format(StringDefine.PATH_SCRIPT, $"EditorLevel/Editor/{selectedScriptType}");

        switch (selectedScriptType)
        {
            case EditorScriptType.Window:
                {
                    if (!string.IsNullOrEmpty(addPath))
                        path = Path.Combine(path, addPath);

                    CreateDirectoryIfNotExist(path);
                    CreateScript(path, $"{assetName}EditorWindow", GenerateEditorWindowCode(assetName));
                }
                break;

            case EditorScriptType.Inspector:
                {
                    if (!string.IsNullOrEmpty(addPath))
                        path = Path.Combine(path, addPath);

                    CreateDirectoryIfNotExist(path);
                    CreateScript(path, $"{assetName}Inspector", GenerateInspectorCode(assetName));
                }
                break;
        }
    }

    public override void DrawCustomOptions()
    {
        EditorGUILayout.LabelField("옵션 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("helpbox");
        {
            selectedScriptType = (EditorScriptType)EditorGUILayout.EnumPopup("에디터 스크립트 타입 선택", selectedScriptType);
            
            // 에디터 스크립트 타입에 따른 설명 제공
            string description = selectedScriptType switch
            {
                EditorScriptType.Window => "Window: Unity 에디터에 커스텀 윈도우 추가",
                EditorScriptType.Inspector => "Inspector: 커스텀 인스펙터 UI 제작",
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
        
        string path = string.Format(StringDefine.PATH_SCRIPT, $"EditorLevel/Editor/{selectedScriptType}");
        
        switch (selectedScriptType)
        {
            case EditorScriptType.Window:
                if (!string.IsNullOrEmpty(addPath))
                    path = Path.Combine(path, addPath);
                paths.Add($"{path.Replace("\\", "/")}{assetName}EditorWindow.cs");
                break;

            case EditorScriptType.Inspector:
                if (!string.IsNullOrEmpty(addPath))
                    path = Path.Combine(path, addPath);
                paths.Add($"{path.Replace("\\", "/")}{assetName}Inspector.cs");
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
                EditorGUILayout.LabelField($"{selectedScriptType} 스크립트가 생성됩니다:", EditorStyles.miniLabel);
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
                        if (selectedScriptType == EditorScriptType.Window)
                            labelStyle.normal.textColor = Color.blue;
                        else if (selectedScriptType == EditorScriptType.Inspector)
                            labelStyle.normal.textColor = Color.red;
                        
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

    private string GenerateEditorWindowCode(string name)
    {
        return $@"
using UnityEditor;
using UnityEngine;

public class {name}Window : EditorWindow
{{
    // 단축키 예시:
    // [MenuItem(""Tools/My Tool %t"")]           // Ctrl+T
    // [MenuItem(""Tools/My Tool #t"")]           // Shift+T
    // [MenuItem(""Tools/My Tool %#t"")]          // Ctrl+Shift+T
    // [MenuItem(""Tools/My Tool %&t"")]          // Ctrl+Alt+T
    // [MenuItem(""Tools/My Tool &q"")]           // Alt+Q
    // [MenuItem(""Tools/My Tool F5"")]           // F5 키
    // [MenuItem(""Tools/My Tool %F1"")]          // Ctrl+F1
    [MenuItem(""Tools/{name}Window"")]
    public static void OpenWindow()
    {{
        var window = GetWindow<{name}Window>(""{name}"");

        window.Show();
    }}
}}";
    }

    private string GenerateInspectorCode(string name)
    {
        return $@"
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof({name}))]
public class {name}Inspector : Editor
{{
    private {name} inspectorTarget;

    private void OnEnable()
    {{
        inspectorTarget = ({name})target;
    }}

    public override void OnInspectorGUI()
    {{
        base.OnInspectorGUI();
    }}
}}";
    }
}