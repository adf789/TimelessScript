using UnityEngine;
using Unity.Entities;

public class GroundAuthoring : MonoBehaviour
{
    [Header("Ground Settings")]
    public float bounciness = 0.3f;
    public float friction = 0.8f;
    public bool isOneWayPlatform = false;
    public GroundType groundType = GroundType.Normal;
    
    private class Baker : Baker<GroundAuthoring>
    {
        public override void Bake(GroundAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new GroundComponent
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