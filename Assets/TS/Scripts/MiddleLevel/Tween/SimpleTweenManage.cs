using UnityEngine;

[ExecuteInEditMode]
public class SimpleTweenManage : MonoBehaviour
{
    [SerializeField] private SimpleTween[] tweens = null;

    void Awake()
    {
        if (tweens == null || tweens.Length == 0)
            RegisterTweens();
    }

    void OnDestroy()
    {
        UnregisterTweens();
    }

    public void StartTween()
    {
        foreach (var tween in tweens)
        {
            tween.StartTween();
        }
    }

    [ContextMenu("Register Tweens")]
    public void RegisterTweens()
    {
        tweens = GetComponentsInChildren<SimpleTween>();

        foreach (var tween in tweens)
        {
            tween.SetAutoPlay(false);
        }
    }

    [ContextMenu("Unregister Tweens")]
    public void UnregisterTweens()
    {
        foreach (var tween in tweens)
        {
            tween.SetAutoPlay(true);
        }

        tweens = null;
    }
}