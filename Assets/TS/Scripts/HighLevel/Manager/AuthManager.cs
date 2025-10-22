using UnityEngine;
using Cysharp.Threading.Tasks;
using Assets.SimpleSignIn.Apple.Scripts;
using Assets.SimpleSignIn.Google.Scripts;
using Assets.SimpleSignIn.Facebook.Scripts;
using System;

public class AuthManager : BaseManager<AuthManager>
{
    [System.Flags]
    public enum eLogInPlatForm
    {
        None = 0,
        Google = 1 << 0,
        Apple = 1 << 1,
        FaceBook = 1 << 2
    }

    public eLogInPlatForm LogInPlatForm { get; private set; }
    public bool IsGetUser { get; private set; }
    private AppleAuth appleAuth;
    private FacebookAuth facebookAuth;
    private GoogleAuth googleAuth;
    private Assets.SimpleSignIn.Google.Scripts.TokenResponse lastTokenResponse; // 토큰 정보 저장용
    private Assets.SimpleSignIn.Facebook.Scripts.TokenResponse lastTokenResponseFB; // 토큰 정보 저장용
    private Assets.SimpleSignIn.Apple.Scripts.TokenResponse lastTokenResponseApple; // 토큰 정보 저장용
    private Action requestCallBack;

    #region Account Common Function
    public string GetAccessToken()
    {
        if (lastTokenResponse != null)
        {
            return lastTokenResponse.AccessToken;
        }
        else if (lastTokenResponseFB != null)
        {
            return lastTokenResponseFB.AccessToken;
        }
        else if (lastTokenResponseApple != null)
        {
            return lastTokenResponseApple.AccessToken;
        }
        return "";
    }

    public void OnSignIn(bool result)
    {
        if (result == true)
        {
            //애드몹 정보,.. 
            // RequestGetAdmob((result) =>
            // {
            //     if (result)
            //         SequentialRewardedAdManager.Instance.Initialize();
            // }).Forget();

            Debug.Log("### OnSignIn 성공 ");
        }
        else
        {
            Debug.LogError("### OnSignIn 실패");
        }
    }
    #endregion Account Common Function

    #region Google Account
    // 구글 로그인 콜백
    private void OnGoogleSignIn(bool success, string error, Assets.SimpleSignIn.Google.Scripts.UserInfo userInfo)
    {
        if (success)
        {
            // 서버에 userInfo.sub(구글 고유ID)로 계정 생성/확인 요청 (여기선 예시로 바로 저장)
            string accountId = userInfo.sub;
            PlayerPrefs.SetString(StringDefine.KEY_PREFS_ACCOUNT_GOOGLE_ID, accountId);
            PlayerPrefs.Save();

            // 토큰 정보 요청
            googleAuth.GetTokenResponse(OnGetTokenResponse);
            // 메인 진입 등 추가 처리
            // 예: LoadMainSceneOnline();
            LogInPlatForm = eLogInPlatForm.Google;
        }
        else
        {
            // 에러 처리 (예: 팝업)
            Debug.LogError($"구글 로그인 실패: {error}");
        }
    }

    // 토큰 정보 콜백
    private void OnGetTokenResponse(bool success, string error, Assets.SimpleSignIn.Google.Scripts.TokenResponse tokenResponse)
    {
        if (success)
        {
            lastTokenResponse = tokenResponse;
            Debug.Log($"AccessToken: {tokenResponse.AccessToken}");
            Debug.Log($"IdToken: {tokenResponse.IdToken}");

            // JSON 데이터 생성
            string nid = PlayerPrefs.GetString(StringDefine.KEY_PREFS_ACCOUNT_GOOGLE_ID, "");

            var dto = new LoginInDto()
            {
                OsType = "WINDOWS",
                Nid = nid,
                Platform = "google",
                PlatformToken = tokenResponse.AccessToken
            };


            RequestLogin(dto, null).Forget();
        }
        else
        {
            Debug.LogError($"토큰 정보 획득 실패: {error}");
        }
    }




