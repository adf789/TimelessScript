
using UnityEngine;
using Unity.Entities;

public class BehaviorAuthoring : MonoBehaviour
{
    public bool check = false;

    private class Baker : Baker<BehaviorAuthoring>
    {
        public override void Bake(BehaviorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new BehaviorComponent());
        }

    }
}