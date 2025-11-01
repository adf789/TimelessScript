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
    }
}
