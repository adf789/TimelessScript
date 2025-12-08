using UnityEngine;
using Unity.Entities;

public class TSGroundAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Ground;
    public override ColliderLayer Layer => ColliderLayer.Ground;
    public override bool IsStatic => true;

    [Header("Ground Settings")]
    [SerializeField] private float _bounciness = 0.3f;
    [SerializeField] private float _friction = 0.8f;
    [SerializeField] private GroundType _groundType = GroundType.Normal;

    private class Baker : BaseObjectBaker<TSGroundAuthoring>
    {
        protected override void BakeDerived(TSGroundAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TSGroundComponent
            {
                Bounciness = authoring._bounciness,
                Friction = authoring._friction,
                GroundType = authoring._groundType
            });
        }
    }

    public void SetPosition(float x, float y)
    {
        transform.localPosition = new Vector3(x, y);
    }

    public void SetSize(int x, int y)
    {
        _size.x = x;
        _size.y = y;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector2 center = (Vector2) transform.position + _offset;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, _size);
    }
#endif
}