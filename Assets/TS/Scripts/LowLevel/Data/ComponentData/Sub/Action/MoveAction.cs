using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct MoveAction
{
    public Entity Target;
    public uint TargetDataID;
    public TSObjectType TargetType;
    public float2 MovePosition;
    public MoveState MoveState;
}
