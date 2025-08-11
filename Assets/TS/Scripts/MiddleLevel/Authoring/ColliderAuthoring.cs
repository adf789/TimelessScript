using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class ColliderAuthoring : MonoBehaviour
{
    [Header("Collider Settings")]
    public Vector2 size = Vector2.one;
    public Vector2 offset = Vector2.zero;
    public bool isTrigger = false;
    
    private class Baker : Baker<ColliderAuthoring>
    {
        public override void Bake(ColliderAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new LightweightColliderComponent
            {
                size = new float2(authoring.size.x, authoring.size.y),
                offset = new float2(authoring.offset.x, authoring.offset.y),
                isTrigger = authoring.isTrigger,
                position = float2.zero // 런타임에 업데이트
            });
            
            AddComponent(entity, new ColliderBounds());
            AddComponent(entity, new CollisionInfo());
            AddBuffer<CollisionBuffer>(entity);
        }
    }
    
    void OnDrawGizmos()
    {
        Vector2 center = (Vector2)transform.position + offset;
        
        if (isTrigger)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
        else
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
}