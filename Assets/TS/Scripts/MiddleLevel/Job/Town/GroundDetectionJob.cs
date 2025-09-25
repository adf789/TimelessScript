using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public struct GroundDetectionJob : IJob
{
    [ReadOnly] public NativeArray<Entity> groundEntities;
    [ReadOnly] public ComponentLookup<TSGroundComponent> groundLookup;
    [ReadOnly] public ComponentLookup<ColliderComponent> colliderLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;

    public float2 checkPosition;
    public float checkDistance;
    public NativeReference<float2> resultPosition;
    public NativeReference<bool> foundGround;

    public void Execute()
    {
        foundGround.Value = false;
        float closestDistance = float.MaxValue;
        float2 closestGroundPosition = float2.zero;

        for (int i = 0; i < groundEntities.Length; i++)
        {
            var groundEntity = groundEntities[i];

            if (!colliderLookup.HasComponent(groundEntity) || !transformLookup.HasComponent(groundEntity))
                continue;

            var collider = colliderLookup[groundEntity];
            var transform = transformLookup[groundEntity];

            // 지면의 콜라이더 영역 계산
            float2 groundCenter = transform.Position.xy + collider.offset;
            float2 groundMin = groundCenter - collider.size * 0.5f;
            float2 groundMax = groundCenter + collider.size * 0.5f;

            // X축이 겹치는지 확인
            if (checkPosition.x >= groundMin.x && checkPosition.x <= groundMax.x)
            {
                // Y축 거리 계산 (지면 위쪽 면까지의 거리)
                float groundTopY = groundMax.y;
                float distanceToGround = math.abs(checkPosition.y - groundTopY);

                // 체크 거리 내에 있고 가장 가까운 지면인지 확인
                if (distanceToGround <= checkDistance && distanceToGround < closestDistance)
                {
                    closestDistance = distanceToGround;
                    closestGroundPosition = new float2(checkPosition.x, groundTopY);
                    foundGround.Value = true;
                }
            }
        }

        if (foundGround.Value)
        {
            resultPosition.Value = closestGroundPosition;
        }
    }
}