using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class PhysicsAuthoring : MonoBehaviour
{
    [Header("Physics Settings")]
    public Vector2 velocity = Vector2.zero;
    public Vector2 gravity = new Vector2(0, -9.81f);
    public float mass = 1f;
    public float drag = 0.98f;
    public bool useGravity = true;
    public bool isStatic = false;
    
    private class Baker : Baker<PhysicsAuthoring>
    {
        public override void Bake(PhysicsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new LightweightPhysicsComponent
            {
                velocity = new float2(authoring.velocity.x, authoring.velocity.y),
                gravity = new float2(authoring.gravity.x, authoring.gravity.y),
                mass = authoring.mass,
                drag = authoring.drag,
                useGravity = authoring.useGravity,
                isGrounded = false,
                isStatic = authoring.isStatic
            });
            
            AddComponent(entity, new PhysicsEventsComponent());
            
            AddBuffer<TriggerBuffer>(entity);
        }
    }
}