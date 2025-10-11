using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Spatial Hashing을 사용하여 충돌 가능한 엔티티 쌍을 수집하는 Job (병렬 처리)
/// 각 엔티티가 차지하는 셀에서 다른 엔티티를 찾아 쌍을 큐에 추가합니다.
/// 중복은 허용되며, 나중에 제거됩니다.
/// </summary>
[BurstCompile]
public struct CollectCollisionPairsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<SpatialHashKeyComponent> allHashKeys;
    [ReadOnly] public NativeParallelMultiHashMap<int2, int> spatialHash;

    public NativeQueue<CollisionPair>.ParallelWriter pairQueue;

    public void Execute(int entityAIndex)
    {
        var hashKeyA = allHashKeys[entityAIndex];

        // 해당 엔티티가 차지하는 모든 셀 검사
        for (int x = hashKeyA.MinCell.x; x <= hashKeyA.MaxCell.x; x++)
        {
            for (int y = hashKeyA.MinCell.y; y <= hashKeyA.MaxCell.y; y++)
            {
                var cell = new int2(x, y);

                if (spatialHash.TryGetFirstValue(cell, out int entityBIndex, out var iterator))
                {
                    do
                    {
                        // 자기 자신은 건너뛰기
                        if (entityAIndex == entityBIndex)
                            continue;

                        // 순서 보장 (A < B)으로 일부 중복 방지
                        if (entityAIndex < entityBIndex)
                        {
                            pairQueue.Enqueue(new CollisionPair
                            {
                                IndexA = entityAIndex,
                                IndexB = entityBIndex
                            });
                        }

                    } while (spatialHash.TryGetNextValue(out entityBIndex, ref iterator));
                }
            }
        }
    }
}
