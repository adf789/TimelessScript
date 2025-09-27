using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct GroundPresetBuffer : IBufferElementData
{
    public FixedString32Bytes Name;
    public GroundType GroundType;
    public float2 Size;
    public float Bounciness;
    public float Friction;
    public bool IsOneWayPlatform;
    public float4 GizmoColor;
}