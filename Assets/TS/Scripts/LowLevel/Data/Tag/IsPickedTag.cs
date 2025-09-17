// Assets/TS/Scripts/LowLevel/Data/Tag/IsPickedTag.cs
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// 현재 선택된 엔티티를 나타내는 태그입니다.
/// </summary>
public struct IsPickedTag : IComponentData
{
    public float2 TouchPosition;
}
