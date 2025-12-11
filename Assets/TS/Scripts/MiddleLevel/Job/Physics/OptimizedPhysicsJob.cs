using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.Runtime.CompilerServices;

/// <summary>
/// 2D 플랫포머에 최적화된 통합 물리 Job
/// - Actor-Ground 전용 충돌
/// - 단일 패스 처리 (Bounds + 충돌 + 응답)
/// - Y축 우선 충돌 해결
/// - Burst 최대 최적화
/// </summary>
[BurstCompile(CompileSynchronously = true,
              OptimizeFor = OptimizeFor.Performance,
              FloatMode = FloatMode.Fast,
              FloatPrecision = FloatPrecision.Low)]
public partial struct OptimizedPhysicsJob : IJobEntity
{
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public NativeArray<Entity> GroundEntities;
    [ReadOnly] public NativeArray<ColliderBoundsComponent> ColliderBounds;
    [ReadOnly] public ComponentLookup<TSGroundComponent> GroundLookup;
    [ReadOnly] public ComponentLookup<TSObjectComponent> ObjectLookup;
    [ReadOnly] public ComponentLookup<ColliderComponent> ColliderLookup;

    public void Execute(
        ref PhysicsComponent physics,
        ref LocalTransform transform,
        ref ColliderBoundsComponent bounds,
        in ColliderComponent collider)
    {
        // Static 엔티티는 물리 처리 안함
        if (physics.IsStatic)
            return;

        // 1. 물리 시뮬레이션 (중력, 속도)
        var delta = ApplyPhysics(ref physics, ref transform, DeltaTime);

        // 2. Bounds 업데이트
        UpdateBounds(ref bounds, delta);

        // 3. 충돌 검사 및 응답 (Actor vs Ground)
        ResolveCollisions(ref physics, ref transform, ref bounds, collider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateBounds(ref ColliderBoundsComponent bounds, float2 delta)
    {
        bounds.Center += delta;
        bounds.Min += delta;
        bounds.Max += delta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float2 ApplyPhysics(ref PhysicsComponent physics, ref LocalTransform transform, float dt)
    {
        // 중력 적용
        if (physics.UseGravity && !physics.IsGrounded)
        {
            physics.Velocity += new float2(0, FloatDefine.PHYSICS_GRAVITY * dt);
        }

        // 위치 업데이트
        float2 delta = physics.Velocity * dt;
        float2 newPos = transform.Position.xy + delta;
        transform.Position = new float3(newPos.x, newPos.y, transform.Position.z);

        return delta;
    }

    [BurstCompile]
    private void ResolveCollisions(
        ref PhysicsComponent physics,
        ref LocalTransform transform,
        ref ColliderBoundsComponent actorBounds,
        in ColliderComponent actorCollider)
    {
        // Trigger는 충돌 응답 안함
        if (actorCollider.IsTrigger)
            return;

        if (physics.IsGrounded)
            return;

        // Actor vs Ground 충돌만 검사
        float2 delta = float2.zero;

        for (int i = 0; i < GroundEntities.Length; i++)
        {
            Entity groundEntity = GroundEntities[i];
            ColliderBoundsComponent groundBound = ColliderBounds[i];

            // Bounds 체크
            if (!BoundsIntersect(actorBounds, groundBound))
                continue;

            // Collider 정보 가져오기
            if (!ColliderLookup.HasComponent(groundEntity))
                continue;

            ColliderComponent groundCollider = ColliderLookup[groundEntity];

            // 레이어 체크
            if (!CheckActorGroundLayer(actorCollider.Layer, groundCollider.Layer))
                continue;

            // Ladder 영역 확인
            if (groundCollider.IsTrigger)
            {
                // 오브젝트 컴포넌트는 무조건 있어야 함. 없으면 에러 처리
                TSObjectComponent obj = ObjectLookup[groundEntity];

                // 사다리의 경우
                if (obj.ObjectType == TSObjectType.Ladder)
                    continue; // Ladder는 Trigger이므로 충돌 응답 스킵

                // 트리거는 응답 안함
                continue;
            }

            // 충돌 응답
            float2 separation = GetSeparationVector(actorBounds, groundBound);

            // Y축 우선 (착지 처리)
            if (math.abs(separation.y) > math.abs(separation.x))
            {
                // Y축 분리
                float2 currentPos = transform.Position.xy;
                currentPos.y += separation.y;
                transform.Position = new float3(currentPos.x, currentPos.y, transform.Position.z);
                delta.y += separation.y;

                // 속도 제거
                physics.Velocity.y = 0;

                // 착지 판정 (아래로 분리되는 경우)
                if (separation.y > 0)
                {
                    physics.IsGrounded = true;

                    if (!physics.IsPrevGrounded)
                        physics.IsRandingAnimation = true;
                }
            }
            else
            {
                // X축 분리
                float2 currentPos = transform.Position.xy;
                currentPos.x += separation.x;
                transform.Position = new float3(currentPos.x, currentPos.y, transform.Position.z);
                delta.x += separation.x;

                // X축 속도 감쇠
                physics.Velocity.x *= 0.5f;
            }
        }

        // Bounds 재계산 (위치 변경 후)
        UpdateBounds(ref actorBounds, delta);

        physics.IsPrevGrounded = physics.IsGrounded;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool BoundsIntersect(in ColliderBoundsComponent a, in ColliderBoundsComponent b)
    {
        return a.Min.x < b.Max.x && a.Max.x > b.Min.x &&
               a.Min.y < b.Max.y && a.Max.y > b.Min.y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float2 GetSeparationVector(in ColliderBoundsComponent actor, in ColliderBoundsComponent ground)
    {
        // 겹침 크기 계산
        float overlapX = math.min(actor.Max.x, ground.Max.x) - math.max(actor.Min.x, ground.Min.x);
        float overlapY = math.min(actor.Max.y, ground.Max.y) - math.max(actor.Min.y, ground.Min.y);

        // 최소 이동 거리 (MTV)
        if (overlapX < overlapY)
        {
            // X축 분리
            return new float2(actor.Center.x < ground.Center.x ? -overlapX : overlapX, 0);
        }
        else
        {
            // Y축 분리
            return new float2(0, actor.Center.y < ground.Center.y ? -overlapY : overlapY);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckActorGroundLayer(ColliderLayer actorLayer, ColliderLayer groundLayer)
    {
        // Actor는 Ground, Ladder, Gimmick과만 충돌
        if (actorLayer == ColliderLayer.Actor)
        {
            return groundLayer == ColliderLayer.Ground ||
                   groundLayer == ColliderLayer.Ladder ||
                   groundLayer == ColliderLayer.Gimmick;
        }
        return false;
    }
}
