using System;
using UnityEngine;

namespace TS.LowLevel
{
    [Serializable]
    public struct TweenData
    {
        [Header("Tween Settings")]
        public TweenType tweenType;
        public EasingType easingType;
        public float duration;
        public float delay;
        public bool loop;
        public bool pingPong;

        [Header("Animation Values")]
        public Vector3 startValue;
        public Vector3 endValue;

        [Header("Runtime")]
        public float elapsed;
        public bool isReversing;

        public static TweenData Default => new TweenData
        {
            tweenType = TweenType.None,
            easingType = EasingType.Linear,
            duration = 1f,
            delay = 0f,
            loop = false,
            pingPong = false,
            startValue = Vector3.zero,
            endValue = Vector3.zero,
            elapsed = 0f,
            isReversing = false
        };
    }
}