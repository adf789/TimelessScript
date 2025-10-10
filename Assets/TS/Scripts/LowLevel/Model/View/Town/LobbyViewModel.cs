
public class LobbyViewModel : BaseModel
{
    public System.Action OnEventShowInventory { get; private set; }
    public System.Func<AnalysisData> OnEventAnalysisGet { get; private set; }
    public System.Func<int> OnEventSpawnCountGet { get; private set; }

    public void SetEventShowInventory(System.Action onEvent)
    {
        OnEventShowInventory = onEvent;
    }

    public void SetEventAnalysisGet(System.Func<AnalysisData> onEvent)
    {
        OnEventAnalysisGet = onEvent;
    }
}
