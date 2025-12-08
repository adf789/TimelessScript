
using UnityEngine;
using Unity.Entities;

public class TSActorAuthoring : TSObjectAuthoring
{
    public override ColliderLayer Layer => ColliderLayer.Actor;
    public override TSObjectType Type => TSObjectType.Actor;

    [SerializeField] private GameObject _select;

    private class Baker : BaseObjectBaker<TSActorAuthoring>
    {
        protected override void BakeDerived(TSActorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TSActorComponent()
            {
                LifePassingTime = 0,
                Move = new MoveAction()
                {
                    Target = Entity.Null,
                    TargetDataID = 0,
                    TargetType = TSObjectType.None,
                    MovePosition = authoring.GetRootPosition(),
                    MoveState = MoveState.None
                },
            });

            AddBuffer<InteractBuffer>(entity);

            // 베이킹 시 GameObject 참조를 명시적으로 등록
            if (authoring._select != null)
            {
                DependsOn(authoring._select);  // 의존성 명시

                AddComponent(entity, new SelectVisualComponent()
                {
                    SelectVisual = GetEntity(authoring._select, TransformUsageFlags.None)
                });
            }
        }
    }
}