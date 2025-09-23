using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct TSObjectBehavior
{
    public Entity Target;
    public int TargetDataID;
    public TSObjectType TargetType;
    public float2 TargetPosition;
    public float2 MovePosition;
    public MoveState MoveState;
}
