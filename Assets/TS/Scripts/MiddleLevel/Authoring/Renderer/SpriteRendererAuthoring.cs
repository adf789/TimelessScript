
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRendererAuthoring : MonoBehaviour
{
    public bool IsInitialized => spriteRenderer;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private class Baker : Baker<SpriteRendererAuthoring>
    {
        public override void Bake(SpriteRendererAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);

            authoring.Initialize();

            // authoring MonoBehaviour 인스턴스를 관리형 컴포넌트로 추가합니다.
            AddComponentObject(entity, authoring);

            // 부모 Transform이 있으면 Parent 컴포넌트 추가
            var parentTransform = authoring.transform.parent;
            if (parentTransform != null)
            {
                var parentEntity = GetEntity(parentTransform, TransformUsageFlags.Dynamic);
                AddComponent(entity, new Parent { Value = parentEntity });
                AddComponent(entity, new LocalTransform() { Position = authoring.transform.localPosition });
            }

            AddComponent(entity, new SpriteRendererComponent()
            {
                Layer = authoring.spriteRenderer.sortingOrder,
                IsFlip = authoring.spriteRenderer.flipX
            });
            AddComponent(entity, new ObjectTargetComponent());
        }

    }

    private void OnValidate()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (!IsInitialized)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetFlip(bool isFlip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlip;
        }
    }

    public void SetSize(Vector2 size)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.size = size;
        }
    }

    public void SetLayer(int layer)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = layer;
    }

    public void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
