using Cysharp.Threading.Tasks;
using UnityEngine;

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

        var objectResourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<ScriptableObject>();
        var flowObject = await objectResourcesPath.LoadByName<ScriptableObject>(flowName);

        return flowObject != null ? flowObject as BaseFlow : null;
    }
}
