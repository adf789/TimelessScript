
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class LobbyView : BaseView<LobbyViewModel>
{
    [SerializeField] private TextMeshProUGUI fps;
    [SerializeField] private TextMeshProUGUI spawnCount;
    [SerializeField] private CurrencyUnit currencyUnit;
    public override void Show()
    {
        ShowAnalysis().Forget();
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

    public async UniTask ShowAnalysis()
    {
        while (true)
        {
            var analysisData = Model.OnEventAnalysisGet();

            fps.SetText($"FPS: {(int) analysisData.CurrentFPS} / {(int) analysisData.AverageFPS}");
            spawnCount.SetText($"SpawnCount: {analysisData.SpawnCount}");

            await UniTask.Delay(IntDefine.TIME_MILLISECONDS_ONE, cancellationToken: TokenPool.Get(GetHashCode()));
        }
    }

    public void OnClickShowInventory()
    {
        Model.OnEventShowInventory();
    }
}
