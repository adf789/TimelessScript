using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
        => state.RequireForUpdate<Config>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        var instances = state.EntityManager.Instantiate(config.prefab, config.spawnCount, Allocator.Temp);

        var rand = new Unity.Mathematics.Random(config.randomSeed);
        foreach(var entity in instances)
        {
            var lForm = SystemAPI.GetComponentRW<LocalTransform>(entity);
            var rotate = SystemAPI.GetComponentRW<RotateSpeed>(entity);

            lForm.ValueRW = LocalTransform.FromPositionRotation
                (rand.NextFloat3() * config.spawnRadius, rand.NextQuaternionRotation().value);

            rotate.ValueRW = RotateSpeed.Random(config.randomSeed);
        }

        // 1회만 실행
        state.Enabled = false;
    }
}
