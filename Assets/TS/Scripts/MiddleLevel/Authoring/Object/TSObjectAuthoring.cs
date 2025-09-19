
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSObjectAuthoring : MonoBehaviour
{
    [SerializeField] private TSObjectType type;
    [SerializeField] private Transform root;

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
                    MovePosition = initialPosition,
                    Purpose = MoveState.None
                },
                RootOffset = authoring.GetRootOffset(),
            });
        }
    }

    public float GetRootOffset()
    {
        if (!root)
            return 0f;

        return root.localPosition.y;
    }
}