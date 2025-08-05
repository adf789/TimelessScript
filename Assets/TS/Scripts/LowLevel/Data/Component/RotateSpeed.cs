using Unity.Entities;
using UnityEngine;

public struct RotateSpeed : IComponentData
{
    public float value;

    public static RotateSpeed Random(uint seed)
        => new RotateSpeed() { value = new Unity.Mathematics.Random(seed).NextFloat(100, 200) };
}