    // 구글 로그아웃 기능 추가
    public void GoogleLogout()
    {
        if (googleAuth != null)
        {
            googleAuth.SignOut(revokeAccessToken: true);
            googleAuth = null;

            LogInPlatForm = eLogInPlatForm.None;
            IsGetUser = false;
        }

        // 계정 정보 삭제
        PlayerPrefs.DeleteKey(StringDefine.KEY_PREFS_ACCOUNT_GOOGLE_ID);
        PlayerPrefs.Save();

        Debug.Log("구글 로그아웃 완료");
    }

    // 구글 로그인 요청 (UIManager에서 호출)
    public void RequestGoogleLogin(Action callBack = null)
    {
        if (googleAuth == null)
            googleAuth = new GoogleAuth();

        requestCallBack = callBack;

        googleAuth.SignIn(OnGoogleSignIn, caching: true);
    }
    #endregion Google Account

    #region Facebook Account
    // 페이스북 로그인 콜백
    private void OnFacebookSignIn(bool success, string error, Assets.SimpleSignIn.Facebook.Scripts.UserInfo userInfo)
    {
        if (success)
        {
            string accountId = userInfo.id;
            PlayerPrefs.SetString(StringDefine.KEY_PREFS_ACCOUNT_FACEBOOK_ID, accountId);
            PlayerPrefs.Save();

            facebookAuth.GetTokenResponse(OnGetTokenResponseFB);

            LogInPlatForm = eLogInPlatForm.FaceBook;
            // 필요시 facebookAuth.GetTokenResponse(...) 등 추가 처리
        }
        else
        {
            Debug.LogError($"페이스북 로그인 실패: {error}");
        }
    }

    private void OnGetTokenResponseFB(bool success, string error, Assets.SimpleSignIn.Facebook.Scripts.TokenResponse tokenResponse)
    {
        if (success)
        {
            lastTokenResponseFB = tokenResponse;
            Debug.Log($"AccessToken: {tokenResponse.AccessToken}");
            Debug.Log($"IdToken: {tokenResponse.IdToken}");

            // JSON 데이터 생성
            string nid = PlayerPrefs.GetString(StringDefine.KEY_PREFS_ACCOUNT_FACEBOOK_ID, "");

            var dto = new LoginInDto()
            {
                OsType = "WINDOWS",
                Nid = nid,
                Platform = "facebook",
                PlatformToken = tokenResponse.AccessToken
            };

            RequestLogin(dto, null).Forget();
        }
        else
        {
            Debug.LogError($"토큰 정보 획득 실패: {error}");
        }
    }


    // 페이스북 로그아웃 기능 추가
    public void FacebookLogout()
    {
        if (facebookAuth != null)
        {
            facebookAuth.SignOut(revokeAccessToken: true);
            facebookAuth = null;

            LogInPlatForm = eLogInPlatForm.None;
            IsGetUser = false;
        }
        PlayerPrefs.DeleteKey(StringDefine.KEY_PREFS_ACCOUNT_FACEBOOK_ID);
        PlayerPrefs.Save();
        Debug.Log("페이스북 로그아웃 완료");
    }

    // 페이스북 로그인 요청 (UIManager에서 호출)
    public void RequestFacebookLogin(Action callBack = null)
    {
        if (facebookAuth == null)
            facebookAuth = new FacebookAuth();

        requestCallBack = callBack;

        facebookAuth.SignIn(OnFacebookSignIn, caching: true);
    }
    #endregion Facebook Account

    #region Apple Account
    // 애플 로그인 콜백
    private void OnAppleSignIn(bool success, string error, Assets.SimpleSignIn.Apple.Scripts.UserInfo userInfo)
    {
        if (success)
        {
            string accountId = userInfo.Id;
            PlayerPrefs.SetString(StringDefine.KEY_PREFS_ACCOUNT_APPLE_ID, accountId);
            PlayerPrefs.Save();

            appleAuth.GetTokenResponse(OnGetTokenResponseApple);

            LogInPlatForm = eLogInPlatForm.Apple;
            requestCallBack?.Invoke();
            // 필요시 appleAuth.GetTokenResponse(...) 등 추가 처리
        }
        else
        {
            Debug.LogError($"애플 로그인 실패: {error}");
        }
    }

