
using Cysharp.Threading.Tasks;

public class IntroViewController : BaseController<IntroView, IntroViewModel>
{
    public override UIType UIType => UIType.IntroView;
    public override bool IsPopup => false;

    public override async UniTask BeforeEnterProcess()
    {

    }

    public override async UniTask EnterProcess()
    {
        GetModel().SetEventNext(OnEventNext);
    }

    public override async UniTask BeforeExitProcess()
    {

    }

    public override async UniTask ExitProcess()
    {

    }

    private void OnEventNext()
    {
        FlowManager.Instance.ChangeFlow(GameState.Town).Forget();
    }
}
