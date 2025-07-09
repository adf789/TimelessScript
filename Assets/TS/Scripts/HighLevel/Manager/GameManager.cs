using UnityEngine;

public class GameManager : BaseManager<GameManager>
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        FlowManager.Instance.ChangeFlow(GameState.Intro);
    }

    [ContextMenu("Test")]
    public void TestCode()
    {
        FlowManager.Instance.ChangeFlow(GameState.Loading);
    }
}
