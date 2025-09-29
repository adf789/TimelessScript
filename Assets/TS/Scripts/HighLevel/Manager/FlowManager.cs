using Cysharp.Threading.Tasks;
using UnityEngine;

public class FlowManager : BaseManager<FlowManager>
{
    public BaseFlow CurrentFlow { get; private set; }

    public async UniTask ChangeFlow(GameState state)
    {
        if (CurrentFlow != null && CurrentFlow.State == state)
            return;

        var beforeFlow = CurrentFlow;

        // 현재 플로우 로드
        CurrentFlow = await LoadFlow(state);

        // 로딩 시작
        var loadingFlow = await LoadFlow(GameState.Loading);
        await loadingFlow.Enter();

        // 이전 플로우 종료
        if (beforeFlow)
            await beforeFlow.Exit();

        // 플로우 시작
        await CurrentFlow.Enter();

        // 로딩 종료
        await loadingFlow.Exit();
    }

    private async UniTask<BaseFlow> LoadFlow(GameState state)
    {
        string flowName = $"{state}Flow";

        var objectResourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<ScriptableObject>();
        var flowObject = await objectResourcesPath.LoadByNameAsync<ScriptableObject>(flowName);

        return flowObject != null ? flowObject as BaseFlow : null;
    }
}
