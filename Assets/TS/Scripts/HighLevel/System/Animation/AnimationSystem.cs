using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct AnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 이 시스템은 SpriteSheetAnimationComponent가 있는 엔티티가 하나라도 있을 때만 업데이트됩니다.
        state.RequireForUpdate<SpriteRendererComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = state.World.Time.DeltaTime;

        // 애니메이션 처리 및 렌더링 옵션 적용
        foreach (var (authoring, renderer, anim) in
        SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<SpriteSheetAnimationAuthoring>,
        RefRO<SpriteRendererComponent>,
        RefRW<SpriteSheetAnimationComponent>>())
        {
            if (!authoring.Value.IsLoaded)
            {
                authoring.Value.Initialize();
                authoring.Value.LoadAnimations();
                continue;
            }


            SetAnimation(authoring.Value, ref anim.ValueRW, deltaTime);
            SetRenderer(authoring.Value, in renderer.ValueRO);
        }

        // 렌더링 옵션 적용
        foreach (var (authoring, renderer) in
        SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<SpriteRendererAuthoring>,
        RefRO<SpriteRendererComponent>>())
        {
            SetRenderer(authoring.Value, in renderer.ValueRO);
        }

        // 애니메이션 콜백 이벤트 호출
        var animationCallbackJob = new AnimationCallbackJob()
        {
            ObjectTargetCLookup = SystemAPI.GetComponentLookup<ObjectTargetComponent>(true),
            InteractCLookup = SystemAPI.GetComponentLookup<InteractComponent>(true),
            InteractBLookup = SystemAPI.GetBufferLookup<InteractBuffer>(false),
        };

        state.Dependency = animationCallbackJob.ScheduleParallel(state.Dependency);
    }

    private void SetAnimation(SpriteSheetAnimationAuthoring authoring,
    ref SpriteSheetAnimationComponent anim, float deltaTime)
    {
        // 현재 애니메이션 진행
        if (!CheckAnimationFrame(authoring, ref anim, deltaTime))
            return;

        // 애니메이션 전환 요청 처리
        if (anim.NextState != AnimationState.None
        && anim.NextState != anim.CurrentState
        && !anim.IsTransitioning)
        {
            ProcessAnimationTransition(authoring, ref anim);
        }

        ProcessCurrentAnimation(authoring, ref anim);
    }

    private void SetRenderer(SpriteSheetAnimationAuthoring authoring, in SpriteRendererComponent renderer)
    {
        // 컴포넌트 값에 맞춰서 렌더러 옵션 변경
        authoring.SetFlip(renderer.IsFlip);
        authoring.SetLayer(renderer.Layer);
        authoring.ToggleOutline(renderer.IsEmphasis);
    }

    private void SetRenderer(SpriteRendererAuthoring authoring, in SpriteRendererComponent renderer)
    {
        // 컴포넌트 값에 맞춰서 렌더러 옵션 변경
        authoring.SetFlip(renderer.IsFlip);
        authoring.SetLayer(renderer.Layer);
    }

    public void SetFlip(SpriteRenderer spriteRenderer, bool isFlip)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = isFlip;
    }

    public void SetLayer(SpriteRenderer spriteRenderer, int layer)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = layer;
    }

    private bool CheckAnimationFrame(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent anim, float deltaTime)
    {
        int frameDelay = anim.CurrentPhase switch
        {
            AnimationPhase.Start => authoring.GetStartFrameDelay(anim.CurrentSpriteIndex, anim.CurrentAnimationIndex),
            AnimationPhase.Loop => authoring.GetFrameDelay(anim.CurrentSpriteIndex, anim.CurrentAnimationIndex),
            AnimationPhase.End => authoring.GetEndFrameDelay(anim.CurrentSpriteIndex, anim.CurrentAnimationIndex),
            _ => authoring.GetFrameDelay(anim.CurrentSpriteIndex, anim.CurrentAnimationIndex)
        };

        // 60 FPS 기준으로 프레임을 시간으로 변환
        float frameDuration = frameDelay / 60.0f;

        if (anim.PassingTime < frameDuration)
        {
            anim.PassingTime += deltaTime;
            return false;
        }
        else
        {
            anim.PassingTime = 0f;
            return true;
        }
    }

    private void ProcessAnimationTransition(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent anim)
    {
        anim.IsTransitioning = true;

        switch (anim.TransitionType)
        {
            case AnimationTransitionType.None:
                anim.IsEndLoopOneTime = true;
                break;

            case AnimationTransitionType.SkipAllPhase:
                StartNextAnimation(authoring, ref anim);
                break;

            case AnimationTransitionType.SkipCurrentPhase:
                SetupPhaseAnimation(AnimationPhase.End, authoring, ref anim);
                break;
        }
    }

    private void ProcessCurrentAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent anim)
    {
        // 애니메이션 루프가 끝나면 콜백 호출
        if (anim.CurrentPhase == AnimationPhase.Loop
        && anim.IsLastAnimation
        && !anim.ShouldEndCurrentAnimation())
            SetCompleteAnimationCallback(ref anim);

        int nextIndex = anim.NextAnimationIndex();

        // 현재 Phase의 애니메이션이 끝났는지 확인
        switch (anim.CurrentPhase)
        {
            case AnimationPhase.Start:
                {
                    authoring.SetStartAnimationByIndex(anim.CurrentState, nextIndex);

                    if (anim.IsLastAnimation)
                        SetupPhaseAnimation(AnimationPhase.Loop, authoring, ref anim);
                }
                break;

            case AnimationPhase.Loop:
                {
                    // 애니메이션 시작 이벤트 호출
                    if (nextIndex == 0 && anim.PrevPhase == AnimationPhase.Loop)
                        SetStartAnimationCallback(ref anim);
                    // 애니메이션 루프가 한 번 종료 시 이전 상태 저장
                    else if (anim.IsLastAnimation && anim.PrevPhase == AnimationPhase.Start)
                        SetPrevPhase(ref anim);

                    if (!anim.ShouldEndCurrentAnimation())
                    {
                        authoring.SetAnimationByIndex(anim.CurrentState, nextIndex);
                        return;
                    }

                    // 전환 요청이 있고 마지막 프레임이라면 End Phase로 이동
                    if (anim.HasEndAnimation)
                        SetupPhaseAnimation(AnimationPhase.End, authoring, ref anim);
                    else // End 애니메이션이 없으면 바로 다음 애니메이션으로
                        StartNextAnimation(authoring, ref anim);
                }
                break;

            case AnimationPhase.End:
                {
                    authoring.SetEndAnimationByIndex(anim.CurrentState, nextIndex);

                    if (anim.IsLastAnimation)
                        StartNextAnimation(authoring, ref anim);
                }
                break;
        }
    }

    private void SetStartAnimationCallback(ref SpriteSheetAnimationComponent anim)
    {
        anim.SetFlag(anim.CurrentState, AnimationFlagType.Start, true);
    }

    private void SetCompleteAnimationCallback(ref SpriteSheetAnimationComponent anim)
    {
        anim.SetFlag(anim.CurrentState, AnimationFlagType.Complete, true);
    }

    private void SetEndAnimationCallback(ref SpriteSheetAnimationComponent anim)
    {
        anim.SetFlag(anim.CurrentState, AnimationFlagType.End, true);
    }

    private void SetPrevPhase(ref SpriteSheetAnimationComponent anim)
    {
        anim.PrevPhase = anim.CurrentPhase;
    }

    /// <summary>
    /// 다음 애니메이션 상태로 설정함
    /// </summary>
    private void StartNextAnimation(SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent anim)
    {
        // 애니메이션 완료 이벤트 플래그 설정
        SetEndAnimationCallback(ref anim);

        AnimationState targetState = anim.NextState;
        bool isSkip = anim.TransitionType == AnimationTransitionType.SkipAllPhase;

        if (!authoring.TryGetSpriteNode(targetState, out var node, out int nodeIndex))
            Debug.LogWarning($"Animation state {targetState} not found, using default");

        anim.CurrentState = targetState;
        anim.CurrentSpriteIndex = nodeIndex;
        anim.NextState = AnimationState.None;
        anim.ShouldTransitionToEnd = false;
        anim.TransitionType = AnimationTransitionType.None;
        anim.HasStartAnimation = authoring.HasStartAnimation(targetState);
        anim.HasEndAnimation = authoring.HasEndAnimation(targetState);
        anim.IsTransitioning = false;

        // 애니메이션 시작 이벤트 플래그 설정
        SetStartAnimationCallback(ref anim);

        if (authoring.CheckPlayOnetime(nodeIndex))
            anim.IsEndLoopOneTime = true;

        if (isSkip || !anim.HasStartAnimation)
        {
            SetupPhaseAnimation(AnimationPhase.Loop, authoring, ref anim);
        }
        else
        {
            SetupPhaseAnimation(AnimationPhase.Start, authoring, ref anim);
        }
    }

    private void SetupPhaseAnimation(AnimationPhase phase, SpriteSheetAnimationAuthoring authoring, ref SpriteSheetAnimationComponent component)
    {
        SetPrevPhase(ref component);

        component.CurrentPhase = phase;
        component.CurrentAnimationIndex = -1;
        component.PassingTime = 0f;

        switch (phase)
        {
            case AnimationPhase.Start:
                component.CurrentAnimationCount = authoring.GetStartAnimationCount(component.CurrentState);
                break;
            case AnimationPhase.Loop:
                component.CurrentAnimationCount = authoring.GetSpriteSheetCount(component.CurrentState);
                break;
            case AnimationPhase.End:
                component.CurrentAnimationCount = authoring.GetEndAnimationCount(component.CurrentState);
                break;
        }
    }
}