using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TSLadderAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Ladder;

    [Header("Ground Connection")]
    [Tooltip("상단 연결 지형")]
    public GameObject topConnectedGround;

    [Tooltip("하단 연결 지형")]
    public GameObject bottomConnectedGround;

    private class Baker : Baker<TSLadderAuthoring>
    {
        public override void Bake(TSLadderAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TSObjectComponent()
            {
                Name = authoring.name,
                Self = entity,
                ObjectType = authoring.Type,
                RootOffset = 0f,
            });

            // 사다리 특화 컴포넌트 추가
            AddComponent(entity, new TSLadderComponent
            {
                TopConnectedGround = authoring.topConnectedGround ? GetEntity(authoring.topConnectedGround, TransformUsageFlags.Dynamic) : Entity.Null,
                BottomConnectedGround = authoring.bottomConnectedGround ? GetEntity(authoring.bottomConnectedGround, TransformUsageFlags.Dynamic) : Entity.Null
            });

            // Collider 관련 컴포넌트들 (ColliderAuthoring이 없는 경우)
            if (!authoring.GetComponent<ColliderAuthoring>())
            {
                // ConnectedGround 위치를 기반으로 높이 계산
                float calculatedHeight = CalculateLadderHeight(authoring);

                AddComponent(entity, new ColliderComponent
                {
                    Layer = ColliderLayer.Ladder,
                    Size = new float2(0.5f, calculatedHeight),
                    Offset = new float2(0f, .5f),
                    IsTrigger = true, // 사다리는 트리거여야 캐릭터가 내부에서 움직일 수 있음
                    Position = float2.zero
                });

                AddComponent(entity, new SpatialHashKeyComponent());
                AddComponent(entity, new ColliderBoundsComponent());
                AddComponent(entity, new CollisionInfoComponent());
                AddBuffer<CollisionBuffer>(entity);
            }
        }

        private float CalculateLadderHeight(TSLadderAuthoring authoring)
        {
            float defaultHeight = 3.0f; // 기본 높이

            // TopConnectedGround와 BottomConnectedGround가 모두 있는 경우
            if (authoring.topConnectedGround && authoring.bottomConnectedGround)
            {
                float topY = authoring.topConnectedGround.transform.position.y;
                float bottomY = authoring.bottomConnectedGround.transform.position.y;
                float groundDistance = math.abs(topY - bottomY);

                // TopConnectedGround보다 1 높게 설정
                return groundDistance + 1.0f;
            }
            // TopConnectedGround만 있는 경우
            else if (authoring.topConnectedGround)
            {
                float topY = authoring.topConnectedGround.transform.position.y;
                float ladderY = authoring.transform.position.y;
                float distanceToTop = math.abs(topY - ladderY);

                // TopConnectedGround보다 1 높게 설정
                return distanceToTop + 1.0f;
            }
            // BottomConnectedGround만 있는 경우
            else if (authoring.bottomConnectedGround)
            {
                float bottomY = authoring.bottomConnectedGround.transform.position.y;
                float ladderY = authoring.transform.position.y;
                float distanceToBottom = math.abs(ladderY - bottomY);

                // 기본적으로 하단에서 위로 올라가는 높이 + 1
                return distanceToBottom + 1.0f;
            }

            return defaultHeight;
        }
    }

    void OnDrawGizmos()
    {
        // 계산된 높이로 사다리 시각화
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + new Vector3(0f, .5f);

        // 실제 계산된 높이 사용
        float calculatedHeight = CalculateGizmoHeight();
        Vector3 size = new Vector3(0.5f, calculatedHeight, 0.1f);
        Gizmos.DrawWireCube(center, size);

        // 연결된 지형 시각화
        Gizmos.color = Color.green;
        if (topConnectedGround)
        {
            Gizmos.DrawLine(transform.position, topConnectedGround.transform.position);
            Gizmos.DrawWireSphere(topConnectedGround.transform.position, 0.3f);
        }

        if (bottomConnectedGround)
        {
            Gizmos.DrawLine(transform.position, bottomConnectedGround.transform.position);
            Gizmos.DrawWireSphere(bottomConnectedGround.transform.position, 0.3f);
        }
    }

    private float CalculateGizmoHeight()
    {
        float defaultHeight = 3.0f; // 기본 높이

        // TopConnectedGround와 BottomConnectedGround가 모두 있는 경우
        if (topConnectedGround && bottomConnectedGround)
        {
            float topY = topConnectedGround.transform.position.y;
            float bottomY = bottomConnectedGround.transform.position.y;
            float groundDistance = Mathf.Abs(topY - bottomY);

            // TopConnectedGround보다 1 높게 설정
            return groundDistance + 1.0f;
        }
        // TopConnectedGround만 있는 경우
        else if (topConnectedGround)
        {
            float topY = topConnectedGround.transform.position.y;
            float ladderY = transform.position.y;
            float distanceToTop = Mathf.Abs(topY - ladderY);

            // TopConnectedGround보다 1 높게 설정
            return distanceToTop + 1.0f;
        }
        // BottomConnectedGround만 있는 경우
        else if (bottomConnectedGround)
        {
            float bottomY = bottomConnectedGround.transform.position.y;
            float ladderY = transform.position.y;
            float distanceToBottom = Mathf.Abs(ladderY - bottomY);

            // 기본적으로 하단에서 위로 올라가는 높이 + 1
            return distanceToBottom + 1.0f;
        }

        return defaultHeight;
    }
}