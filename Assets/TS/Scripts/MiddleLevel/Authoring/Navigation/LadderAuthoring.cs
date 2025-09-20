using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class LadderAuthoring : MonoBehaviour
{
    [Header("Ladder Settings")]
    [Tooltip("사다리의 높이 (자동 계산되지만 수동 설정 가능)")]
    public float ladderHeight = 3.0f;

    [Tooltip("사다리 이용 가능 여부")]
    public bool isUsable = true;

    [Tooltip("사다리 상단 연결 지점")]
    public Transform topConnectionPoint;

    [Tooltip("사다리 하단 연결 지점")]
    public Transform bottomConnectionPoint;

    [Header("Auto Setup")]
    [Tooltip("자동으로 필요한 컴포넌트들을 추가할지 여부")]
    public bool autoSetupComponents = true;

    private class Baker : Baker<LadderAuthoring>
    {
        public override void Bake(LadderAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 사다리 특화 컴포넌트 추가
            AddComponent(entity, new LadderComponent
            {
                Height = authoring.ladderHeight,
                IsUsable = authoring.isUsable,
                TopPosition = authoring.topConnectionPoint ?
                    new float2(authoring.topConnectionPoint.position.x, authoring.topConnectionPoint.position.y) :
                    new float2(authoring.transform.position.x, authoring.transform.position.y + authoring.ladderHeight * 0.5f),
                BottomPosition = authoring.bottomConnectionPoint ?
                    new float2(authoring.bottomConnectionPoint.position.x, authoring.bottomConnectionPoint.position.y) :
                    new float2(authoring.transform.position.x, authoring.transform.position.y - authoring.ladderHeight * 0.5f)
            });

            // 자동 설정이 활성화된 경우 필요한 컴포넌트들 추가
            if (authoring.autoSetupComponents)
            {
                // TSObjectComponent - Ladder 타입으로 설정
                AddComponent(entity, new TSObjectComponent
                {
                    Name = authoring.name,
                    Self = entity,
                    ObjectType = TSObjectType.Ladder,
                    Behavior = new TSObjectBehavior(),
                    RootOffset = 0f
                });

                // PickedComponent - 클릭 가능하도록 설정
                AddComponent(entity, new PickedComponent { Order = 1 });

                // Collider 관련 컴포넌트들 (ColliderAuthoring이 없는 경우)
                if (!authoring.GetComponent<ColliderAuthoring>())
                {
                    AddComponent(entity, new ColliderComponent
                    {
                        size = new float2(1.0f, authoring.ladderHeight),
                        offset = float2.zero,
                        isTrigger = true, // 사다리는 트리거여야 캐릭터가 내부에서 움직일 수 있음
                        position = float2.zero
                    });

                    AddComponent(entity, new ColliderBoundsComponent());
                    AddComponent(entity, new CollisionInfoComponent());
                    AddBuffer<CollisionBuffer>(entity);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        // 사다리 시각화
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(0.5f, ladderHeight, 0.1f);
        Gizmos.DrawWireCube(center, size);

        // 연결점 시각화
        Gizmos.color = Color.red;
        if (topConnectionPoint)
        {
            Gizmos.DrawWireSphere(topConnectionPoint.position, 0.2f);
            Gizmos.DrawLine(transform.position, topConnectionPoint.position);
        }

        if (bottomConnectionPoint)
        {
            Gizmos.DrawWireSphere(bottomConnectionPoint.position, 0.2f);
            Gizmos.DrawLine(transform.position, bottomConnectionPoint.position);
        }

        // 기본 연결점 표시 (Transform이 없는 경우)
        if (!topConnectionPoint)
        {
            Vector3 topPos = transform.position + Vector3.up * (ladderHeight * 0.5f);
            Gizmos.DrawWireSphere(topPos, 0.15f);
        }

        if (!bottomConnectionPoint)
        {
            Vector3 bottomPos = transform.position + Vector3.down * (ladderHeight * 0.5f);
            Gizmos.DrawWireSphere(bottomPos, 0.15f);
        }
    }
}