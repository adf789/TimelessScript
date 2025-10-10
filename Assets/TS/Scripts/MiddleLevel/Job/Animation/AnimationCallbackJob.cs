
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct AnimationCallbackJob : IJobEntity
{
    public ComponentLookup<ObjectTargetComponent> ObjectTargetCLookup;
    public ComponentLookup<InteractComponent> InteractCLookup;
    [NativeDisableParallelForRestriction]
    public BufferLookup<InteractBuffer> InteractBLookup;
    public EntityCommandBuffer.ParallelWriter Ecb;

    public void Execute(
[EntityIndexInQuery] int entityIndexInQuery,
        Entity entity,
    ref SpriteSheetAnimationComponent anim)
    {
        // 애니메이션이 시작한 경우
        OnAnimationStarted(entityIndexInQuery, entity, ref anim);

        // 애니메이션의 한 루프가 종료한 경우
        OnAnimationCompleted(entityIndexInQuery, entity, ref anim);

        // 애니메이션이 종료한 경우
        OnAnimationEnded(entityIndexInQuery, entity, ref anim);
    }

    /// <summary>
    /// 애니메이션이 시작될 때 콜백
    /// </summary>
    private void OnAnimationStarted(int entityIndexInQuery, Entity entity, ref SpriteSheetAnimationComponent anim)
    {
        var flag = anim.GetFlag(AnimationFlagType.Start);

        if (!flag.IsOn)
            return;

        HandleAnimationStarted(entityIndexInQuery, entity, flag.State);

        anim.SetFlagReset(AnimationFlagType.Start);
    }

    /// <summary>
    /// 애니메이션 하나의 루프가 종료될 때 콜백
    /// </summary>
    private void OnAnimationCompleted(int entityIndexInQuery, Entity entity, ref SpriteSheetAnimationComponent anim)
    {
        var flag = anim.GetFlag(AnimationFlagType.Complete);

        if (!flag.IsOn)
            return;

        HandleAnimationCompleted(entityIndexInQuery, entity, flag.State);

        anim.SetFlagReset(AnimationFlagType.Complete);
    }

    /// <summary>
    /// 애니메이션이 모두 종료될 때 콜백
    /// </summary>
    private void OnAnimationEnded(int entityIndexInQuery, Entity entity, ref SpriteSheetAnimationComponent anim)
    {
        var flag = anim.GetFlag(AnimationFlagType.End);

        if (!flag.IsOn)
            return;

        HandleAnimationEnded(entityIndexInQuery, entity, flag.State);

        anim.SetFlagReset(AnimationFlagType.End);
    }

    private void HandleAnimationStarted(int entityIndexInQuery, Entity entity, AnimationState animationState)
    {
        switch (animationState)
        {
            default:
                // 기본 처리 로직
                break;
        }
    }

    private void HandleAnimationCompleted(int entityIndexInQuery, Entity entity, AnimationState animationState)
    {
        switch (animationState)
        {
            case AnimationState.Interact:
                HandleInteractAnimationCompleted(entityIndexInQuery, entity);
                break;

            default:
                // 기본 처리 로직
                break;
        }
    }

    private void HandleAnimationEnded(int entityIndexInQuery, Entity entity, AnimationState animationState)
    {
        switch (animationState)
        {
            case AnimationState.Interact:
                HandleInteractAnimationEnded(entityIndexInQuery, entity);
                break;

            default:
                // 기본 처리 로직
                break;
        }
    }

    private void HandleInteractAnimationCompleted(int entityIndexInQuery, Entity entity)
    {
        // 엔티티 타겟을 가져옴
        if (!ObjectTargetCLookup.HasComponent(entity))
            return;

        var objectTarget = ObjectTargetCLookup[entity];

        if (!InteractCLookup.HasComponent(objectTarget.Target))
            return;

        // 상호작용 정보를 가져옴
        var interactComponent = InteractCLookup[objectTarget.Target];

        // 상호작용 버퍼 가져옴
        DynamicBuffer<InteractBuffer> interactBuffer;

        if (!InteractBLookup.HasBuffer(objectTarget.Target))
        {
            interactBuffer = Ecb.AddBuffer<InteractBuffer>(entityIndexInQuery, objectTarget.Target);
        }
        else
        {
            interactBuffer = InteractBLookup[objectTarget.Target];
        }

        // 상호작용 등록
        interactBuffer.Add(new InteractBuffer()
        {
            DataID = interactComponent.DataID,
            DataType = interactComponent.DataType,
        });
    }

    private void HandleInteractAnimationEnded(int entityIndexInQuery, Entity entity)
    {
        if (!ObjectTargetCLookup.HasComponent(entity))
            return;

        var objectTarget = ObjectTargetCLookup[entity];

        if (objectTarget.Target == Entity.Null)
            return;

        Ecb.RemoveComponent<InteractBuffer>(entityIndexInQuery, objectTarget.Target);
    }
}
