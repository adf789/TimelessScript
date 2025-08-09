using UnityEngine;

[System.Serializable]
public class ColliderPreset
{
    public string name;
    public Vector2 size;
    public Vector2 offset;
    public bool isTrigger;
}

public class LightweightPhysicsHelper : MonoBehaviour
{
    public ColliderPreset[] presets = new ColliderPreset[]
    {
        new ColliderPreset { name = "Player", size = new Vector2(1, 2), offset = Vector2.zero, isTrigger = false },
        new ColliderPreset { name = "Item", size = new Vector2(0.5f, 0.5f), offset = Vector2.zero, isTrigger = true },
        new ColliderPreset { name = "DamageZone", size = new Vector2(3, 3), offset = Vector2.zero, isTrigger = true }
    };
    
    [ContextMenu("Apply Player Preset")]
    void ApplyPlayerPreset() => ApplyPreset("Player");
    
    [ContextMenu("Apply Item Preset")]
    void ApplyItemPreset() => ApplyPreset("Item");
    
    [ContextMenu("Apply DamageZone Preset")]
    void ApplyDamageZonePreset() => ApplyPreset("DamageZone");
    
    void ApplyPreset(string presetName)
    {
        var preset = System.Array.Find(presets, p => p.name == presetName);
        if (preset != null)
        {
            var physics = GetComponent<LightweightPhysics2D>();
            if (physics != null)
            {
                physics.colliderSize = preset.size;
                physics.colliderOffset = preset.offset;
                physics.isTrigger = preset.isTrigger;
            }
        }
    }
}