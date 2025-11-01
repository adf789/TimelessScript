
using Unity.Entities;
using UnityEngine;

// Managed Component - class + IComponentData = Managed Component
public struct SelectVisualComponent : IComponentData
{
    public Entity SelectVisual;
}
