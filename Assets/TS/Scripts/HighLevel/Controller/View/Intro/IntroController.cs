
using Cysharp.Threading.Tasks;

public class IntroViewController : BaseController<IntroView, IntroViewModel>
{
    public override UIType UIType => UIType.IntroView;
    public override bool IsPopup => false;

    public override void BeforeEnterProcess()
    {
        GetModel().SetEventNext(OnEventNext);
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

    private void OnEventNext()
    {
        FlowManager.Instance.ChangeFlow(GameState.Town).Forget();
    }
}
