
public class InventoryPopupModel : BaseModel
{
    public long Count { get; private set; }
    public System.Action OnEventClose { get; private set; }

    public void SetCount(long count)
    {
        Count = count;
    }

    public void SetEventClose(System.Action onEvent)
    {
        OnEventClose = onEvent;
    }
}
