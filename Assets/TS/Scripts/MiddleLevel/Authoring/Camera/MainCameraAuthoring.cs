using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Authoring component for main camera
/// Add this to your main camera GameObject to expose it to ECS systems
/// </summary>
[RequireComponent(typeof(Camera))]
public class MainCameraAuthoring : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private class Baker : Baker<MainCameraAuthoring>
    {
        public override void Bake(MainCameraAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var cam = authoring.GetComponent<Camera>();

            AddComponent(entity, new MainCameraComponent
            {
                Position = authoring.transform.position,
                OrthographicSize = cam.orthographic ? cam.orthographicSize : 5f,
                Aspect = cam.aspect,
                IsOrthographic = cam.orthographic
            });
        }
    }
}
