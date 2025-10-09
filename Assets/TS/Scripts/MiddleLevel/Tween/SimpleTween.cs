using UnityEngine;
using TS.LowLevel;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class SimpleTween : MonoBehaviour
{
    public bool IsPlaying => isPlaying;

    [SerializeField] private bool isAutoPlay = true;
    [SerializeField] private TweenData tweenData = TweenData.Default;
    [SerializeField] private Graphic[] graphics;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    private Vector3 originPosition;
    private Vector3 originRotation;
    private Vector3 originScale;
    private bool isPlaying;

    private void OnEnable()
    {
        if (isAutoPlay)
            StartTween();
    }

    void OnDisable()
    {
        StopTweenProcess();

        ResetToStart();
    }

    public void StartTween()
    {
        SetPosition();

        TweenProcess().Forget();
    }

    private async UniTask TweenProcess()
    {
        StopTweenProcess();

        isPlaying = true;
        tweenData.elapsed = 0f;

        float delay = tweenData.delay;
        float duration = tweenData.duration;
        EasingType easingType = tweenData.easingType;
        bool isReversing = tweenData.isReversing;

        while (true)
        {
            tweenData.elapsed += Time.deltaTime;

            if (tweenData.elapsed < delay) continue;

            float adjustedElapsed = tweenData.elapsed - delay;
            float normalizedTime = Mathf.Clamp01(adjustedElapsed / duration);

            if (isReversing)
            {
                normalizedTime = 1f - normalizedTime;
            }

            float easedTime = Utility.Tween.Evaluate(normalizedTime, easingType);
            ApplyTween(easedTime);

            if (adjustedElapsed >= duration)
            {
                OnTweenComplete();
                break;
            }

            if (await UniTask.NextFrame(cancellationToken: TokenPool.Get(GetHashCode())).SuppressCancellationThrow())
                break;
        }

        isPlaying = false;
    }

    private void StopTweenProcess()
    {
        TokenPool.Cancel(GetHashCode());

        ResetToStart();
    }

    private void SetPosition()
    {
        originPosition = transform.localPosition;
        originRotation = transform.localEulerAngles;
        originScale = transform.localScale;
    }

    private void ApplyTween(float t)
    {
        Vector3 currentValue = Vector3.Lerp(tweenData.startValue, tweenData.endValue, t);

        switch (tweenData.tweenType)
        {
            case TweenType.Position:
                transform.localPosition = originPosition + currentValue;
                break;

            case TweenType.Rotation:
                transform.localEulerAngles = originRotation + currentValue;
                break;

            case TweenType.Scale:
                transform.localScale = originScale + currentValue;
                break;

            case TweenType.Alpha:
                ApplyAlpha(currentValue.x);
                break;
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (spriteRenderers != null)
        {
            foreach (var spriteRenderer in spriteRenderers)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }
        }

        if (graphics != null)
        {
            foreach (var graphic in graphics)
            {
                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }
    }

    private void OnTweenComplete()
    {
        if (tweenData.pingPong)
        {
            tweenData.isReversing = !tweenData.isReversing;
            tweenData.elapsed = tweenData.delay;
        }
        else if (tweenData.loop)
        {
            tweenData.elapsed = tweenData.delay;
        }
        else
        {
            isPlaying = false;
        }
    }

    public void SetAutoPlay(bool isAutoPlay)
    {
        this.isAutoPlay = isAutoPlay;
    }

    [ContextMenu("Play")]
    public void Play()
    {
        tweenData.elapsed = 0f;
        tweenData.isReversing = false;

        isPlaying = true;
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        isPlaying = false;

        tweenData.elapsed = 0f;
    }

    [ContextMenu("Reset to Start")]
    public void ResetToStart()
    {
        Stop();
        ApplyTween(0f);
    }

    [ContextMenu("Reset to End")]
    public void ResetToEnd()
    {
        Stop();
        ApplyTween(1f);
    }

    [ContextMenu("Preview Start")]
    public void PreviewStart()
    {
        ApplyTween(0f);
    }

    [ContextMenu("Preview End")]
    public void PreviewEnd()
    {
        ApplyTween(1f);
    }

    public void SetTweenData(TweenData data)
    {
        tweenData = data;
    }

    public TweenData GetTweenData()
    {
        return tweenData;
    }
}