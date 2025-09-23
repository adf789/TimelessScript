
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct CollectorComponent : IComponentData
{
    public NativeList<InteractComponent> InteractCollector;
}
