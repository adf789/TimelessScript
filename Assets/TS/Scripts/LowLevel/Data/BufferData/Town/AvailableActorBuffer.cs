
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct AvailableActorBuffer : IBufferElementData
{
    public TSActorComponent Actor;
    public int Layer;

    public AvailableActorBuffer(TSActorComponent actor, int layer)
    {
        Actor = actor;
        Layer = layer;
    }
}
