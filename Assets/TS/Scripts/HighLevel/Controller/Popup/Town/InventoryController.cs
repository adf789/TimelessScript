
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InventoryPopupController : BaseController<InventoryPopup, InventoryPopupModel>
{
    public override UIType UIType => UIType.InventoryPopup;
    public override bool IsPopup => true;

    public override void BeforeEnterProcess()
    {
        GetModel().SetEventClose(OnEventClose);

        GetModel().SetCount(GetFirstItemCount());
    }

    public override void EnterProcess()
    {
        GetView().Show();
    }

    public override void BeforeExitProcess()
    {

    }

    public override void ExitProcess()
    {

    }

    private long GetFirstItemCount()
    {
        foreach (var item in PlayerSubManager.Instance.Inventory.GetAllItems())
        {
            return item.Count;
        }

        return 0;
    }

    private void OnEventClose()
    {
        Exit();
    }
}
