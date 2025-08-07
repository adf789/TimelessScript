using Cysharp.Threading.Tasks;

public class FlowManager : BaseManager<FlowManager>
{
    public BaseFlow CurrentFlow { get; private set; }

    public async UniTask ChangeFlow(GameState state)
    {
        if (CurrentFlow != null && CurrentFlow.State == state)
            return;

        CurrentFlow = await LoadFlow(state);

        CurrentFlow.Enter();
    }

    private async UniTask<BaseFlow> LoadFlow(GameState state)
    {
        string flowName = $"{state}Flow";

        return await ResourcesManager.Instance.LoadAssetByName<BaseFlow>(ResourceType.Flow, flowName);
    }
}
