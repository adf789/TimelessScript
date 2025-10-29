using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using Firebase;
using Firebase.Database;
#endif

/// <summary>
/// Firebase Realtime Database 중앙 관리 Manager
/// - DatabaseReference 제공
/// - Firebase 초기화 및 상태 관리
/// - HighLevel에서 Database 작업 통합 처리
/// </summary>
public class DatabaseManager : SubBaseManager<DatabaseManager>
{
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private FirebaseApp _firebaseApp;
    private DatabaseReference _dbReference;
    private bool _isInitialized;

    /// <summary>
    /// Firebase Database Root Reference
    /// </summary>
    public DatabaseReference DBReference => _dbReference;

    /// <summary>
    /// Firebase 초기화 완료 여부
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Firebase 비동기 초기화
    /// </summary>
    public async UniTask InitializeFirebaseAsync()
    {
        try
        {
            Debug.Log("[DatabaseManager] Starting Firebase initialization...");

            // Firebase 의존성 체크 및 수정
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase 초기화 성공
                _firebaseApp = FirebaseApp.DefaultInstance;
                _dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                _isInitialized = true;

                Debug.Log("[DatabaseManager] Firebase initialized successfully!");
                Debug.Log($"[DatabaseManager] Database URL: {FirebaseDatabase.DefaultInstance.App.Options.DatabaseUrl}");
            }
            else
            {
                Debug.LogError($"[DatabaseManager] Firebase initialization failed: {dependencyStatus}");
                _isInitialized = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Firebase initialization error: {ex.Message}");
            _isInitialized = false;
        }
    }

    /// <summary>
    /// 특정 경로의 DatabaseReference 가져오기
    /// </summary>
    /// <param name="path">데이터베이스 경로 (예: "users/123")</param>
    /// <returns>DatabaseReference 또는 null (초기화 실패 시)</returns>
    public DatabaseReference GetReference(string path)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseManager] Firebase not initialized. Cannot get reference.");
            return null;
        }

        if (string.IsNullOrEmpty(path))
        {
            return _dbReference; // Root reference
        }

        return _dbReference.Child(path);
    }

    /// <summary>
    /// 데이터 읽기 (비동기)
    /// </summary>
    public async UniTask<DataSnapshot> GetDataAsync(string path)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseManager] Firebase not initialized.");
            return null;
        }

        try
        {
            var reference = GetReference(path);
            var snapshot = await reference.GetValueAsync();
            return snapshot;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Failed to read data from '{path}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 데이터 쓰기 (비동기)
    /// </summary>
    public async UniTask<bool> SetDataAsync(string path, object value)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseManager] Firebase not initialized.");
            return false;
        }

        try
        {
            var reference = GetReference(path);
            await reference.SetValueAsync(value);
            Debug.Log($"[DatabaseManager] Data written successfully to '{path}'");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Failed to write data to '{path}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 데이터 업데이트 (비동기)
    /// </summary>
    public async UniTask<bool> UpdateDataAsync(string path, System.Collections.Generic.Dictionary<string, object> updates)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseManager] Firebase not initialized.");
            return false;
        }

        try
        {
            var reference = GetReference(path);
            await reference.UpdateChildrenAsync(updates);
            Debug.Log($"[DatabaseManager] Data updated successfully at '{path}'");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Failed to update data at '{path}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 데이터 삭제 (비동기)
    /// </summary>
    public async UniTask<bool> DeleteDataAsync(string path)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseManager] Firebase not initialized.");
            return false;
        }

        try
        {
            var reference = GetReference(path);
            await reference.RemoveValueAsync();
            Debug.Log($"[DatabaseManager] Data deleted successfully at '{path}'");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseManager] Failed to delete data at '{path}': {ex.Message}");
            return false;
        }
    }

#else
    public bool IsInitialized => false;

    private void Start()
    {
        Debug.LogWarning("[DatabaseManager] Firebase is only supported on Android, iOS, and Editor platforms.");
    }

    public object GetReference(string path)
    {
        Debug.LogWarning("[DatabaseManager] Firebase not available on this platform.");
        return null;
    }
#endif
}
