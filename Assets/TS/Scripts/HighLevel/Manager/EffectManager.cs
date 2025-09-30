using UnityEngine;

public class EffectManager : BaseManager<EffectManager>
{
    [SerializeField] private ObjectPoolSupport rewardEffectPool;

    void OnEnable()
    {
        ObserverSubManager.Instance.AddObserver<RewardEffectParam>(ShowRewardEffect);
    }

    void OnDisable()
    {
        ObserverSubManager.Instance.RemoveObserver<RewardEffectParam>(ShowRewardEffect);
    }

    private async void ShowRewardEffect(IObserverParam p)
    {
        var param = (RewardEffectParam) p;

        var rewardEffect = await rewardEffectPool.LoadAsync();

        rewardEffect.transform.position = new Vector3(param.Position.x, param.Position.y);

        if (rewardEffect.TryGetComponent(out RewardEffectSupport effectSupport))
            effectSupport.Show(param.RewardCount);
    }
}
