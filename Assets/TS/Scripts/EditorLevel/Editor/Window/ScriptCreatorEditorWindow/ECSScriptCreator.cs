
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

    // 그룹으로 관리할 체크박스들
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
        EditorGUILayout.LabelField("생성할 스크립트:", EditorStyles.boldLabel);

        for (int i = 0; i < groupCheckboxes.Length; i++)
        {
            groupCheckboxes[i] = EditorGUILayout.Toggle(checkboxLabels[i], groupCheckboxes[i]);
        }
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