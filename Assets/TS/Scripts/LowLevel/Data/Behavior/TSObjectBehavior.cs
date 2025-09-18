using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct TSObjectBehavior
{
    public Entity Target;
    public float2 MovePosition;
    public MoveState Purpose;
}
