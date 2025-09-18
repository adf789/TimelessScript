
using UnityEngine;
using Unity.Entities;

public class TSObjectAuthoring : MonoBehaviour
{
    [SerializeField] private TSObjectType type;
    [SerializeField] private Transform root;

    private class Baker : Baker<TSObjectAuthoring>
    {
        public override void Bake(TSObjectAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new TSObjectComponent()
            {
                Self = entity,
                ObjectType = authoring.type,
                Behavior = new TSObjectBehavior(),
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