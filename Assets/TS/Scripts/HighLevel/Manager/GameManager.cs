using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : BaseManager<GameManager>
{
    [Header("Analysis")]
    public AnalysisData AnalysisData;

    private void Awake()
    {
        DontDestroyOnLoad(Instance);
    }

    private void Start()
    {
        Application.runInBackground = true;

        AuthManager.Instance.SignInAsync(() =>
        {
            FlowManager.Instance.ChangeFlow(GameState.Intro).Forget();
        }).Forget();
    }

    [ContextMenu("Test")]
    public void TestCode()
    {
        FlowManager.Instance.ChangeFlow(GameState.Loading).Forget();
    }
}
