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

        // Login().Forget();
        FlowManager.Instance.ChangeFlow(GameState.Intro).Forget();
    }

    private async UniTask Login()
    {
        await DatabaseSubManager.Instance.InitializeFirebaseAsync();

        if (await AuthManager.Instance.SignInAsync())
        {
            if (await AuthManager.Instance.LoadUserDataFromDatabase())
            {
                Debug.Log($"계정 로드 성공: {AuthManager.Instance.PlayerId}");
            }
            else
            {
                Debug.Log($"계정 가입 성공: {AuthManager.Instance.PlayerId}");

                await AuthManager.Instance.SaveUserDataToDatabase();
            }

            FlowManager.Instance.ChangeFlow(GameState.Intro).Forget();
        }
        else
        {
            Debug.LogError("로그인에 실패했습니다.");
        }
    }
}
