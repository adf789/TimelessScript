
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
public partial struct BehaviorJob : IJobEntity
{
    [NativeDisableParallelForRestriction]
    public ComponentLookup<SpriteSheetAnimationComponent> AnimationComponentLookup;
    public float Speed;
    public float DeltaTime;

    public void Execute(Entity entity,
    ref LocalTransform transform,
    ref TSObjectComponent objectComponent,
    ref LightweightPhysicsComponent physicsComponent,
    [ReadOnly] DynamicBuffer<Child> children)
    {
        bool isGrounded = physicsComponent.isGrounded;
        RefRW<SpriteSheetAnimationComponent> animComponent = default;

        // 2. 자식 목록을 순회하여 원하는 컴포넌트를 가진 자식을 찾습니다.
        foreach (var child in children)
        {
            var childEntity = child.Value;

            // 3. 자식 엔티티가 SpriteSheetAnimationComponent와 Authoring을 모두 가지고 있는지 확인합니다.
            if (AnimationComponentLookup.HasComponent(child.Value))
            {
                // 4. 자식의 컴포넌트들을 가져옵니다.
                animComponent = AnimationComponentLookup.GetRefRW(child.Value);
                break;
            }
        }

        if (!animComponent.IsValid)
            return;

        Debug.Log($"[BehaviorJob] {objectComponent.Name} - Purpose: {objectComponent.Behavior.Purpose}, MovePosition: ({objectComponent.Behavior.MovePosition.x:G2}, {objectComponent.Behavior.MovePosition.y:G2})");

        if (objectComponent.Behavior.Purpose == MoveState.Move)
        {
            if (animComponent.ValueRW.CurrentState != AnimationState.Walking && animComponent.ValueRW.StartState != AnimationState.Walking)
            {
                animComponent.ValueRW.StartState = AnimationState.Walking;
            }

            Debug.Log($"[BehaviorJob] 일반 이동 처리 중: {objectComponent.Name}");
            HandleMovement(ref transform, ref objectComponent, ref animComponent.ValueRW);
        }
        else if (objectComponent.Behavior.Purpose == MoveState.ClimbUp || objectComponent.Behavior.Purpose == MoveState.ClimbDown)
        {
            if (animComponent.ValueRW.CurrentState != AnimationState.Walking && animComponent.ValueRW.StartState != AnimationState.Walking)
            {
                animComponent.ValueRW.StartState = AnimationState.Walking; // 사다리 애니메이션이 없으면 걷기 애니메이션 사용
            }

            Debug.Log($"[BehaviorJob] 사다리 이동 처리 중: {objectComponent.Name} - {objectComponent.Behavior.Purpose}");
            HandleClimbing(ref transform, ref objectComponent, ref animComponent.ValueRW);
        }
        else if (isGrounded && animComponent.ValueRW.CurrentState == AnimationState.Jump_Idle)
        {
            if (animComponent.ValueRW.StartState != AnimationState.Jump_Land)
            {
                animComponent.ValueRW.StartState = AnimationState.Jump_Land;
                animComponent.ValueRW.IsLoop = false;
            }
        }
        else if (!isGrounded && animComponent.ValueRW.CurrentState != AnimationState.Jump_Idle)
        {
            if (animComponent.ValueRW.StartState != AnimationState.Jump_Idle)
            {
                animComponent.ValueRW.StartState = AnimationState.Jump_Idle;
            }
        }
    }

    private void HandleMovement(ref LocalTransform transform, ref TSObjectComponent objectComponent, ref SpriteSheetAnimationComponent animComponent)
    {
        // 1. 현재 '루트(발)'의 위치를 계산합니다. (피벗 위치 + 오프셋)
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        // 2. 목표 '루트' 위치를 가져옵니다.
        float2 targetRootPosition = objectComponent.Behavior.MovePosition;

        animComponent.IsFlip = targetRootPosition.x < currentRootPosition.x;

        // 3. 한 프레임에 이동할 최대 거리를 계산합니다.
        float maxDistanceDelta = Speed * DeltaTime;

        // 4. MoveTowards를 사용해 다음 '루트' 위치를 계산합니다.
        float2 newRootPosition = Utility.Geometry.MoveTowards(currentRootPosition, targetRootPosition, maxDistanceDelta);

        // 5. 새로 계산된 '루트' 위치를 엔티티의 '피벗' 위치로 다시 변환합니다. (루트 위치 - 오프셋)
        float2 newTransformPosition = newRootPosition;
        newTransformPosition.y -= objectComponent.RootOffset;

        // 6. 최종 '피벗' 위치를 LocalTransform에 적용합니다. Z축 값은 유지합니다.
        transform.Position = new float3(newTransformPosition.x, newTransformPosition.y, transform.Position.z);

        // 도착지 위치 비교
        currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        float distance = math.distance(currentRootPosition, targetRootPosition);
        if (distance < 0.2f) // NavigationSystem과 동일한 임계값 사용
        {
            animComponent.StartState = AnimationState.Idle;
            objectComponent.Behavior.Purpose = MoveState.None;
            // 현재 위치를 MovePosition에 업데이트
            objectComponent.Behavior.MovePosition = currentRootPosition;

            Debug.Log($"[BehaviorJob] 일반 이동 완료: {objectComponent.Name}, 거리: {distance:G3}");
        }
    }

    private void HandleClimbing(ref LocalTransform transform, ref TSObjectComponent objectComponent, ref SpriteSheetAnimationComponent animComponent)
    {
        // 현재 위치와 목표 위치 계산
        float2 currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        float2 targetRootPosition = objectComponent.Behavior.MovePosition;

        Debug.Log($"사다리 이동: 현재({currentRootPosition.x:G2}, {currentRootPosition.y:G2}) → 목표({targetRootPosition.x:G2}, {targetRootPosition.y:G2})");

        float maxDistanceDelta = Speed * DeltaTime; // 일반 이동 속도 사용

        // 사다리 이동은 일반적인 MoveTowards 사용 (물리 무시)
        float2 newRootPosition = Utility.Geometry.MoveTowards(currentRootPosition, targetRootPosition, maxDistanceDelta);

        // Transform 위치 업데이트
        float2 newTransformPosition = newRootPosition;
        newTransformPosition.y -= objectComponent.RootOffset;

        transform.Position = new float3(newTransformPosition.x, newTransformPosition.y, transform.Position.z);

        // 도착 확인
        currentRootPosition = transform.Position.xy;
        currentRootPosition.y += objectComponent.RootOffset;

        float remainingDistance = math.distance(currentRootPosition, targetRootPosition);
        Debug.Log($"[BehaviorJob] 사다리 이동 중: 남은 거리 = {remainingDistance:G3}");

        if (remainingDistance < 0.2f) // NavigationSystem과 동일한 임계값 사용
        {
            // 정확한 목표 위치로 스냅
            float2 exactTransformPos = targetRootPosition;
            exactTransformPos.y -= objectComponent.RootOffset;
            transform.Position = new float3(exactTransformPos.x, exactTransformPos.y, transform.Position.z);

            animComponent.StartState = AnimationState.Idle;
            objectComponent.Behavior.Purpose = MoveState.None;
            objectComponent.Behavior.MovePosition = targetRootPosition;

            Debug.Log($"[BehaviorJob] 사다리 이동 완료: {objectComponent.Name}, 거리: {remainingDistance:G3}");
        }
    }

}