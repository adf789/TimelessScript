
using TMPro;
using UnityEngine;

public class CurrencyUnit : BaseUnit<CurrencyUnitModel>
{
    [SerializeField] private TextMeshProUGUI countText;

    public override void Show()
    {
        countText.SetText(Model.Count);
    }

    public void SetCurrency(long currency)
    {
        if (IsNullModel)
            SetModel(new CurrencyUnitModel());

        Model.SetCount(currency);
    }
}
