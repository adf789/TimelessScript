
public class LobbyViewModel : BaseModel
{
    public System.Action OnEventShowInventory { get; private set; }

    public void SetEventShowInventory(System.Action onEvent)
    {
        OnEventShowInventory = onEvent;
    }
}
