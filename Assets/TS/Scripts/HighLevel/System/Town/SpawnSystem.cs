
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[DisableAutoCreation]
public partial struct SpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // SpawnConfigComponent가 있는 엔티티들을 쿼리
        EntityQuery spawnConfigQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<SpawnConfigComponent>(),
            ComponentType.ReadOnly<LocalTransform>()
        );

        // 스폰 설정이 없어도 시스템이 계속 실행되도록 RequireForUpdate 제거
        state.RequireForUpdate(spawnConfigQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentTime = (float) SystemAPI.Time.ElapsedTime;

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        // 없어진 엔티티 처리
        var spawnCleanupJob = new SpawnCleanupJob
        {
            currentTime = currentTime,
            entityLookup = state.GetEntityStorageInfoLookup(),
            spawnedObjectLookup = state.GetComponentLookup<SpawnedObjectComponent>(),
            tsObjectLookup = SystemAPI.GetComponentLookup<TSObjectComponent>(true),
            animationComponentLookup = SystemAPI.GetComponentLookup<SpriteSheetAnimationComponent>(true)
        };

        state.Dependency = spawnCleanupJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        // 스폰 요청 실행
        var spawnJob = new SpawnJob
        {
            currentTime = currentTime,
            ecb = ecb.AsParallelWriter()
        };

        state.Dependency = spawnJob.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        // 스폰 로직 실행 (스폰 설정이 있는 경우에만)
        var spawnExecutionJob = new SpawnExecutionJob
        {
            currentTime = currentTime,
            linkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true),
            availableActorLookup = SystemAPI.GetBufferLookup<AvailableActorBuffer>(false),
            spawnConfigLookup = SystemAPI.GetComponentLookup<SpawnConfigComponent>(false),
            ecb = ecb
        };

        state.Dependency = spawnExecutionJob.Schedule(state.Dependency);

        // 스폰된 오브젝트 애니메이션 연결
        var linkAnimationJob = new LinkRendererJob()
        {
            linkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true),
            rendererComponentLookup = SystemAPI.GetComponentLookup<SpriteRendererComponent>(false),
            targetComponentLookup = SystemAPI.GetComponentLookup<ObjectTargetComponent>(false),
            ecb = ecb
        };

        state.Dependency = linkAnimationJob.Schedule(state.Dependency);
    }
}