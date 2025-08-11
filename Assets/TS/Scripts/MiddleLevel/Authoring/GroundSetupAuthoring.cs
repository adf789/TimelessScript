using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public class GroundPresetMono
{
    public string name;
    public GroundType groundType;
    public Vector2 size;
    public float bounciness;
    public float friction;
    public bool isOneWayPlatform;
    public Color gizmoColor;
}

public class GroundSetupAuthoring : MonoBehaviour
{
    [Header("Ground Presets")]
    public GroundPresetMono[] groundPresets = new GroundPresetMono[]
    {
        new GroundPresetMono 
        { 
            name = "Normal Ground", 
            groundType = GroundType.Normal, 
            size = new Vector2(10, 1), 
            bounciness = 0.1f, 
            friction = 0.8f, 
            isOneWayPlatform = false,
            gizmoColor = new Color(0.6f, 0.3f, 0.1f, 1f) // brown
        },
        new GroundPresetMono 
        { 
            name = "Bouncy Platform", 
            groundType = GroundType.Bouncy, 
            size = new Vector2(3, 0.5f), 
            bounciness = 1.5f, 
            friction = 0.9f, 
            isOneWayPlatform = false,
            gizmoColor = Color.green
        },
        new GroundPresetMono 
        { 
            name = "One Way Platform", 
            groundType = GroundType.Normal, 
            size = new Vector2(5, 0.3f), 
            bounciness = 0.0f, 
            friction = 0.8f, 
            isOneWayPlatform = true,
            gizmoColor = Color.yellow
        },
        new GroundPresetMono 
        { 
            name = "Ice Ground", 
            groundType = GroundType.Slippery, 
            size = new Vector2(8, 1), 
            bounciness = 0.1f, 
            friction = 0.1f, 
            isOneWayPlatform = false,
            gizmoColor = Color.cyan
        }
    };
    
    [Header("Setup Options")]
    public int selectedPresetIndex = 0;
    public bool autoSetupOnStart = true;
    
    private class Baker : Baker<GroundSetupAuthoring>
    {
        public override void Bake(GroundSetupAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Setup Component 추가
            AddComponent(entity, new GroundSetupComponent
            {
                selectedPresetIndex = authoring.selectedPresetIndex,
                autoSetupOnStart = authoring.autoSetupOnStart
            });

            // Preset Buffer 추가
            var presetBuffer = AddBuffer<GroundPresetBuffer>(entity);
            
            foreach (var preset in authoring.groundPresets)
            {
                presetBuffer.Add(new GroundPresetBuffer
                {
                    preset = new GroundPreset
                    {
                        name = preset.name,
                        groundType = preset.groundType,
                        size = new float2(preset.size.x, preset.size.y),
                        bounciness = preset.bounciness,
                        friction = preset.friction,
                        isOneWayPlatform = preset.isOneWayPlatform,
                        gizmoColor = new float4(preset.gizmoColor.r, preset.gizmoColor.g, preset.gizmoColor.b, preset.gizmoColor.a)
                    }
                });
            }
        }
    }
    
    // Editor 메서드들을 ECS 호환으로 변경하려면 별도의 Editor 스크립트가 필요
    [ContextMenu("Setup as Normal Ground")]
    void SetupNormalGround() => ApplyPreset(0);
    
    [ContextMenu("Setup as Bouncy Platform")]
    void SetupBouncyPlatform() => ApplyPreset(1);
    
    [ContextMenu("Setup as One Way Platform")]
    void SetupOneWayPlatform() => ApplyPreset(2);
    
    [ContextMenu("Setup as Ice Ground")]
    void SetupIceGround() => ApplyPreset(3);
    
    void ApplyPreset(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= groundPresets.Length) return;
        
        selectedPresetIndex = presetIndex;
        var preset = groundPresets[presetIndex];
        
        // GroundAuthoring이 있는지 확인하고 설정
        var groundAuthoring = GetComponent<GroundAuthoring>();
        if (groundAuthoring == null)
            groundAuthoring = gameObject.AddComponent<GroundAuthoring>();
        
        groundAuthoring.groundType = preset.groundType;
        groundAuthoring.bounciness = preset.bounciness;
        groundAuthoring.friction = preset.friction;
        groundAuthoring.isOneWayPlatform = preset.isOneWayPlatform;
        
        Debug.Log($"{preset.name} 설정 완료!");
    }
}