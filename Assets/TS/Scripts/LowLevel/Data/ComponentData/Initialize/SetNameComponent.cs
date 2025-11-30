
using Unity.Collections;
using Unity.Entities;

public struct SetNameComponent : IComponentData
{
    public FixedString64Bytes Name;

    public SetNameComponent(FixedString64Bytes name)
    {
        Name = name;
    }
}
