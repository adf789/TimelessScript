using Cysharp.Threading.Tasks;
using UnityEngine;

public class IntroScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Login().Forget();
    }

    private async UniTask Login()
    {
        await DatabaseSubManager.Instance.InitializeFirebaseAsync();

        if (await AuthManager.Instance.SignInAnonymouslyAsync())
        {
            PlayerSubManager.Instance.SetPlayerID(AuthManager.Instance.PlayerID);

            if (await AuthManager.Instance.LoadUserDataFromDatabase())
            {
                await PlayerSubManager.Instance.LoadInventory();

                Debug.Log($"계정 로드 성공: {AuthManager.Instance.PlayerID}");
            }
            else
            {
                Debug.Log($"계정 가입 성공: {AuthManager.Instance.PlayerID}");

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
