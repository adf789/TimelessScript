
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSActorAuthoring : TSObjectAuthoring
{
    [SerializeField] private GameObject _select;

    public override TSObjectType Type => TSObjectType.Actor;

    private class Baker : Baker<TSActorAuthoring>
    {
        public override void Bake(TSActorAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new TSObjectComponent()
            {
                Name = authoring.name,
                Self = entity,
                ObjectType = authoring.Type,
                RootOffset = authoring.GetRootOffset(),
            });

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