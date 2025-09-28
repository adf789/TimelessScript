using Unity.Entities;
using Unity.Mathematics;

public struct GroundContactResult
{
    public Entity GroundEntity;
    public float2 ContactPoint;
    public float Distance;
}