using UnityEngine;
using Unity.Entities;

public class TSGroundAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Ground;

    [Header("Ground Settings")]
    public float bounciness = 0.3f;
    public float friction = 0.8f;
    public bool isOneWayPlatform = false;
    public GroundType groundType = GroundType.Normal;

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
                bounciness = authoring.bounciness,
                friction = authoring.friction,
                isOneWayPlatform = authoring.isOneWayPlatform,
                groundType = authoring.groundType
            });

            // 초기 충돌 데이터
            AddComponent(entity, new GroundCollisionComponent());
        }
    }
}