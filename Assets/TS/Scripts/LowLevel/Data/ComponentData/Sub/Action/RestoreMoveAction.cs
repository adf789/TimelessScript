
using Unity.Entities;
using Unity.Mathematics;

public struct RestoreMoveAction
{
    public Entity Target;
    public uint TargetDataID;
    public TSObjectType TargetType;
    public float2 Position;
}
