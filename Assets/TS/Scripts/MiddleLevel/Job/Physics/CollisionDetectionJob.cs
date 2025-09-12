
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct CollisionDetectionJob : IJob
{
    [ReadOnly] public NativeArray<Entity> allEntities;
    [ReadOnly] public NativeArray<ColliderBoundsComponent> allBounds;
    [ReadOnly] public NativeArray<LightweightColliderComponent> allColliders;
    [NativeDisableParallelForRestriction] public BufferLookup<CollisionBuffer> collisionBufferLookup;
    public ComponentLookup<CollisionInfoComponent> collisionInfoLookup;
    [ReadOnly] public bool useSpacialHashing;
    [ReadOnly] public float cellSize;
    
    public void Execute()
    {
        // 모든 entity의 충돌 버퍼 초기화
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entity = allEntities[i];
            if (collisionBufferLookup.HasBuffer(entity))
            {
                var buffer = collisionBufferLookup[entity];
                buffer.Clear();
            }
            
            if (collisionInfoLookup.HasComponent(entity))
            {
                var info = collisionInfoLookup[entity];
                info.hasCollision = false;
                info.collidedEntity = Entity.Null;
                collisionInfoLookup[entity] = info;
            }
        }
        
        // 모든 entity 쌍에 대해 충돌 검사
        for (int i = 0; i < allEntities.Length; i++)
        {
            var entityA = allEntities[i];
            var boundsA = allBounds[i];
            var colliderA = allColliders[i];
            
            for (int j = i + 1; j < allEntities.Length; j++)
            {
                var entityB = allEntities[j];
                var boundsB = allBounds[j];
                var colliderB = allColliders[j];
                
                // 충돌 검사
                if (Utility.Physics.BoundsIntersect(boundsA, boundsB))
                {
                    float2 separationVector = new float2(0,0);
                    Utility.Physics.GetSeparationVector(boundsA, boundsB, out separationVector);
                    bool isTriggerCollision = colliderA.isTrigger || colliderB.isTrigger;
                    
                    // entityA에 충돌 데이터 추가
                    if (collisionBufferLookup.HasBuffer(entityA))
                    {
                        var bufferA = collisionBufferLookup[entityA];
                        bufferA.Add(new CollisionBuffer
                        {
                            collidedEntity = entityB,
                            separationVector = separationVector,
                            isTrigger = isTriggerCollision
                        });
                    }
                    
                    // entityB에 반대 방향 충돌 데이터 추가
                    if (collisionBufferLookup.HasBuffer(entityB))
                    {
                        var bufferB = collisionBufferLookup[entityB];
                        bufferB.Add(new CollisionBuffer
                        {
                            collidedEntity = entityA,
                            separationVector = -separationVector,
                            isTrigger = isTriggerCollision
                        });
                    }
                    
                    // CollisionInfo 업데이트
                    if (collisionInfoLookup.HasComponent(entityA))
                    {
                        var infoA = collisionInfoLookup[entityA];
                        infoA.hasCollision = true;
                        infoA.collidedEntity = entityB;
                        infoA.separationVector = separationVector;
                        collisionInfoLookup[entityA] = infoA;
                    }
                    
                    if (collisionInfoLookup.HasComponent(entityB))
                    {
                        var infoB = collisionInfoLookup[entityB];
                        infoB.hasCollision = true;
                        infoB.collidedEntity = entityA;
                        infoB.separationVector = -separationVector;
                        collisionInfoLookup[entityB] = infoB;
                    }
                }
            }
        }
    }
}