    private void OnGetTokenResponseApple(bool success, string error, Assets.SimpleSignIn.Apple.Scripts.TokenResponse tokenResponse)
    {
        if (success)
        {
            lastTokenResponseApple = tokenResponse;
            Debug.Log($"AccessToken: {tokenResponse.AccessToken}");
            Debug.Log($"IdToken: {tokenResponse.IdToken}");

            // JSON 데이터 생성
            string nid = PlayerPrefs.GetString(StringDefine.KEY_PREFS_ACCOUNT_APPLE_ID, "");

            var dto = new LoginInDto()
            {
                OsType = "IOS",
                Nid = nid,
                Platform = "apple",
                PlatformToken = tokenResponse.AccessToken
            };


            RequestLogin(dto, null).Forget();
        }
        else
        {
            Debug.LogError($"토큰 정보 획득 실패: {error}");
        }
    }


    // 애플 로그아웃 기능 추가
    public void AppleLogout()
    {
        if (appleAuth != null)
        {
            appleAuth.SignOut(revokeAccessToken: true);
            appleAuth = null;

            LogInPlatForm = eLogInPlatForm.None;
            IsGetUser = false;
        }
        PlayerPrefs.DeleteKey(StringDefine.KEY_PREFS_ACCOUNT_APPLE_ID);
        PlayerPrefs.Save();
        Debug.Log("애플 로그아웃 완료");
    }

    // 애플 로그인 요청 (UIManager에서 호출)
    public void RequestAppleLogin(Action callBack = null)
    {
        if (appleAuth == null)
            appleAuth = new AppleAuth();

        requestCallBack = callBack;

        appleAuth.SignIn(OnAppleSignIn, caching: true);
    }
    #endregion Apple Account

    #region Request Login
    private async UniTask RequestLogin(LoginInDto dto, Action<bool> onEventFinish = null)
    {
        // NetworkManager.Instance.Web.Initialize();

        // // 주소 입력 필요
        // NetworkManager.Instance.Web.SetUrl(GameServerConfig.BaseUrl);

        // var loginProcess = NetworkManager.Instance.Web.GetProcess(WebProcess.Login);

        // loginProcess.SetPacket(dto);

        // if (await loginProcess.OnNetworkAsyncRequest())
        // {
        //     var response = loginProcess.GetResponse<LoginResponse>();

        //     loginProcess.OnNetworkResponse();

        //     RequestGetUser(OnSignIn).Forget();

        //     onEventFinish?.Invoke(true);
        //     return;
        // }

        onEventFinish?.Invoke(false);
    }


    private async UniTask RequestGetUser(Action<bool> onEventFinish = null)
    {
        // NetworkManager.Instance.Web.Initialize();

        // // 주소 입력 필요
        // NetworkManager.Instance.Web.SetUrl(GameServerConfig.BaseUrl);
        // NetworkManager.Instance.Web.SetAuthorizationKey();


        // var getUserProcess = NetworkManager.Instance.Web.GetProcess(WebProcess.GetUser);

        // IsGetUser = false;
        // if (await getUserProcess.OnNetworkAsyncRequest())
        // {
        //     var response = getUserProcess.GetResponse<GetUserResponse>();

        //     getUserProcess.OnNetworkResponse();

        //     IsGetUser = true;
        //     GetUserOutDto = response.data;


        //     //유저 정보 갱신..
        //     Debug.Log(GetUserOutDto.userDetail);



        //     onEventFinish?.Invoke(true);
        //     return;
        // }

        onEventFinish?.Invoke(false);
    }
    #endregion Request Login
}
