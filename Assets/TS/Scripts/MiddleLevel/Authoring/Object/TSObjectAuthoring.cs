
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSObjectAuthoring : MonoBehaviour
{
    [SerializeField] private TSObjectType type;
    [SerializeField] private Transform root;
    [SerializeField] private float radius;

    private class Baker : Baker<TSObjectAuthoring>
    {
        public override void Bake(TSObjectAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            // 초기 위치 계산 (Transform + RootOffset)
            var initialPosition = new float2(authoring.transform.position.x, authoring.transform.position.y);
            initialPosition.y += authoring.GetRootOffset();

            AddComponent(entity, new TSObjectComponent()
            {
                Name = authoring.name,
                Self = entity,
                ObjectType = authoring.type,
                Behavior = new TSObjectBehavior()
                {
                    Target = Entity.Null,
                    TargetType = TSObjectType.None,
                    TargetPosition = float2.zero,
                    MovePosition = initialPosition,
                    MoveState = MoveState.None
                },
                RootOffset = authoring.GetRootOffset(),
                Radius = authoring.radius,
            });
        }
    }

    public float GetRootOffset()
    {
        if (!root)
            return 0f;

        return root.localPosition.y;
    }

    void OnDrawGizmos()
    {
        Vector2 center = (Vector2)transform.position;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, radius);
    }
}