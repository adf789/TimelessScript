
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ECSScriptCreator : BaseScriptCreator
{
    private enum EcsScriptType
    {
        Component,
        Buffer,
        Authoring,
        Job,
        System,
    }

    // 그룹별 스크립트 체크박스들
    private bool[] _groupCheckboxes = new bool[5] { false, false, false, false, false };

    private readonly string PATH_COMPONENT = "LowLevel/Data/ComponentData";
    private readonly string PATH_BUFFER = "LowLevel/Data/BufferData";
    private readonly string PATH_AUTHORING = "MiddleLevel/Authoring";
    private readonly string PATH_JOB = "MiddleLevel/Job";
    private readonly string PATH_SYSTEM = "HighLevel/System";
    private readonly string SUFFIX_COMPONENT = nameof(EcsScriptType.Component);
    private readonly string SUFFIX_BUFFER = nameof(EcsScriptType.Buffer);
    private readonly string SUFFIX_AUTHORING = nameof(EcsScriptType.Authoring);
    private readonly string SUFFIX_JOB = nameof(EcsScriptType.Job);
    private readonly string SUFFIX_SYSTEM = nameof(EcsScriptType.System);

    public override void Create(string addPath, string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("Create script name cannot be empty.");
            return;
        }

        bool checkComponent = _groupCheckboxes[(int) EcsScriptType.Component];
        bool checkBuffer = _groupCheckboxes[(int) EcsScriptType.Buffer];
        bool checkAuthoring = _groupCheckboxes[(int) EcsScriptType.Authoring];
        bool checkJob = _groupCheckboxes[(int) EcsScriptType.Job];
        bool checkSystem = _groupCheckboxes[(int) EcsScriptType.System];

        if (checkComponent)
        {
            string componentPath = string.Format(StringDefine.PATH_SCRIPT, PATH_COMPONENT);

            if (!string.IsNullOrEmpty(addPath))
                componentPath = Path.Combine(componentPath, addPath);

            CreateDirectoryIfNotExist(componentPath);
            CreateScript(componentPath, $"{assetName}{SUFFIX_COMPONENT}", GenerateComponentCode(assetName));
        }

        if (checkBuffer)
        {
            string bufferPath = string.Format(StringDefine.PATH_SCRIPT, PATH_BUFFER);

            if (!string.IsNullOrEmpty(addPath))
                bufferPath = Path.Combine(bufferPath, addPath);

            CreateDirectoryIfNotExist(bufferPath);
            CreateScript(bufferPath, $"{assetName}{SUFFIX_BUFFER}", GenerateBufferCode(assetName));
        }

        if (checkAuthoring)
        {
            string authoringPath = string.Format(StringDefine.PATH_SCRIPT, PATH_AUTHORING);

            if (!string.IsNullOrEmpty(addPath))
                authoringPath = Path.Combine(authoringPath, addPath);

            CreateDirectoryIfNotExist(authoringPath);
            CreateScript(authoringPath, $"{assetName}{SUFFIX_AUTHORING}", GenerateAuthoringCode(assetName));
        }

        if (checkJob)
        {
            string jobPath = string.Format(StringDefine.PATH_SCRIPT, PATH_JOB);

            if (!string.IsNullOrEmpty(addPath))
                jobPath = Path.Combine(jobPath, addPath);

            CreateDirectoryIfNotExist(jobPath);
            CreateScript(jobPath, $"{assetName}{SUFFIX_JOB}", GenerateJobCode(assetName));
        }

        if (checkSystem)
        {
            string systemPath = string.Format(StringDefine.PATH_SCRIPT, PATH_SYSTEM);

            if (!string.IsNullOrEmpty(addPath))
                systemPath = Path.Combine(systemPath, addPath);

            CreateDirectoryIfNotExist(systemPath);
            CreateScript(systemPath, $"{assetName}{SUFFIX_SYSTEM}", GenerateSystemCode(assetName));
        }
    }

    public override void DrawCustomOptions()
    {
        EditorGUILayout.LabelField("옵션 설정", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("helpbox");
        {
            EditorGUILayout.LabelField("생성할 ECS 스크립트 선택:", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            for (EcsScriptType type = 0; Enum.IsDefined(typeof(EcsScriptType), type); type++)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    int index = (int) type;
                    _groupCheckboxes[index] = EditorGUILayout.Toggle(_groupCheckboxes[index], GUILayout.Width(20));

                    string description = type switch
                    {
                        EcsScriptType.Component => "Component - ECS 데이터 구조체",
                        EcsScriptType.Buffer => "Buffer - ECS 버퍼 역할 데이터 구조체",
                        EcsScriptType.Authoring => "Authoring - MonoBehaviour에서 ECS로 데이터 변환",
                        EcsScriptType.Job => "Job - 병렬 처리를 위한 Job 시스템",
                        EcsScriptType.System => "System - ECS 업데이트 로직",
                        _ => "None"
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

        bool checkComponent = _groupCheckboxes[(int) EcsScriptType.Component];
        bool checkBuffer = _groupCheckboxes[(int) EcsScriptType.Buffer];
        bool checkAuthoring = _groupCheckboxes[(int) EcsScriptType.Authoring];
        bool checkJob = _groupCheckboxes[(int) EcsScriptType.Job];
        bool checkSystem = _groupCheckboxes[(int) EcsScriptType.System];

        if (checkComponent)
        {
            string componentPath = string.Format(StringDefine.PATH_SCRIPT, PATH_COMPONENT);
            if (!string.IsNullOrEmpty(addPath))
                componentPath = Path.Combine(componentPath, addPath);
            paths.Add($"{componentPath.Replace("\\", "/")}{assetName}{SUFFIX_COMPONENT}.cs");
        }

        if (checkBuffer)
        {
            string componentPath = string.Format(StringDefine.PATH_SCRIPT, PATH_BUFFER);
            if (!string.IsNullOrEmpty(addPath))
                componentPath = Path.Combine(componentPath, addPath);
            paths.Add($"{componentPath.Replace("\\", "/")}{assetName}{SUFFIX_BUFFER}.cs");
        }

        if (checkAuthoring)
        {
            string authoringPath = string.Format(StringDefine.PATH_SCRIPT, PATH_AUTHORING);
            if (!string.IsNullOrEmpty(addPath))
                authoringPath = Path.Combine(authoringPath, addPath);
            paths.Add($"{authoringPath.Replace("\\", "/")}{assetName}{SUFFIX_AUTHORING}.cs");
        }

        if (checkJob)
        {
            string jobPath = string.Format(StringDefine.PATH_SCRIPT, PATH_JOB);
            if (!string.IsNullOrEmpty(addPath))
                jobPath = Path.Combine(jobPath, addPath);
            paths.Add($"{jobPath.Replace("\\", "/")}{assetName}{SUFFIX_JOB}.cs");
        }

        if (checkSystem)
        {
            string systemPath = string.Format(StringDefine.PATH_SCRIPT, PATH_SYSTEM);
            if (!string.IsNullOrEmpty(addPath))
                systemPath = Path.Combine(systemPath, addPath);
            paths.Add($"{systemPath.Replace("\\", "/")}{assetName}{SUFFIX_SYSTEM}.cs");
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

                        // ECS 타입별 스타일
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

    private string GenerateComponentCode(string name)
    {
        return $@"
using Unity.Entities;
using Unity.Mathematics;

public struct {name}{SUFFIX_COMPONENT} : IComponentData
{{
    
}}
";
    }

    private string GenerateBufferCode(string name)
    {
        return $@"
using Unity.Entities;
using Unity.Mathematics;

public struct {name}{SUFFIX_BUFFER} : IBufferElementData
{{
    
}}
";
    }

    private string GenerateAuthoringCode(string name)
    {
        return $@"
using UnityEngine;
using Unity.Entities;

public class {name}{SUFFIX_AUTHORING} : MonoBehaviour
{{
    
    private class Baker : Baker<{name}Authoring>
    {{
        public override void Bake({name}Authoring authoring)
        {{

        }}

    }}
}}
";
    }

    private string GenerateJobCode(string name)
    {
        return $@"
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct {name}{SUFFIX_JOB} : IJobEntity
{{
    
    public void Execute(Entity entity,
    ref LocalTransform localTransform)
    {{

    }}
}}
";
    }

    private string GenerateSystemCode(string name)
    {
        return $@"
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct {name}{SUFFIX_SYSTEM} : ISystem
{{
    //public void OnCreate(ref SystemState state)
    //    => state.RequireForUpdate<>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {{
        //var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        //var job = new {name}Job();

        //state.Dependency = job.ScheduleParallel(state.Dependency);
    }}
}}
";
    }
}