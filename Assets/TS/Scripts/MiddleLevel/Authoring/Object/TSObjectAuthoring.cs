
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSObjectAuthoring : MonoBehaviour
{
    public virtual TSObjectType Type => TSObjectType.None;

    [SerializeField] protected Transform root;

    public float GetRootOffset()
    {
        if (!root)
            return 0f;

        return root.localPosition.y;
    }

    public float2 GetRootPosition()
    {
        var initialPosition = new float2(transform.position.x, transform.position.y);

        initialPosition.y += GetRootOffset();

        return initialPosition;
    }
}