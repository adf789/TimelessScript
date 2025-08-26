using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct GroundPresetBuffer : IBufferElementData
{
    public FixedString32Bytes name;
    public GroundType groundType;
    public float2 size;
    public float bounciness;
    public float friction;
    public bool isOneWayPlatform;
    public float4 gizmoColor;
}