
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpriteRendererReferenceComponent : IComponentData
{
    public SpriteRenderer Renderer;

    public SpriteRendererReferenceComponent(SpriteRenderer renderer)
    {
        Renderer = renderer;
    }
}
