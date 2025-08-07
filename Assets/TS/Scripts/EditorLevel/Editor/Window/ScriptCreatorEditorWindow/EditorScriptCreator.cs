
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

    // 그룹으로 관리할 체크박스들
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
        selectedScriptType = (EditorScriptType)EditorGUILayout.EnumPopup("Select Editor Script Type", selectedScriptType);
    }

    private string GenerateEditorWindowCode(string name)
    {
        return $@"
using UnityEditor;
using UnityEngine;

public class {name}Window : EditorWindow
{{
    // 단축키:
    // [MenuItem(""Tools/My Tool %t"")]           // Ctrl+T
    // [MenuItem(""Tools/My Tool #t"")]           // Shift+T
    // [MenuItem(""Tools/My Tool %#t"")]          // Ctrl+Shift+T
    // [MenuItem(""Tools/My Tool %&t"")]          // Ctrl+Alt+T
    // [MenuItem(""Tools/My Tool &q"")]           // Alt+Q
    // [MenuItem(""Tools/My Tool &t"")]           // Alt+T
    // [MenuItem(""Tools/My Tool %g"")]           // Ctrl+G
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