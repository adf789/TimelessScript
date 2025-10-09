
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class LobbyView : BaseView<LobbyViewModel>
{
    [SerializeField] private TextMeshProUGUI fps;
    [SerializeField] private CurrencyUnit currencyUnit;
    public override void Show()
    {
        ShowFPS().Forget();
    }

    void OnDisable()
    {
        TokenPool.Cancel(GetHashCode());
    }

    public void ShowCurrencies(long currency)
    {
        currencyUnit.SetCurrency(currency);
        currencyUnit.Show();
    }

    public async UniTask ShowFPS()
    {
        while (true)
        {
            var fpsResult = Model.OnEventFpsGet();

            fps.SetText($"FPS: {(int) fpsResult.Item1} / {(int) fpsResult.Item2}");

            await UniTask.Delay(IntDefine.TIME_MILLISECONDS_ONE, cancellationToken: TokenPool.Get(GetHashCode()));
        }
    }

    public void OnClickShowInventory()
    {
        Model.OnEventShowInventory();
    }
}
