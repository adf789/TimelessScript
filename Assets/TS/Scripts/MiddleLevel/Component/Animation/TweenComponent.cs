using UnityEngine;
using TS.LowLevel;
using UnityEngine.UI;

namespace TS.MiddleLevel
{
    public class TweenComponent : MonoBehaviour
    {
        [SerializeField] private TweenData tweenData = TweenData.Default;
        [SerializeField] private Graphic[] graphics;
        [SerializeField] private SpriteRenderer[] spriteRenderers;

        private Vector3 originPosition;
        private Vector3 originRotation;
        private Vector3 originScale;

        private void OnEnable()
        {
            tweenData.isPlaying = true;
        }

        void OnDisable()
        {
            tweenData.isPlaying = false;

            ResetToStart();
        }

        private void Start()
        {
            originPosition = transform.localPosition;
            originRotation = transform.localEulerAngles;
            originScale = transform.localScale;
        }

        private void Update()
        {
            if (!tweenData.isPlaying) return;

            tweenData.elapsed += Time.deltaTime;

            if (tweenData.elapsed < tweenData.delay) return;

            float adjustedElapsed = tweenData.elapsed - tweenData.delay;
            float normalizedTime = Mathf.Clamp01(adjustedElapsed / tweenData.duration);

            if (tweenData.isReversing)
            {
                normalizedTime = 1f - normalizedTime;
            }

            float easedTime = TweenSupport.Evaluate(normalizedTime, tweenData.easingType);
            ApplyTween(easedTime);

            if (adjustedElapsed >= tweenData.duration)
            {
                OnTweenComplete();
            }
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
                tweenData.isPlaying = false;
            }
        }

        [ContextMenu("Play")]
        public void Play()
        {
            tweenData.elapsed = 0f;
            tweenData.isPlaying = true;
            tweenData.isReversing = false;
        }

        [ContextMenu("Stop")]
        public void Stop()
        {
            tweenData.isPlaying = false;
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
}