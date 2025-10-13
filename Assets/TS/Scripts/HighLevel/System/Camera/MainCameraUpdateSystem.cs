using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Updates MainCameraComponent data from Unity Camera
/// Runs in presentation system group to get latest camera position
/// </summary>
[DisableAutoCreation]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(TilemapRenderingSystem))]
public partial struct MainCameraUpdateSystem : ISystem
{
    private Camera mainCamera;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MainCameraComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Cache camera reference
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Update camera component data
        foreach (var cameraComp in SystemAPI.Query<RefRW<MainCameraComponent>>())
        {
            cameraComp.ValueRW.Position = mainCamera.transform.position;
            cameraComp.ValueRW.OrthographicSize = mainCamera.orthographic ? mainCamera.orthographicSize : 5f;
            cameraComp.ValueRW.Aspect = mainCamera.aspect;
            cameraComp.ValueRW.IsOrthographic = mainCamera.orthographic;
        }
    }
}
