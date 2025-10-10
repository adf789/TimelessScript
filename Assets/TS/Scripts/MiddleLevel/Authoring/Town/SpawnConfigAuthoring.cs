using UnityEngine;
using Unity.Entities;

public class SpawnConfigAuthoring : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private TSObjectAuthoring spawnObjectPrefab;
    [SerializeField] private float lifeTime = 0;
    [SerializeField] private int layerOffset = 0;
    [SerializeField] private int maxSpawnCount = 10;
    [SerializeField] private float spawnCooldown = 2.0f;
    [SerializeField] private float minSpawnDistance = 1.0f;

    private class Baker : Baker<SpawnConfigAuthoring>
    {
        public override void Bake(SpawnConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            string spawnName = authoring.spawnObjectPrefab != null ? authoring.spawnObjectPrefab.name : authoring.name;
            var spawnEntity = GetEntity(authoring.spawnObjectPrefab.gameObject, TransformUsageFlags.Dynamic);
            var objectType = authoring.spawnObjectPrefab.Type;

            // SpawnConfigComponent 추가
            AddComponent(entity, new SpawnConfigComponent
            {
                SpawnObjectPrefab = spawnEntity,
                Name = spawnName,
                ObjectType = objectType,
                LayerOffset = authoring.layerOffset,
                MaxSpawnCount = authoring.maxSpawnCount,
                ReadySpawnCount = 0,
                CurrentSpawnCount = 0,
                LifeTime = authoring.lifeTime,
                SpawnCooldown = authoring.spawnCooldown,
                NextSpawnTime = 0f,
                MinSpawnDistance = authoring.minSpawnDistance,
                PositionYOffset = -authoring.spawnObjectPrefab.GetRootOffset()
            });

            // 스폰된 엔티티들을 추적하기 위한 버퍼 추가
            AddBuffer<SpawnedEntityBuffer>(entity);

            // 재사용 가능한 Layer를 관리하기 위한 버퍼 추가
            AddBuffer<AvailableLayerBuffer>(entity);

            // 재사용 가능한 Layer를 관리하기 위한 버퍼 추가
            AddBuffer<AvailableActorBuffer>(entity);
        }
    }
}