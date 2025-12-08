
using UnityEngine;
using Unity.Entities;

public class TSGimmickAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Gimmick;
    public override ColliderLayer Layer => ColliderLayer.Gimmick;
    public override bool IsStatic => true;

    [SerializeField] private float radius;

    private class Baker : BaseObjectBaker<TSGimmickAuthoring>
    {
        protected override void BakeDerived(TSGimmickAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TSGimmickComponent()
            {
                Radius = authoring.radius,
            });
        }
    }

    protected override void OnDrawGizmosDerived()
    {
        Vector2 center = (Vector2) transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, radius);
    }
}