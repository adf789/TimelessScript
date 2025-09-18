
using UnityEngine;
using Unity.Entities;

public class TSObjectAuthoring : MonoBehaviour
{
    [SerializeField] private BehaviorType type;
    
    private class Baker : Baker<TSObjectAuthoring>
    {
        public override void Bake(TSObjectAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new TSObjectInfoComponent()
            {
                Target = entity,
                Behavior = authoring.type
            });
        }

    }
}