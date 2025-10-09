public class LobbyViewController : BaseController<LobbyView, LobbyViewModel>
{
    public override UIType UIType => UIType.LobbyView;
    public override bool IsPopup => false;

    public override void BeforeEnterProcess()
    {
        GetModel().SetEventShowInventory(OnEventShowInventory);
        GetModel().SetEventFpsGet(OnEventFpsGet);

        ObserverSubManager.Instance.AddObserver<ShowCurrencyParam>(OnShowCurrency);
    }

    public override void EnterProcess()
    {
        GetView().Show();

        ShowCurrency();
    }

    public override void BeforeExitProcess()
    {

    }

    public override void ExitProcess()
    {
        ObserverSubManager.Instance.RemoveObserver<ShowCurrencyParam>(OnShowCurrency);
    }

    private void OnEventShowInventory()
    {
        var inventoryPopup = UIManager.Instance.GetController(UIType.InventoryPopup);

        inventoryPopup.Enter();
    }

    private (float, float) OnEventFpsGet()
    {
        float fps = GameManager.Instance.CurrentFPS;
        float avsFps = GameManager.Instance.AverageFPS;

        return (fps, avsFps);
    }

    private void ShowCurrency()
    {
        if (ViewIsNull)
            return;

        long currency = 0;

        foreach (var item in PlayerSubManager.Instance.Inventory.GetAllItems())
        {
            currency = item.Count;

            break;
        }

        GetView().ShowCurrencies(currency);
    }

    private void OnShowCurrency(ShowCurrencyParam param)
    {
        ShowCurrency();
    }
}
