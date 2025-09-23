
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial struct SpawnSystem : ISystem
{
    private EntityQuery _spawnConfigQuery;
    private EntityQuery _spawnRequestQuery;

    public void OnCreate(ref SystemState state)
    {
        // SpawnConfigComponent가 있는 엔티티들을 쿼리
        _spawnConfigQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<SpawnConfigComponent>(),
            ComponentType.ReadOnly<LocalTransform>()
        );

        // SpawnRequestComponent가 있는 엔티티들을 쿼리
        _spawnRequestQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<SpawnRequestComponent>()
        );

        // 스폰 설정이 없어도 시스템이 계속 실행되도록 RequireForUpdate 제거
        state.RequireForUpdate(_spawnConfigQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float) SystemAPI.Time.ElapsedTime;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 스폰 로직 실행 (스폰 설정이 있는 경우에만)
        var spawnJob = new SpawnJob
        {
            currentTime = currentTime,
            spawnedEntityBufferLookup = SystemAPI.GetBufferLookup<SpawnedEntityBuffer>(true),
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            ecb = ecb.AsParallelWriter()
        };

        state.Dependency = spawnJob.ScheduleParallel(state.Dependency);

        state.Dependency.Complete();

        // 스폰 요청 실행
        var spawnExecutionJob = new SpawnExecutionJob
        {
            currentTime = currentTime,
            objectLookup = SystemAPI.GetComponentLookup<TSObjectComponent>(true),
            spawnConfigLookup = SystemAPI.GetComponentLookup<SpawnConfigComponent>(true),
            ecb = ecb.AsParallelWriter()
        };

        state.Dependency = spawnExecutionJob.ScheduleParallel(state.Dependency);
    }
}