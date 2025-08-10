
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ECSScriptCreator : BaseScriptCreator
{
    private enum EcsScriptType
    {
        Component,
        Authoring,
        Job,
        System,
    }

    // 그룹별 스크립트 체크박스들
    private bool[] groupCheckboxes = new bool[4] { false, false, false, false };
    private string[] checkboxLabels = { "Component", "Authoring", "Job", "System" };

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        bool checkComponent = groupCheckboxes[(int)EcsScriptType.Component];
        bool checkAuthoring = groupCheckboxes[(int)EcsScriptType.Authoring];
        bool checkJob = groupCheckboxes[(int)EcsScriptType.Job];
        bool checkSystem = groupCheckboxes[(int)EcsScriptType.System];

        if (checkComponent)
        {
            string componentPath = string.Format(StringDefine.PATH_SCRIPT, "LowLevel/Data/ComponentData");

            if(!string.IsNullOrEmpty(addPath))
                componentPath = Path.Combine(componentPath, addPath);

            CreateDirectoryIfNotExist(componentPath);
            CreateScript(componentPath, $"{assetName}Component", GenerateComponentCode(assetName));
        }

        if (checkAuthoring)
        {
            string authoringPath = string.Format(StringDefine.PATH_SCRIPT, "MiddleLevel/Authoring");

            if (!string.IsNullOrEmpty(addPath))
                authoringPath = Path.Combine(authoringPath, addPath);

            CreateDirectoryIfNotExist(authoringPath);
            CreateScript(authoringPath, $"{assetName}Authoring", GenerateAuthoringCode(assetName));
        }

        if (checkJob)
        {
            string jobPath = string.Format(StringDefine.PATH_SCRIPT, "MiddleLevel/Job");

            if (!string.IsNullOrEmpty(addPath))
                jobPath = Path.Combine(jobPath, addPath);

            CreateDirectoryIfNotExist(jobPath);
            CreateScript(jobPath, $"{assetName}Job", GenerateJobCode(assetName));
        }

        if (checkSystem)
        {
            string systemPath = string.Format(StringDefine.PATH_SCRIPT, "HighLevel/System");

            if (!string.IsNullOrEmpty(addPath))
                systemPath = Path.Combine(systemPath, addPath);

            CreateDirectoryIfNotExist(systemPath);
            CreateScript(systemPath, $"{assetName}System", GenerateSystemCode(assetName));
        }
    }

    public override void DrawCustomOptions()
    {
        EditorGUILayout.LabelField("옵션 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("helpbox");
        {
            EditorGUILayout.LabelField("생성할 ECS 스크립트 선택:", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            for (int i = 0; i < groupCheckboxes.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    groupCheckboxes[i] = EditorGUILayout.Toggle(groupCheckboxes[i], GUILayout.Width(20));
                    
                    string description = checkboxLabels[i] switch
                    {
                        "Component" => "Component - ECS 데이터 구조체",
                        "Authoring" => "Authoring - MonoBehaviour에서 ECS로 데이터 변환",
                        "Job" => "Job - 병렬 처리를 위한 Job 시스템",
                        "System" => "System - ECS 업데이트 로직",
                        _ => checkboxLabels[i]
                    };
                    
                    EditorGUILayout.LabelField(description, GUILayout.ExpandWidth(true));
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("하나 이상의 스크립트를 선택하세요. 일반적으로 Component와 System을 함께 사용합니다.", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
    }
    
    public override List<string> GetFinalPaths(string addPath, string assetName)
    {
        var paths = new List<string>();
        
        bool checkComponent = groupCheckboxes[(int)EcsScriptType.Component];
        bool checkAuthoring = groupCheckboxes[(int)EcsScriptType.Authoring];
        bool checkJob = groupCheckboxes[(int)EcsScriptType.Job];
        bool checkSystem = groupCheckboxes[(int)EcsScriptType.System];
        
        if (checkComponent)
        {
            string componentPath = string.Format(StringDefine.PATH_SCRIPT, "LowLevel/Data/ComponentData");
            if (!string.IsNullOrEmpty(addPath))
                componentPath = Path.Combine(componentPath, addPath);
            paths.Add($"{componentPath.Replace("\\", "/")}/{assetName}Component.cs");
        }
        
        if (checkAuthoring)
        {
            string authoringPath = string.Format(StringDefine.PATH_SCRIPT, "MiddleLevel/Authoring");
            if (!string.IsNullOrEmpty(addPath))
                authoringPath = Path.Combine(authoringPath, addPath);
            paths.Add($"{authoringPath.Replace("\\", "/")}/{assetName}Authoring.cs");
        }
        
        if (checkJob)
        {
            string jobPath = string.Format(StringDefine.PATH_SCRIPT, "MiddleLevel/Job");
            if (!string.IsNullOrEmpty(addPath))
                jobPath = Path.Combine(jobPath, addPath);
            paths.Add($"{jobPath.Replace("\\", "/")}/{assetName}Job.cs");
        }
        
        if (checkSystem)
        {
            string systemPath = string.Format(StringDefine.PATH_SCRIPT, "HighLevel/System");
            if (!string.IsNullOrEmpty(addPath))
                systemPath = Path.Combine(systemPath, addPath);
            paths.Add($"{systemPath.Replace("\\", "/")}/{assetName}System.cs");
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
                EditorGUILayout.HelpBox("생성할 스크립트를 하나 이상 선택하세요.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"총 {finalPaths.Count}개 ECS 스크립트가 생성됩니다:", EditorStyles.miniLabel);
                EditorGUILayout.Space();
                
                foreach (string path in finalPaths)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        string normalizedPath = path.Replace("\\", "/");
                        
                        // C# 스크립트 아이콘
                        GUIContent content = EditorGUIUtility.IconContent("cs Script Icon");
                        EditorGUILayout.LabelField(content, GUILayout.Width(20), GUILayout.Height(16));
                        
                        // ECS 타입별 라벨 색상
                        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                        string fileName = Path.GetFileNameWithoutExtension(normalizedPath);
                        if (fileName.EndsWith("Component"))
                            labelStyle.normal.textColor = Color.cyan;
                        else if (fileName.EndsWith("Authoring"))
                            labelStyle.normal.textColor = Color.green;
                        else if (fileName.EndsWith("Job"))
                            labelStyle.normal.textColor = Color.yellow;
                        else if (fileName.EndsWith("System"))
                            labelStyle.normal.textColor = Color.magenta;
                        
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

    private string GenerateComponentCode(string name)
    {
        return $@"
using Unity.Entities;

public struct {name}Component : IComponentData
{{
    
}}";
    }

    private string GenerateAuthoringCode(string name)
    {
        return $@"
using UnityEngine;
using Unity.Entities;

public class {name}Authoring : MonoBehaviour
{{
    
    private class Baker : Baker<{name}Authoring>
    {{
        public override void Bake({name}Authoring authoring)
        {{

        }}

    }}
}}";
    }

    private string GenerateJobCode(string name)
    {
        return $@"
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct {name}Job : IJobEntity
{{
    
    public void Execute(ref LocalTransform localTransform)
    {{

    }}
}}";
    }

    private string GenerateSystemCode(string name)
    {
        return $@"
using Unity.Burst;
using Unity.Entities;

public partial struct {name}System : ISystem
{{
    //public void OnCreate(ref SystemState state)
    //    => state.RequireForUpdate<>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {{
        //var job = new {name}Job();

        //state.Dependency = job.ScheduleParallel(state.Dependency);
    }}
}}";
    }
}