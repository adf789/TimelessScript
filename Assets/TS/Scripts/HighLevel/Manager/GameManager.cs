using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : BaseManager<GameManager>
{
    [Header("Analysis")]
    public AnalysisData AnalysisData;

    private void Awake()
    {
        DontDestroyOnLoad(Instance);

        InitApplicationEnvironment();
    }

    private void Start()
    {

    }

    private void InitApplicationEnvironment()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 30;
    }
}
