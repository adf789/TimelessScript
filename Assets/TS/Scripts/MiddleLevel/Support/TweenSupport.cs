using UnityEngine;
using TS.LowLevel;

namespace TS.MiddleLevel
{
    public static class TweenSupport
    {
        public static float Evaluate(float t, EasingType easingType)
        {
            return easingType switch
            {
                EasingType.Linear => t,
                EasingType.EaseInQuad => t * t,
                EasingType.EaseOutQuad => t * (2f - t),
                EasingType.EaseInOutQuad => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
                EasingType.EaseInCubic => t * t * t,
                EasingType.EaseOutCubic => (--t) * t * t + 1f,
                EasingType.EaseInOutCubic => t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f,
                EasingType.EaseInQuart => t * t * t * t,
                EasingType.EaseOutQuart => 1f - (--t) * t * t * t,
                EasingType.EaseInOutQuart => t < 0.5f ? 8f * t * t * t * t : 1f - 8f * (--t) * t * t * t,
                EasingType.EaseInQuint => t * t * t * t * t,
                EasingType.EaseOutQuint => 1f + (--t) * t * t * t * t,
                EasingType.EaseInOutQuint => t < 0.5f ? 16f * t * t * t * t * t : 1f + 16f * (--t) * t * t * t * t,
                EasingType.EaseInSine => 1f - Mathf.Cos(t * Mathf.PI / 2f),
                EasingType.EaseOutSine => Mathf.Sin(t * Mathf.PI / 2f),
                EasingType.EaseInOutSine => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f,
                EasingType.EaseInExpo => t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f),
                EasingType.EaseOutExpo => t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t),
                EasingType.EaseInOutExpo => EaseInOutExpo(t),
                EasingType.EaseInCirc => 1f - Mathf.Sqrt(1f - t * t),
                EasingType.EaseOutCirc => Mathf.Sqrt(1f - (--t) * t),
                EasingType.EaseInOutCirc => EaseInOutCirc(t),
                EasingType.EaseInBack => EaseInBack(t),
                EasingType.EaseOutBack => EaseOutBack(t),
                EasingType.EaseInOutBack => EaseInOutBack(t),
                EasingType.EaseInElastic => EaseInElastic(t),
                EasingType.EaseOutElastic => EaseOutElastic(t),
                EasingType.EaseInOutElastic => EaseInOutElastic(t),
                EasingType.EaseInBounce => 1f - EaseOutBounce(1f - t),
                EasingType.EaseOutBounce => EaseOutBounce(t),
                EasingType.EaseInOutBounce => t < 0.5f ? (1f - EaseOutBounce(1f - 2f * t)) / 2f : (1f + EaseOutBounce(2f * t - 1f)) / 2f,
                _ => t
            };
        }

        private static float EaseInOutExpo(float t)
        {
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
        }

        private static float EaseInOutCirc(float t)
        {
            return t < 0.5f
                ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f
                : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;
        }

        private static float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
        }

        private static float EaseInElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
        }

        private static float EaseOutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        private static float EaseInOutElastic(float t)
        {
            const float c5 = (2f * Mathf.PI) / 4.5f;
            if (t == 0f) return 0f;
            if (t == 1f) return 1f;
            return t < 0.5f
                ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f
                : (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }
    }
}