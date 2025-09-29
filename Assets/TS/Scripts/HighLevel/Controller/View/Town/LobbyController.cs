
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LobbyViewController : BaseController<LobbyView, LobbyViewModel>
{
    public override UIType UIType => UIType.LobbyView;
    public override bool IsPopup => false;

    public override async UniTask BeforeEnterProcess()
    {
        GetModel().SetEventShowInventory(OnEventShowInventory);
    }

    public override async UniTask EnterProcess()
    {

    }

    public override async UniTask BeforeExitProcess()
    {

    }

    public override async UniTask ExitProcess()
    {

    }

    private void OnEventShowInventory()
    {
        var inventoryPopup = UIManager.Instance.GetController(UIType.InventoryPopup);

        inventoryPopup.Enter().Forget();
    }
}
