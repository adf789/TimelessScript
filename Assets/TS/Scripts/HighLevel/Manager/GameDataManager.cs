using UnityEngine;
using Firebase.Database;
using System.Threading.Tasks;

public class GameDataManager : BaseManager<GameDataManager>
{
    private DatabaseReference dbRef;
    private string userId = "player001"; // 실제로는 고유 ID 사용

    void Start()
    {
        // Firebase가 준비될 때까지 대기
        if (FirebaseInitializeSupport.Instance.IsFirebaseReady)
        {
            dbRef = FirebaseInitializeSupport.Instance.DBReference;
        }
    }

    // 데이터 저장
    public async Task SaveGameData(GameData data)
    {
        if (dbRef == null) return;

        try
        {
            string json = JsonUtility.ToJson(data);
            await dbRef.Child("players").Child(userId).SetRawJsonValueAsync(json);
            Debug.Log("데이터 저장 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"저장 실패: {e.Message}");
        }
    }

    // 데이터 불러오기
    public async Task<GameData> LoadGameData()
    {
        if (dbRef == null) return null;

        try
        {
            var snapshot = await dbRef.Child("players").Child(userId).GetValueAsync();

            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                GameData data = JsonUtility.FromJson<GameData>(json);
                Debug.Log("데이터 로드 완료!");
                return data;
            }
            else
            {
                Debug.Log("저장된 데이터가 없습니다.");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로드 실패: {e.Message}");
            return null;
        }
    }

    // 특정 필드만 업데이트
    public async Task UpdateCoins(int coins)
    {
        if (dbRef == null) return;

        try
        {
            await dbRef.Child("players").Child(userId).Child("coins").SetValueAsync(coins);
            Debug.Log($"코인 업데이트 완료: {coins}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"업데이트 실패: {e.Message}");
        }
    }
}

// 게임 데이터 구조
[System.Serializable]
public class GameData
{
    public int level;
    public int coins;
    public int experience;
    public string lastPlayDate;

    public GameData(int level, int coins, int exp)
    {
        this.level = level;
        this.coins = coins;
        this.experience = exp;
        this.lastPlayDate = System.DateTime.Now.ToString();
    }
}