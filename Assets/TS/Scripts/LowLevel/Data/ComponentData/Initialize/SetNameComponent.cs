
using Unity.Collections;
using Unity.Entities;

public struct SetNameComponent : IComponentData
{
    public FixedString64Bytes Name;
}
