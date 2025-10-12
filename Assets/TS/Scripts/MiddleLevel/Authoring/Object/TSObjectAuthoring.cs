
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSObjectAuthoring : MonoBehaviour
{
    public virtual TSObjectType Type => TSObjectType.None;

    public float GetRootOffset()
    {
        var collider = GetComponent<ColliderAuthoring>();

        if (!collider)
            return 0f;

        return -collider.offset.y + collider.size.y * 0.5f;
    }

    public float2 GetRootPosition()
    {
        var initialPosition = new float2(transform.position.x, transform.position.y);

        initialPosition.y += GetRootOffset();

        return initialPosition;
    }
}