using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSGroundAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Ground;

    [Header("Ground Settings")]
    [SerializeField] private float _bounciness = 0.3f;
    [SerializeField] private float _friction = 0.8f;
    [SerializeField] private GroundType _groundType = GroundType.Normal;
    [SerializeField] private Vector2 _size = Vector2.one;
    [SerializeField] private Vector2 _offset = Vector2.zero;

    private class Baker : Baker<TSGroundAuthoring>
    {
        public override void Bake(TSGroundAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TSObjectComponent()
            {
                Name = authoring.name,
                Self = entity,
                ObjectType = authoring.Type,
                RootOffset = authoring.GetRootOffset(),
            });

            AddComponent(entity, new TSGroundComponent
            {
                Bounciness = authoring._bounciness,
                Friction = authoring._friction,
                GroundType = authoring._groundType
            });

            AddComponent(entity, PhysicsComponent.GetStaticPhysic(entity));

            AddComponent(entity, new ColliderComponent
            {
                Layer = ColliderLayer.Ground,
                Size = new float2(authoring._size.x, authoring._size.y),
                Offset = new float2(authoring._offset.x, authoring._offset.y),
                IsTrigger = false,
            });
        }
    }

    public void SetPosition(float x, float y)
    {
        transform.position = new Vector3(x, y);
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