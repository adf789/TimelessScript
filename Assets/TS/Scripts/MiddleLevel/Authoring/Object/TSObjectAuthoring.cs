
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSObjectAuthoring : MonoBehaviour
{
    public virtual TSObjectType Type => TSObjectType.None;
    public virtual ColliderLayer Layer => ColliderLayer.None;
    public virtual bool IsStatic => false;
    public virtual uint DataID => _dataId;
    public virtual Vector2 Size => _size;
    public virtual Vector2 Offset => _offset;

    [SerializeField] protected uint _dataId = 0;
    [SerializeField] protected Vector2 _size = Vector2.one;
    [SerializeField] protected Vector2 _offset = Vector2.zero;

    public float GetRootOffset()
    {
        return -_offset.y + _size.y * 0.5f;
    }

    public float2 GetRootPosition()
    {
        var initialPosition = new float2(transform.position.x, transform.position.y);

        initialPosition.y += GetRootOffset();

        return initialPosition;
    }

    public float3 GetWorldOffset()
    {
        return transform.position - transform.localPosition;
    }

    public abstract class BaseObjectBaker<T> : Baker<T> where T : TSObjectAuthoring
    {
        // 공통 ComponentData 추가 로직
        protected void BakeBase(T authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new SetNameComponent(authoring.name));

            AddComponent(entity, new TSObjectComponent()
            {
                Self = entity,
                ObjectType = authoring.Type,
                DataID = authoring.DataID,
                RootOffset = authoring.GetRootOffset(),
            });

            AddComponent(entity, new PhysicsComponent()
            {
                Entity = entity,
                Velocity = float2.zero,
                UseGravity = !authoring.IsStatic,
                IsPrevGrounded = false,
                IsRandingAnimation = false,
                IsGrounded = false,
                IsStatic = authoring.IsStatic
            });

            AddComponent(entity, new ColliderComponent
            {
                Layer = authoring.Layer,
                Size = new float2(authoring.Size.x, authoring.Size.y),
                Offset = new float2(authoring.Offset.x, authoring.Offset.y),
                IsTrigger = false,
            });

            AddComponent(entity, new ColliderBoundsComponent());
        }

        public override void Bake(T authoring)
        {
            // 기본 컴포넌트 추가
            BakeBase(authoring);

            // 자식에서 구현하도록 위임
            BakeDerived(authoring);
        }

        // 자식 Baker에서 오버라이드할 메서드
        protected abstract void BakeDerived(T authoring);
    }

    void OnDrawGizmos()
    {
        OnDrawGizmosBase();

        OnDrawGizmosDerived();
    }

    private void OnDrawGizmosBase()
    {
        Vector2 center = (Vector2) transform.position + Offset;

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(center, Size);
    }

    protected virtual void OnDrawGizmosDerived()
    {

    }
}