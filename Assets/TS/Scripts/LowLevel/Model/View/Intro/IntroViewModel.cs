
public class IntroViewModel : BaseModel
{
    public System.Action OnEventNext;

    public void SetEventNext(System.Action onEvent)
    {
        OnEventNext = onEvent;
    }
}
