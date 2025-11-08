using UnityEngine;

/// <summary>
/// OAuth 인증 설정 (암호화된 민감 정보 저장)
/// Git에는 암호화된 값만 저장되며, 로컬 키로 복호화
/// </summary>
[CreateAssetMenu(fileName = "AuthSetting", menuName = "TS/Authentication/AuthSetting")]
public class AuthSetting : ScriptableObject
{
    // 런타임에서 복호화된 값을 반환
    public string EditorClientId => Utility.Encryption.Decrypt(encryptedClientId);
    public string EditorClientSecret => Utility.Encryption.Decrypt(encryptedClientSecret);
    public int EditorRedirectPort => editorRedirectPort;

    [Header("Encrypted Values (Safe for Git)")]
    [SerializeField] private string encryptedClientId = "";
    [SerializeField] private string encryptedClientSecret = "";

    [Header("Settings")]
    [SerializeField] private int editorRedirectPort = 8080; // 고정 포트 (Google Console에 등록 필요)

#if UNITY_EDITOR
    /// <summary>
    /// 평문 값을 암호화해서 저장 (에디터 전용)
    /// </summary>
    public void SetEncryptedClientId(string plainText)
    {
        encryptedClientId = Utility.Encryption.Encrypt(plainText);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// 평문 값을 암호화해서 저장 (에디터 전용)
    /// </summary>
    public void SetEncryptedClientSecret(string plainText)
    {
        encryptedClientSecret = Utility.Encryption.Encrypt(plainText);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// 암호화된 값이 설정되어 있는지 확인
    /// </summary>
    public bool HasEncryptedValues()
    {
        return !string.IsNullOrEmpty(encryptedClientId) && !string.IsNullOrEmpty(encryptedClientSecret);
    }
#endif
}
