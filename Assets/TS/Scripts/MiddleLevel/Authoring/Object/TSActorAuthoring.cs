
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TSActorAuthoring : TSObjectAuthoring
{
    public override TSObjectType Type => TSObjectType.Actor;

    [SerializeField] private float lifeTime = 10f;

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
                LifeTime = authoring.lifeTime,
                LifePassingTime = 0,
                Move = new MoveAction()
                {
                    Target = Entity.Null,
                    TargetDataID = 0,
                    TargetType = TSObjectType.None,
                    TargetPosition = float2.zero,
                    MovePosition = authoring.GetRootPosition(),
                    MoveState = MoveState.None
                },
            });
        }
    }
}