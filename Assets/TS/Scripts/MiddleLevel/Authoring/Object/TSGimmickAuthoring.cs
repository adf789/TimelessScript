
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSGimmickAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Gimmick;

    [SerializeField] private uint gimmickID;
    [SerializeField] private float radius;

    private class Baker : Baker<TSGimmickAuthoring>
    {
        public override void Bake(TSGimmickAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TSObjectComponent()
            {
                Name = authoring.name,
                Self = entity,
                DataID = authoring.gimmickID,
                ObjectType = authoring.Type,
                RootOffset = authoring.GetRootOffset(),
            });

            AddComponent(entity, new TSGimmickComponent()
            {
                Radius = authoring.radius,
            });
        }
    }
    
    void OnDrawGizmos()
    {
        Vector2 center = (Vector2)transform.position;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, radius);
    }
}