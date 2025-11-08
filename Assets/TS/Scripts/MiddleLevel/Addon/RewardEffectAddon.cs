using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class RewardEffectAddon : MonoBehaviour
{
    [SerializeField] private TextMeshPro countText;
    [SerializeField] private SimpleTweenManage tween;

    public void Show(int count)
    {
        countText.SetText(count.ToString());

        gameObject.SetActive(true);

        tween.StartTween();

        WaitInactive().Forget();
    }

    private async UniTask WaitInactive()
    {
        await UniTask.Delay(2000);

        gameObject.SetActive(false);
    }
}
