using UnityEngine;

[System.Serializable]
public class GroundPreset
{
    public string name;
    public GroundType groundType;
    public Vector2 size;
    public float bounciness;
    public float friction;
    public bool isOneWayPlatform;
    public Color gizmoColor;
}

public class GroundSetup : MonoBehaviour
{
    public GroundPreset[] groundPresets = new GroundPreset[]
    {
        new GroundPreset 
        { 
            name = "Normal Ground", 
            groundType = GroundType.Normal, 
            size = new Vector2(10, 1), 
            bounciness = 0.1f, 
            friction = 0.8f, 
            isOneWayPlatform = false,
            gizmoColor = Color.brown
        },
        new GroundPreset 
        { 
            name = "Bouncy Platform", 
            groundType = GroundType.Bouncy, 
            size = new Vector2(3, 0.5f), 
            bounciness = 1.5f, 
            friction = 0.9f, 
            isOneWayPlatform = false,
            gizmoColor = Color.green
        },
        new GroundPreset 
        { 
            name = "One Way Platform", 
            groundType = GroundType.Normal, 
            size = new Vector2(5, 0.3f), 
            bounciness = 0.0f, 
            friction = 0.8f, 
            isOneWayPlatform = true,
            gizmoColor = Color.yellow
        },
        new GroundPreset 
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
    
    [ContextMenu("Setup as Normal Ground")]
    void SetupNormalGround() => ApplyPreset("Normal Ground");
    
    [ContextMenu("Setup as Bouncy Platform")]
    void SetupBouncyPlatform() => ApplyPreset("Bouncy Platform");
    
    [ContextMenu("Setup as One Way Platform")]
    void SetupOneWayPlatform() => ApplyPreset("One Way Platform");
    
    [ContextMenu("Setup as Ice Ground")]
    void SetupIceGround() => ApplyPreset("Ice Ground");
    
    void ApplyPreset(string presetName)
    {
        var preset = System.Array.Find(groundPresets, p => p.name == presetName);
        if (preset == null) return;
        
        // LightweightPhysics2D 추가/설정
        var physics = GetComponent<LightweightPhysics2D>();
        if (physics == null)
            physics = gameObject.AddComponent<LightweightPhysics2D>();
        
        physics.colliderSize = preset.size;
        physics.isTrigger = false;
        physics.useGravity = false;
        physics.mass = float.MaxValue;
        
        // GroundObject 추가/설정
        var ground = GetComponent<GroundObject>();
        if (ground == null)
            ground = gameObject.AddComponent<GroundObject>();
        
        ground.groundType = preset.groundType;
        ground.bounciness = preset.bounciness;
        ground.friction = preset.friction;
        ground.isOneWayPlatform = preset.isOneWayPlatform;
        
        // 태그 설정
        gameObject.tag = "Ground";
        
        Debug.Log($"{presetName} 설정 완료!");
    }
    
    void OnDrawGizmos()
    {
        var preset = System.Array.Find(groundPresets, p => p.name == "Normal Ground");
        var ground = GetComponent<GroundObject>();
        
        if (ground != null)
        {
            var matchingPreset = System.Array.Find(groundPresets, p => p.groundType == ground.groundType);
            if (matchingPreset != null)
                preset = matchingPreset;
        }
        
        Gizmos.color = preset?.gizmoColor ?? Color.brown;
        
        var physics = GetComponent<LightweightPhysics2D>();
        Vector2 size = physics != null ? physics.colliderSize : Vector2.one;
        
        Gizmos.DrawCube(transform.position, size);
        
        // 일방통행 플랫폼 표시
        if (ground != null && ground.isOneWayPlatform)
        {
            Gizmos.color = Color.red;
            Vector3 start = transform.position + Vector3.left * size.x * 0.4f;
            Vector3 end = transform.position + Vector3.right * size.x * 0.4f;
            Gizmos.DrawLine(start, end);
            
            // 화살표
            Gizmos.DrawLine(end, end + Vector3.up * 0.2f + Vector3.left * 0.1f);
            Gizmos.DrawLine(end, end + Vector3.up * 0.2f + Vector3.right * 0.1f);
        }
    }
}