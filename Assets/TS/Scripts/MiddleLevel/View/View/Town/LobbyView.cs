
using UnityEngine;

public class LobbyView : BaseView<LobbyViewModel>
{
    [SerializeField] private CurrencyUnit currencyUnit;
    public override void Show()
    {

    }

    public void ShowCurrencies(long currency)
    {
        currencyUnit.SetCurrency(currency);
        currencyUnit.Show();
    }

    public void OnClickShowInventory()
    {
        Model.OnEventShowInventory();
    }
}
