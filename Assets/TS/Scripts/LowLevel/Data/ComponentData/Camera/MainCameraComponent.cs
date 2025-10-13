using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Camera data for ECS systems
/// Tagged as main camera for rendering systems
/// </summary>
public struct MainCameraComponent : IComponentData
{
    public float3 Position;
    public float OrthographicSize;
    public float Aspect;
    public bool IsOrthographic;
}
