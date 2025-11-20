#if UNITY_EDITOR
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public partial class DrawEntityGizmosSystem : SystemBase
{
    private GUIStyle _labelStyle;
    private List<(float3 position, string label)> _labelData = new List<(float3, string)>();

    protected override void OnCreate()
    {
        base.OnCreate();

        // GUIStyle 초기화
        _labelStyle = new GUIStyle()
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.BoldAndItalic
        };
        _labelStyle.normal.textColor = Color.yellow;

        // SceneView 콜백 등록 (Scene GUI 이벤트에서 Handles 호출)
        SceneView.duringSceneGui += OnSceneGUI;
    }

    protected override void OnDestroy()
    {
        // 콜백 해제
        SceneView.duringSceneGui -= OnSceneGUI;
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        // Editor에서만 실행
        if (!Application.isPlaying) return;

        // 라벨 데이터 수집
        _labelData.Clear();

        foreach (var (ltw, obj, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<TSObjectComponent>>()
            .WithEntityAccess())
        {
            float3 position = ltw.ValueRO.Position;

            // 구체 모양 근사 (8각형 3개로 구 표현)
            DrawDebugSphere(position, 0.3f, Color.yellow);
            var name = obj.ValueRO.Name;

            // 라벨 데이터 저장 (SceneView 콜백에서 그림)
            _labelData.Add((
                position + new float3(0, 0.5f, 0),
                $"{name}\n({entity.Index})"
            ));
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // SceneView GUI 이벤트에서 Handles 호출
        if (!Application.isPlaying) return;

        foreach (var (position, label) in _labelData)
        {
            Handles.Label(position, label, _labelStyle);
        }
    }

    private void DrawDebugSphere(float3 center, float radius, Color color)
    {
        int segments = 8;

        // X축 원
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * math.PI * 2 / segments;
            float angle2 = (i + 1) * math.PI * 2 / segments;
            float3 p1 = center + new float3(0, math.cos(angle1) * radius, math.sin(angle1) * radius);
            float3 p2 = center + new float3(0, math.cos(angle2) * radius, math.sin(angle2) * radius);
            Debug.DrawLine(p1, p2, color, 0.1f);
        }

        // Y축 원
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * math.PI * 2 / segments;
            float angle2 = (i + 1) * math.PI * 2 / segments;
            float3 p1 = center + new float3(math.cos(angle1) * radius, 0, math.sin(angle1) * radius);
            float3 p2 = center + new float3(math.cos(angle2) * radius, 0, math.sin(angle2) * radius);
            Debug.DrawLine(p1, p2, color, 0.1f);
        }

        // Z축 원
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * math.PI * 2 / segments;
            float angle2 = (i + 1) * math.PI * 2 / segments;
            float3 p1 = center + new float3(math.cos(angle1) * radius, math.sin(angle1) * radius, 0);
            float3 p2 = center + new float3(math.cos(angle2) * radius, math.sin(angle2) * radius, 0);
            Debug.DrawLine(p1, p2, color, 0.1f);
        }
    }
}

#endif