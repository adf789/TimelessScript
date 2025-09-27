
using Unity.Entities;
using Unity.Mathematics;

public struct TriggerBuffer : IBufferElementData
{
    public Entity TriggerEntity;
    public bool IsEntering; // true면 Enter, false면 Exit
}