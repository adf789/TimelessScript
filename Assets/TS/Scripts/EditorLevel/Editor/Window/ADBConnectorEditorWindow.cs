#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

public class ADBConnectorEditor : EditorWindow
{
    private string pairingIP = "192.168.0.0:00000";
    private string pairingCode = "";
    private string connectIP = "192.168.0.0:0000";
    private string statusMessage = "";
    private bool showHelp = true;

    [MenuItem("TS/ADB Connector")]
    public static void ShowWindow()
    {
        GetWindow<ADBConnectorEditor>("ADB Connector");
    }

    private void OnEnable()
    {
        AutoFillConnectedAndPairingIPs();
    }

    private void OnGUI()
    {
        GUILayout.Label("🔌 Android ADB 무선 디버깅 도구", EditorStyles.boldLabel);

        showHelp = EditorGUILayout.Foldout(showHelp, "사용 방법 안내", true);
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "안드로이드 기기와 PC를 무선으로 연결하기 위한 ADB 페어링 및 연결 과정을 반자동화하기 위한 툴입니다.\n\n" +
                "1. 개발자 옵션 활성화 : 설정 > 휴대전화 정보 > 소프트웨어 정보 > 빌드번호 를 연속으로 누르기. (휴대전화 암호 입력 필요)\n" +
                "2. 무선 디버깅 활성화 : 설정 > 개발자 옵션 > 무선 디버깅 클릭하여 활성화하기.\n" +
                "3. 기기와 페어링 하기 : 설정 > 개발자 옵션 > 무선 디버깅 > 페어링 코드로 기기 페어링 클릭.\n" +
                "4. 기기 페어링 화면에서 페어링코드와 IP 주소 및 포트 확인하여, 페어링 IP:포트 및 페어링 코드 입력 후 'ADB Pair 클릭'\n" +
                "5. 정상적으로 페어링되면 휴대전화에 페어링기기가 표시되고 IP 주소 및 포트가 표시됨을 확인.\n" +
                "6. 연결 IP:포트 입력 후 'ADB Connect' 클릭\n" +
                "7. 사용 후 연결을 해제하려면 'ADB Disconnect' 클릭",
                MessageType.Info);
        }

        EditorGUILayout.Space(10);

        DrawPairingSection();
        EditorGUILayout.Space(10);
        DrawConnectionSection();

        EditorGUILayout.Space(10);
        ShowStatusMessage();
    }

    private void ShowStatusMessage()
    {
        MessageType messageType = MessageType.Info;
        if (statusMessage.StartsWith("❗") || statusMessage.StartsWith("Exception") || statusMessage.StartsWith("예외"))
            messageType = MessageType.Error;
        else if (statusMessage.StartsWith("✅"))
            messageType = MessageType.None;

        EditorGUILayout.HelpBox(statusMessage, messageType);
    }

    private void AutoFillConnectedAndPairingIPs()
    {
        string adbPath = GetADBPath();
        if (!IsADBAvailable(adbPath))
        {
            statusMessage = "❌ ADB 실행 파일을 찾을 수 없습니다.";
            return;
        }

        var (output, error) = RunProcess(adbPath, "devices");
        if (!string.IsNullOrEmpty(error))
        {
            statusMessage = $"❗ ADB 오류:\n{error}";
            return;
        }

        string foundConnectIP = null;
        string[] lines = output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (line.Contains(":") && line.Contains("device"))
            {
                string[] parts = line.Trim().Split('\t');
                if (parts.Length > 0)
                {
                    foundConnectIP = parts[0];
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(foundConnectIP))
        {
            connectIP = foundConnectIP;

            int colonIndex = foundConnectIP.IndexOf(':');
            if (colonIndex >= 0)
            {
                string ipOnly = foundConnectIP.Substring(0, colonIndex);
                pairingIP = $"{ipOnly}:37000";
            }
            else
            {
                pairingIP = $"{foundConnectIP}:37000";
            }

            statusMessage = $"✅ 연결 IP와 페어링 IP가 자동으로 설정되었습니다.";
        }
        else
        {
            statusMessage = "❗ 연결된 무선 디바이스가 없습니다.";
        }
    }

    private void DrawPairingSection()
    {
        GUILayout.Label("📡 ADB 페어링", EditorStyles.boldLabel);

        pairingIP = EditorGUILayout.TextField("페어링 IP:포트", pairingIP);
        pairingCode = EditorGUILayout.TextField("페어링 코드", pairingCode);

        if (GUILayout.Button("ADB Pair"))
        {
            string adbPath = GetADBPath();
            if (IsADBAvailable(adbPath))
            {
                RunADBCommand(adbPath, $"pair {pairingIP}", pairingCode);
            }
        }
    }

    private void DrawConnectionSection()
    {
        GUILayout.Label("🔗 ADB 연결", EditorStyles.boldLabel);

        connectIP = EditorGUILayout.TextField("연결 IP:포트", connectIP);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("ADB Connect"))
        {
            string adbPath = GetADBPath();
            if (IsADBAvailable(adbPath))
            {
                RunADBCommand(adbPath, $"connect {connectIP}");
            }
        }

        if (GUILayout.Button("ADB Disconnect"))
        {
            string adbPath = GetADBPath();
            if (IsADBAvailable(adbPath))
            {
                RunADBCommand(adbPath, $"disconnect {connectIP}");
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private bool IsADBAvailable(string adbPath)
    {
        if (!File.Exists(adbPath))
        {
            statusMessage = "❌ ADB 파일을 찾을 수 없습니다!\nAndroid SDK가 설치되어 있는지 확인하세요.";
            return false;
        }
        return true;
    }

    private string GetADBPath()
    {
        string adbExeName = Application.platform == RuntimePlatform.WindowsEditor ? "adb.exe" : "adb";

        string sdkFromPrefs = EditorPrefs.GetString("AndroidSdkRoot");
        if (!string.IsNullOrEmpty(sdkFromPrefs))
        {
            string adbFromPrefs = Path.Combine(sdkFromPrefs, "platform-tools", adbExeName);
            if (File.Exists(adbFromPrefs)) return adbFromPrefs;
        }

        string editorPath = EditorApplication.applicationPath;
        string editorDir = Path.GetDirectoryName(editorPath);
        string sdkPath = Path.Combine(editorDir, "Data", "PlaybackEngines", "AndroidPlayer", "SDK");
        string adbPath = Path.Combine(sdkPath, "platform-tools", adbExeName);

        return adbPath;
    }

    private (string output, string error) RunProcess(string filePath, string arguments, string input = null)
    {
        try
        {
            using Process process = new Process();
            process.StartInfo.FileName = filePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = input != null;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            if (input != null)
            {
                process.StandardInput.WriteLine(input);
                process.StandardInput.Close();
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return (output, error);
        }
        catch (System.Exception ex)
        {
            return (null, $"Exception: {ex.Message}");
        }
    }

    private void RunADBCommand(string adbPath, string args, string input = null)
    {
        var (output, error) = RunProcess(adbPath, args, input);
        if (!string.IsNullOrEmpty(error))
            statusMessage = $"❗ ADB 오류:\n{error}";
        else
            statusMessage = $"✅ 명령 실행 성공:\n{output}";
    }
}
#endif