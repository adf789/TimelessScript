using UnityEngine;

public class GameManager : BaseManager<GameManager>
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    [ContextMenu("Test")]
    public void TestCode()
    {
        FlowManager.Instance.ChangeFlow(GameState.Loading);
    }
}
