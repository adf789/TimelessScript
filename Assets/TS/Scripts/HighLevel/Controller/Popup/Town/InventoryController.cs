
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InventoryPopupController : BaseController<InventoryPopup, InventoryPopupModel>
{
    public override UIType UIType => UIType.InventoryPopup;
    public override bool IsPopup => true;

    public override async UniTask BeforeEnterProcess()
    {
        GetModel().SetEventClose(OnEventClose);

        GetModel().SetCount(GetFirstItemCount());
    }

    public override async UniTask EnterProcess()
    {
        view.Show();
    }

    public override async UniTask BeforeExitProcess()
    {

    }

    public override async UniTask ExitProcess()
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
        Exit().Forget();
    }
}
