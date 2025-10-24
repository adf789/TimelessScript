#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// AuthSetting 커스텀 인스펙터 - 암호화/복호화 UI 제공
/// </summary>
[CustomEditor(typeof(AuthSetting))]
public class AuthSettingEditor : UnityEditor.Editor
{
    private bool isDecrypted = false;
    private string tempClientId = "";
    private string tempClientSecret = "";

    public override void OnInspectorGUI()
    {
        AuthSetting authSetting = (AuthSetting) target;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "🔐 OAuth 인증 정보는 암호화되어 저장됩니다.\n" +
            "Git에는 암호화된 값만 업로드되며, 로컬 키로만 복호화됩니다.",
            MessageType.Info
        );
        EditorGUILayout.Space(5);

        // // 암호화 키 상태 표시
        // if (!EncryptionUtility.HasEncryptionKey())
        // {
        //     EditorGUILayout.HelpBox(
        //         "⚠️ 암호화 키가 없습니다. 처음 암호화 시 자동 생성됩니다.",
        //         MessageType.Warning
        //     );
        // }
        // else
        // {
        //     EditorGUILayout.HelpBox(
        //         $"✅ 암호화 키: {EncryptionUtility.GetKeyFilePath()}",
        //         MessageType.None
        //     );
        // }

        EditorGUILayout.Space(10);

        // 암호화된 값이 있는지 확인
        bool hasEncryptedValues = authSetting.HasEncryptedValues();

        if (!isDecrypted)
        {
            // 암호화된 상태 - 읽기 전용 표시
            EditorGUILayout.LabelField("Encrypted Client ID", hasEncryptedValues ? "●●●●●●●●●●●●" : "Empty");
            EditorGUILayout.LabelField("Encrypted Client Secret", hasEncryptedValues ? "●●●●●●●●●●●●" : "Empty");

            EditorGUILayout.Space(10);

            if (GUILayout.Button("🔓 Decrypt & Edit", GUILayout.Height(30)))
            {
                if (hasEncryptedValues)
                {
                    tempClientId = authSetting.EditorClientId;
                    tempClientSecret = authSetting.EditorClientSecret;
                }
                isDecrypted = true;
            }
        }
        else
        {
            // 복호화된 상태 - 편집 가능
            EditorGUILayout.LabelField("Plain Text (Edit Mode)", EditorStyles.boldLabel);

            tempClientId = EditorGUILayout.TextField("Client ID", tempClientId);
            tempClientSecret = EditorGUILayout.TextField("Client Secret", tempClientSecret);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔒 Encrypt & Save", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(tempClientId) || string.IsNullOrEmpty(tempClientSecret))
                {
                    EditorUtility.DisplayDialog("Validation Error",
                        "Client ID와 Secret을 모두 입력해주세요.", "OK");
                }
                else
                {
                    authSetting.SetEncryptedClientId(tempClientId);
                    authSetting.SetEncryptedClientSecret(tempClientSecret);

                    EditorUtility.SetDirty(authSetting);
                    AssetDatabase.SaveAssets();

                    isDecrypted = false;
                    tempClientId = "";
                    tempClientSecret = "";

                    Debug.Log("[AuthSetting] 암호화 완료. Git에 안전하게 커밋할 수 있습니다.");
                }
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                isDecrypted = false;
                tempClientId = "";
                tempClientSecret = "";
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        // 기본 Inspector 그리기 (editorRedirectPort)
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "⚙️ 사용 방법:\n" +
            "1. 'Decrypt & Edit' 클릭\n" +
            "2. Client ID와 Secret 입력\n" +
            "3. 'Encrypt & Save' 클릭\n" +
            "4. Git에 커밋 (암호화된 값만 업로드됨)\n\n" +
            "⚠️ 주의: .encryption_key 파일은 Git에 업로드하지 마세요!",
            MessageType.None
        );
    }
}
#endif
