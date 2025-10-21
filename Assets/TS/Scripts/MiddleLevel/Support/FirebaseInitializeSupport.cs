using UnityEngine;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;

public class FirebaseInitializeSupport : MonoBehaviour
{
    public static FirebaseInitializeSupport Instance { get; private set; }

    private DatabaseReference dbReference;
    public DatabaseReference DBReference => dbReference;

    private bool isFirebaseReady = false;
    public bool IsFirebaseReady => isFirebaseReady;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void InitializeFirebase()
    {
        // Firebase 의존성 체크
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus == DependencyStatus.Available)
        {
            // Firebase 초기화 성공
            FirebaseApp app = FirebaseApp.DefaultInstance;
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            isFirebaseReady = true;

            Debug.Log("Firebase 초기화 완료!");
        }
        else
        {
            Debug.LogError($"Firebase 초기화 실패: {dependencyStatus}");
            isFirebaseReady = false;
        }
    }
}