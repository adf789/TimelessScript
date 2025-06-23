using UnityEngine;

public class FlowManager : BaseManager<FlowManager>
{
    public BaseFlow CurrentFlow { get; private set; }

    public void ChangeFlow(GameState state)
    {
        if (CurrentFlow != null && CurrentFlow.State == state)
            return;

        CurrentFlow = LoadFlow(state);
        CurrentFlow.Enter();
    }

    private BaseFlow LoadFlow(GameState state)
    {
        return Resources.Load<BaseFlow>(string.Format(StringDefine.PATH_LOAD_FLOW, state));
    }
}
