
using TMPro;
using UnityEngine;

public class InventoryPopup : BaseView<InventoryPopupModel>
{
    [SerializeField] private TextMeshProUGUI itemCount;

    public override void Show()
    {
        itemCount.SetText(Model.Count.ToString());
    }

    public void OnClickClose()
    {
        Model.OnEventClose();
    }
}
