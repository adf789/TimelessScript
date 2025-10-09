
public class LobbyViewModel : BaseModel
{
    public System.Action OnEventShowInventory { get; private set; }
    public System.Func<(float, float)> OnEventFpsGet { get; private set; }

    public void SetEventShowInventory(System.Action onEvent)
    {
        OnEventShowInventory = onEvent;
    }

    public void SetEventFpsGet(System.Func<(float, float)> onEvent)
    {
        OnEventFpsGet = onEvent;
    }
}
