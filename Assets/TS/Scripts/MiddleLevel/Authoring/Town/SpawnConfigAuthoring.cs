using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class SpawnConfigAuthoring : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private GameObject spawnObjectPrefab;
    [SerializeField] private int maxSpawnCount = 10;
    [SerializeField] private float spawnCooldown = 2.0f;
    [SerializeField] private float minSpawnDistance = 1.0f;
    [SerializeField] private bool autoSpawn = true;

    private class Baker : Baker<SpawnConfigAuthoring>
    {
        public override void Bake(SpawnConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // SpawnConfigComponent 추가
            AddComponent(entity, new SpawnConfigComponent
            {
                spawnObjectPrefab = GetEntity(authoring.spawnObjectPrefab, TransformUsageFlags.Dynamic),
                maxSpawnCount = authoring.maxSpawnCount,
                currentSpawnCount = 0,
                spawnCooldown = authoring.spawnCooldown,
                nextSpawnTime = 0f,
                minSpawnDistance = authoring.minSpawnDistance,
                autoSpawn = authoring.autoSpawn
            });

            // SpawnAreaComponent 추가
            var worldPosition = authoring.transform.position;

            // 스폰된 엔티티들을 추적하기 위한 버퍼 추가
            AddBuffer<SpawnedEntityBuffer>(entity);
        }
    }
}