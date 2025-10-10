
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct AvailableActorBuffer : IBufferElementData
{
    public TSActorComponent Actor;

    public AvailableActorBuffer(TSActorComponent actor)
    {
        Actor = actor;
    }
}
