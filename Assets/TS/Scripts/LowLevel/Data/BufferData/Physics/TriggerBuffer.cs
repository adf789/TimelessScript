
using Unity.Entities;
using Unity.Mathematics;

public struct TriggerBuffer : IBufferElementData
{
    public Entity triggerEntity;
    public bool isEntering; // true면 Enter, false면 Exit
}