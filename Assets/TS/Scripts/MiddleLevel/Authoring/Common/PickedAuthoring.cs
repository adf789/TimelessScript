
using UnityEngine;
using Unity.Entities;

public class PickedAuthoring : MonoBehaviour
{
    [SerializeField] private int order;

    private class Baker : Baker<PickedAuthoring>
    {
        public override void Bake(PickedAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new PickedComponent(){Order = authoring.order});
        }

    }
}