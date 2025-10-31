using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using Firebase;
using Firebase.Firestore;
#endif

/// <summary>
/// Firebase Firestore 중앙 관리 SubManager
/// - Firestore Database 제공
/// - Firebase 초기화 및 상태 관리
/// - 문서(Document) 및 컬렉션(Collection) CRUD 작업
/// </summary>
public class DatabaseSubManager : SubBaseManager<DatabaseSubManager>
{
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private FirebaseApp _firebaseApp;
    private FirebaseFirestore _firestore;
    private bool _isInitialized;

    /// <summary>
    /// Firestore Database Instance
    /// </summary>
    public FirebaseFirestore Firestore => _firestore;

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
            Debug.Log("[DatabaseSubManager] Starting Firebase initialization...");

            // Firebase 의존성 체크 및 수정
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase 초기화 성공
                _firebaseApp = FirebaseApp.DefaultInstance;
                _firestore = FirebaseFirestore.DefaultInstance;
                _isInitialized = true;

                Debug.Log("[DatabaseSubManager] Firebase Firestore initialized successfully!");
                Debug.Log($"[DatabaseSubManager] App Name: {_firebaseApp.Name}");
            }
            else
            {
                Debug.LogError($"[DatabaseSubManager] Firebase initialization failed: {dependencyStatus}");
                _isInitialized = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Firebase initialization error: {ex.Message}");
            _isInitialized = false;
        }
    }

    /// <summary>
    /// 컬렉션 참조 가져오기
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로 (예: "users")</param>
    public CollectionReference GetCollection(string collectionPath)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized. Cannot get collection.");
            return null;
        }

        if (string.IsNullOrEmpty(collectionPath))
        {
            Debug.LogWarning("[DatabaseSubManager] Collection path is empty.");
            return null;
        }

        return _firestore.Collection(collectionPath);
    }

    /// <summary>
    /// 문서 참조 가져오기
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    /// <param name="documentId">문서 ID</param>
    public DocumentReference GetDocument(string collectionPath, string documentId)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized. Cannot get document.");
            return null;
        }

        if (string.IsNullOrEmpty(collectionPath) || string.IsNullOrEmpty(documentId))
        {
            Debug.LogWarning("[DatabaseSubManager] Collection path or document ID is empty.");
            return null;
        }

        return _firestore.Collection(collectionPath).Document(documentId);
    }

    /// <summary>
    /// 문서 읽기 (비동기)
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    /// <param name="documentId">문서 ID</param>
    /// <returns>DocumentSnapshot 또는 null</returns>
    public async UniTask<DocumentSnapshot> GetDocumentAsync(string collectionPath, string documentId)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized.");
            return null;
        }

        try
        {
            var docRef = GetDocument(collectionPath, documentId);
            var snapshot = await docRef.GetSnapshotAsync().AsUniTask();

            if (snapshot.Exists)
            {
                Debug.Log($"[DatabaseSubManager] Document '{collectionPath}/{documentId}' read successfully.");
                return snapshot;
            }
            else
            {
                Debug.LogWarning($"[DatabaseSubManager] Document '{collectionPath}/{documentId}' does not exist.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Failed to read document '{collectionPath}/{documentId}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 문서 쓰기/덮어쓰기 (비동기)
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    /// <param name="documentId">문서 ID</param>
    /// <param name="data">저장할 데이터 (Dictionary 또는 직렬화 가능한 객체)</param>
    public async UniTask<bool> SetDocumentAsync(string collectionPath, string documentId, object data)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized.");
            return false;
        }

        try
        {
            var docRef = GetDocument(collectionPath, documentId);
            await docRef.SetAsync(data).AsUniTask();
            Debug.Log($"[DatabaseSubManager] Document '{collectionPath}/{documentId}' written successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Failed to write document '{collectionPath}/{documentId}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 문서 업데이트 (필드 부분 수정, 비동기)
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    /// <param name="documentId">문서 ID</param>
    /// <param name="updates">업데이트할 필드와 값</param>
    public async UniTask<bool> UpdateDocumentAsync(string collectionPath, string documentId, Dictionary<string, object> updates)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized.");
            return false;
        }

        try
        {
            var docRef = GetDocument(collectionPath, documentId);
            await docRef.UpdateAsync(updates).AsUniTask();
            Debug.Log($"[DatabaseSubManager] Document '{collectionPath}/{documentId}' updated successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Failed to update document '{collectionPath}/{documentId}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 문서 삭제 (비동기)
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    /// <param name="documentId">문서 ID</param>
    public async UniTask<bool> DeleteDocumentAsync(string collectionPath, string documentId)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized.");
            return false;
        }

        try
        {
            var docRef = GetDocument(collectionPath, documentId);
            await docRef.DeleteAsync().AsUniTask();
            Debug.Log($"[DatabaseSubManager] Document '{collectionPath}/{documentId}' deleted successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Failed to delete document '{collectionPath}/{documentId}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 컬렉션 내 모든 문서 조회 (비동기)
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    public async UniTask<QuerySnapshot> GetCollectionAsync(string collectionPath)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized.");
            return null;
        }

        try
        {
            var collectionRef = GetCollection(collectionPath);
            var snapshot = await collectionRef.GetSnapshotAsync().AsUniTask();
            Debug.Log($"[DatabaseSubManager] Collection '{collectionPath}' retrieved with {snapshot.Count} documents.");
            return snapshot;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Failed to get collection '{collectionPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 조건부 쿼리 실행 (비동기)
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    /// <param name="fieldPath">필터링할 필드명</param>
    /// <param name="value">필터 값</param>
    public async UniTask<QuerySnapshot> QueryDocumentsAsync(string collectionPath, string fieldPath, object value)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized.");
            return null;
        }

        try
        {
            var query = _firestore.Collection(collectionPath).WhereEqualTo(fieldPath, value);
            var snapshot = await query.GetSnapshotAsync().AsUniTask();
            Debug.Log($"[DatabaseSubManager] Query on '{collectionPath}' returned {snapshot.Count} results.");
            return snapshot;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Query failed on '{collectionPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 문서 실시간 리스너 등록
    /// </summary>
    /// <param name="collectionPath">컬렉션 경로</param>
    /// <param name="documentId">문서 ID</param>
    /// <param name="callback">변경 시 호출될 콜백</param>
    public ListenerRegistration ListenToDocument(string collectionPath, string documentId, Action<DocumentSnapshot> callback)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[DatabaseSubManager] Firebase not initialized.");
            return null;
        }

        try
        {
            var docRef = GetDocument(collectionPath, documentId);
            var listener = docRef.Listen(snapshot =>
            {
                if (snapshot.Exists)
                {
                    Debug.Log($"[DatabaseSubManager] Document '{collectionPath}/{documentId}' changed.");
                    callback?.Invoke(snapshot);
                }
            });

            Debug.Log($"[DatabaseSubManager] Listener registered for '{collectionPath}/{documentId}'.");
            return listener;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DatabaseSubManager] Failed to register listener for '{collectionPath}/{documentId}': {ex.Message}");
            return null;
        }
    }

#else
    public bool IsInitialized => false;

    public DatabaseSubManager()
    {
        Debug.LogWarning("[DatabaseSubManager] Firebase is only supported on Android, iOS, and Editor platforms.");
    }

    public object GetCollection(string collectionPath)
    {
        Debug.LogWarning("[DatabaseSubManager] Firebase not available on this platform.");
        return null;
    }

    public object GetDocument(string collectionPath, string documentId)
    {
        Debug.LogWarning("[DatabaseSubManager] Firebase not available on this platform.");
        return null;
    }
#endif
}
