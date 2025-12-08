using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TSLadderAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Ladder;
    public override ColliderLayer Layer => ColliderLayer.Ladder;
    public override bool IsStatic => true;
    public override Vector2 Size => new Vector2(0.5f, CalculateLadderHeight());
    public override Vector2 Offset => new Vector2(0f, .5f);

    [Header("Ground Connection")]
    [SerializeField] private TSGroundAuthoring _firstConnectedGround;
    [SerializeField] private TSGroundAuthoring _secondConnectedGround;

    private class Baker : BaseObjectBaker<TSLadderAuthoring>
    {
        protected override void BakeDerived(TSLadderAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 사다리 특화 컴포넌트 추가
            var topGround = authoring.GetTopConnectGround();
            var bottomGround = authoring.GetBottomConnectGround();
            AddComponent(entity, new TSLadderComponent
            {
                TopConnectedGround = topGround ? GetEntity(topGround, TransformUsageFlags.Dynamic) : Entity.Null,
                BottomConnectedGround = bottomGround ? GetEntity(
                    bottomGround, TransformUsageFlags.Dynamic) : Entity.Null
            });

            AddBuffer<CollisionBuffer>(entity);
        }
    }

    public void SetFirstConnectGround(TSGroundAuthoring ground)
    {
        _firstConnectedGround = ground;
    }

    public void SetSecondConnectGround(TSGroundAuthoring ground)
    {
        _secondConnectedGround = ground;
    }

    public TSGroundAuthoring GetTopConnectGround()
    {
        // 내림차순 정리
        return GetConnectGround((ground1, ground2) => ground2.transform.position.y.CompareTo(ground1.transform.position.y));
    }

    public TSGroundAuthoring GetBottomConnectGround()
    {
        // 오름차순 정리
        return GetConnectGround((ground1, ground2) => ground1.transform.position.y.CompareTo(ground2.transform.position.y));
    }

    public float CalculateLadderHeight()
    {
        float defaultHeight = 3.0f; // 기본 높이
        var topGround = GetTopConnectGround();
        var bottomGround = GetBottomConnectGround();

        // TopConnectedGround와 BottomConnectedGround가 모두 있는 경우
        if (topGround && bottomGround)
        {
            float topY = topGround.transform.position.y;
            float bottomY = bottomGround.transform.position.y;
            float groundDistance = math.abs(topY - bottomY);

            // TopConnectedGround보다 1 높게 설정
            return groundDistance + 1.0f;
        }
        // TopConnectedGround만 있는 경우
        else if (topGround)
        {
            float topY = topGround.transform.position.y;
            float ladderY = transform.position.y;
            float distanceToTop = math.abs(topY - ladderY);

            // TopConnectedGround보다 1 높게 설정
            return distanceToTop + 1.0f;
        }
        // BottomConnectedGround만 있는 경우
        else if (bottomGround)
        {
            float bottomY = bottomGround.transform.position.y;
            float ladderY = transform.position.y;
            float distanceToBottom = math.abs(ladderY - bottomY);

            // 기본적으로 하단에서 위로 올라가는 높이 + 1
            return distanceToBottom + 1.0f;
        }

        return defaultHeight;
    }

    private TSGroundAuthoring GetConnectGround(System.Comparison<TSGroundAuthoring> comparison)
    {
        if (comparison == null)
            return null;

        if (_firstConnectedGround && _secondConnectedGround)
        {
            int value = comparison(_firstConnectedGround, _secondConnectedGround);

            if (value < 0)
                return _firstConnectedGround;
            else
                return _secondConnectedGround;
        }
        else if (_firstConnectedGround)
        {
            return _firstConnectedGround;
        }
        else if (_secondConnectedGround)
        {
            return _secondConnectedGround;
        }
        else
        {
            return null;
        }
    }

#if UNITY_EDITOR
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
        var topGround = GetTopConnectGround();
        var bottomGround = GetBottomConnectGround();
        if (topGround)
        {
            Gizmos.DrawLine(transform.position, topGround.transform.position);
            Gizmos.DrawWireSphere(topGround.transform.position, 0.3f);
        }

        if (bottomGround)
        {
            Gizmos.DrawLine(transform.position, bottomGround.transform.position);
            Gizmos.DrawWireSphere(bottomGround.transform.position, 0.3f);
        }
    }

    private float CalculateGizmoHeight()
    {
        float defaultHeight = 3.0f; // 기본 높이
        var topGround = GetTopConnectGround();
        var bottomGround = GetBottomConnectGround();

        // TopConnectedGround와 BottomConnectedGround가 모두 있는 경우
        if (topGround && bottomGround)
        {
            float topY = topGround.transform.position.y;
            float bottomY = bottomGround.transform.position.y;
            float groundDistance = Mathf.Abs(topY - bottomY);

            // TopConnectedGround보다 1 높게 설정
            return groundDistance + 1.0f;
        }
        // TopConnectedGround만 있는 경우
        else if (topGround)
        {
            float topY = topGround.transform.position.y;
            float ladderY = transform.position.y;
            float distanceToTop = Mathf.Abs(topY - ladderY);

            // TopConnectedGround보다 1 높게 설정
            return distanceToTop + 1.0f;
        }
        // BottomConnectedGround만 있는 경우
        else if (bottomGround)
        {
            float bottomY = bottomGround.transform.position.y;
            float ladderY = transform.position.y;
            float distanceToBottom = Mathf.Abs(ladderY - bottomY);

            // 기본적으로 하단에서 위로 올라가는 높이 + 1
            return distanceToBottom + 1.0f;
        }

        return defaultHeight;
    }
#endif
}