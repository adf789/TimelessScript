using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public struct GroundPreset
{
    public FixedString32Bytes name;
    public GroundType groundType;
    public float2 size;
    public float bounciness;
    public float friction;
    public bool isOneWayPlatform;
    public float4 gizmoColor;

    public bool IsNull => default(GroundPreset).Equals(this);
}

public struct GroundSetupComponent : IComponentData
{
    public int selectedPresetIndex;
    public bool autoSetupOnStart;
}

public struct GroundPresetBuffer : IBufferElementData
{
    public GroundPreset preset;
}