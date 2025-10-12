using Unity.Burst;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// 물리 시스템 성능 모니터링
/// - FPS 측정
/// - 엔티티 수 추적
/// - 충돌 검사 횟수 계산
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(OptimizedPhysicsSystem))]
[DisableAutoCreation]
public partial class PhysicsPerformanceMonitor : SystemBase
{
    private ProfilerMarker physicsMarker;
    private double lastUpdateTime;
    private int frameCount;
    private float fps;

    protected override void OnCreate()
    {
        physicsMarker = new ProfilerMarker("Physics.Performance");
        lastUpdateTime = SystemAPI.Time.ElapsedTime;

        RequireForUpdate<TSGroundComponent>();
    }

    protected override void OnUpdate()
    {
        physicsMarker.Begin();

        // FPS 계산
        frameCount++;
        double currentTime = SystemAPI.Time.ElapsedTime;
        double deltaTime = currentTime - lastUpdateTime;

        if (deltaTime >= 1.0) // 1초마다 업데이트
        {
            fps = frameCount / (float) deltaTime;
            frameCount = 0;
            lastUpdateTime = currentTime;

            // 엔티티 수 집계
            int actorCount = 0;
            int groundCount = 0;

            Entities
                .WithAll<PhysicsComponent, ColliderComponent>()
                .WithNone<TSGroundComponent>()
                .ForEach((Entity entity) => { actorCount++; })
                .WithoutBurst()
                .Run();

            Entities
                .WithAll<ColliderComponent, TSGroundComponent>()
                .ForEach((Entity entity) => { groundCount++; })
                .WithoutBurst()
                .Run();

            // 충돌 검사 횟수 계산
            int totalChecks = actorCount * groundCount;

            // 로그 출력
            Debug.Log($"[Physics Performance] FPS: {fps:F1} | Actors: {actorCount} | Grounds: {groundCount} | Checks: {totalChecks:N0}");
        }

        physicsMarker.End();
    }
}
