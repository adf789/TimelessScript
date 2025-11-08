#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

public class ADBConnectorEditor : EditorWindow
{
    private string pairingIP = "192.168.219.100:00000";
    private string pairingCode = "";
    private string connectIP = "192.168.219.100:0000";
    private string statusMessage = "";
    private bool showHelp = true;
    private bool showDebugLog = false;
    private bool showAdvanced = false;
    private string customCommand = "";
    private string currentAdbPath = "";
    private string customAdbPath = "";
    private Vector2 scrollPosition;

    [MenuItem("TS/ADB Connector")]
    public static void ShowWindow()
    {
        GetWindow<ADBConnectorEditor>("ADB Connector");
    }

    private void OnEnable()
    {
        currentAdbPath = GetADBPath();
        AutoFillConnectedAndPairingIPs();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("🔌 Android ADB 무선 디버깅 도구", EditorStyles.boldLabel);

        // ADB 경로 정보 표시
        DrawADBPathSection();
        EditorGUILayout.Space(5);

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

        // 고급 기능
        DrawAdvancedSection();

        EditorGUILayout.Space(10);
        ShowStatusMessage();

        EditorGUILayout.EndScrollView();
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

    private void DrawAdvancedSection()
    {
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "🔧 고급 기능", true);
        if (!showAdvanced) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 직접 명령 실행
        GUILayout.Label("💻 직접 명령 실행", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        customCommand = EditorGUILayout.TextField("ADB 명령", customCommand);
        if (GUILayout.Button("실행", GUILayout.Width(60)))
        {
            string adbPath = GetCurrentADBPath();
            if (IsADBAvailable(adbPath) && !string.IsNullOrEmpty(customCommand))
            {
                RunADBCommand(adbPath, customCommand);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 상세 로그 토글
        showDebugLog = EditorGUILayout.Toggle("상세 디버그 로그 표시", showDebugLog);

        EditorGUILayout.EndVertical();
    }

    private string GetCurrentADBPath()
    {
        return string.IsNullOrEmpty(customAdbPath) ? currentAdbPath : customAdbPath;
    }

    private void AutoFillConnectedAndPairingIPs()
    {
        string adbPath = GetCurrentADBPath();
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

    private void DrawADBPathSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("⚙️ ADB 설정", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("현재 ADB 경로", currentAdbPath);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📂 ADB 경로 수동 선택"))
        {
            string path = EditorUtility.OpenFilePanel("ADB 실행 파일 선택", "", "exe");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                customAdbPath = path;
                currentAdbPath = path;
                statusMessage = $"✅ ADB 경로가 변경되었습니다:\n{path}";
            }
        }

        if (GUILayout.Button("🔄 기본 경로로 복원"))
        {
            customAdbPath = "";
            currentAdbPath = GetADBPath();
            statusMessage = "✅ 기본 ADB 경로로 복원되었습니다.";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawPairingSection()
    {
        GUILayout.Label("📡 ADB 페어링", EditorStyles.boldLabel);

        pairingIP = EditorGUILayout.TextField("페어링 IP:포트", pairingIP);
        pairingCode = EditorGUILayout.TextField("페어링 코드", pairingCode);

        if (GUILayout.Button("ADB Pair"))
        {
            string adbPath = GetCurrentADBPath();
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
            string adbPath = GetCurrentADBPath();
            if (IsADBAvailable(adbPath))
            {
                RunADBCommand(adbPath, $"connect {connectIP}");
            }
        }

        if (GUILayout.Button("ADB Disconnect"))
        {
            string adbPath = GetCurrentADBPath();
            if (IsADBAvailable(adbPath))
            {
                RunADBCommand(adbPath, $"disconnect {connectIP}");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 ADB 서버 재시작"))
        {
            string adbPath = GetCurrentADBPath();
            if (IsADBAvailable(adbPath))
            {
                RestartADBServer(adbPath);
            }
        }

        if (GUILayout.Button("🛑 ADB 서버 종료"))
        {
            string adbPath = GetCurrentADBPath();
            if (IsADBAvailable(adbPath))
            {
                KillADBServer(adbPath);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⚠️ 모든 ADB 강제 종료"))
        {
            ForceKillAllADB();
        }

        if (GUILayout.Button("🔍 ADB 버전 확인"))
        {
            string adbPath = GetCurrentADBPath();
            if (IsADBAvailable(adbPath))
            {
                RunADBCommand(adbPath, "version");
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("📋 연결된 기기 목록"))
        {
            string adbPath = GetCurrentADBPath();
            if (IsADBAvailable(adbPath))
            {
                RunADBCommand(adbPath, "devices -l");
            }
        }
    }

    private void RestartADBServer(string adbPath)
    {
        // ADB 서버 종료
        RunProcess(adbPath, "kill-server");
        System.Threading.Thread.Sleep(500); // 500ms 대기

        // ADB 서버 시작
        var (_, startError) = RunProcess(adbPath, "start-server");

        if (string.IsNullOrEmpty(startError))
        {
            statusMessage = "✅ ADB 서버가 재시작되었습니다.\nProtocol fault 문제가 해결되었을 수 있습니다.";
        }
        else
        {
            statusMessage = $"❗ ADB 서버 재시작 중 오류:\n{startError}";
        }
    }

    private void KillADBServer(string adbPath)
    {
        var (_, error) = RunProcess(adbPath, "kill-server");

        if (string.IsNullOrEmpty(error))
        {
            statusMessage = "✅ ADB 서버가 종료되었습니다.\n필요 시 다시 시작됩니다.";
        }
        else
        {
            statusMessage = $"❗ ADB 서버 종료 중 오류:\n{error}";
        }
    }

    private void ForceKillAllADB()
    {
        try
        {
            using Process killProcess = new();
            killProcess.StartInfo.FileName = "taskkill";
            killProcess.StartInfo.Arguments = "/F /IM adb.exe";
            killProcess.StartInfo.UseShellExecute = false;
            killProcess.StartInfo.RedirectStandardOutput = true;
            killProcess.StartInfo.RedirectStandardError = true;
            killProcess.StartInfo.CreateNoWindow = true;

            killProcess.Start();
            string output = killProcess.StandardOutput.ReadToEnd();
            string error = killProcess.StandardError.ReadToEnd();
            killProcess.WaitForExit();

            if (killProcess.ExitCode == 0)
            {
                statusMessage = "✅ 모든 ADB 프로세스가 강제 종료되었습니다.\n다시 연결을 시도해보세요.";
            }
            else if (error.Contains("not found") || error.Contains("찾을 수 없습니다"))
            {
                statusMessage = "ℹ️ 실행 중인 ADB 프로세스가 없습니다.";
            }
            else
            {
                statusMessage = $"❗ 프로세스 종료 실패:\n{error}";
            }
        }
        catch (System.Exception ex)
        {
            statusMessage = $"❗ 예외 발생:\n{ex.Message}";
        }
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
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

            process.Start();

            if (input != null)
            {
                process.StandardInput.WriteLine(input);
                process.StandardInput.Close();
            }

            // 비동기로 읽어서 데드락 방지
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            // Exit code 확인 - 0이면 성공
            int exitCode = process.ExitCode;

            // Exit code가 0이면 error는 무시 (ADB는 정상 메시지도 stderr로 출력함)
            if (exitCode == 0)
            {
                return (output + error, null); // error에도 유용한 정보가 있을 수 있음
            }

            return (output, error);
        }
        catch (System.Exception ex)
        {
            return (null, $"Exception: {ex.Message}");
        }
    }

    private void RunADBCommand(string adbPath, string args, string input = null)
    {
        var (output, error, exitCode) = RunProcessWithExitCode(adbPath, args, input);

        if (showDebugLog)
        {
            // 상세 디버그 로그
            statusMessage = $"🔍 디버그 로그\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"명령: adb {args}\n" +
                           $"Exit Code: {exitCode}\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"📤 Output:\n{(string.IsNullOrEmpty(output) ? "(없음)" : output)}\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"📥 Error:\n{(string.IsNullOrEmpty(error) ? "(없음)" : error)}";
            return;
        }

        // error가 있어도 output에 성공 메시지가 있으면 성공으로 처리
        bool hasSuccessMessage = !string.IsNullOrEmpty(output) &&
            (output.Contains("Successfully") ||
             output.Contains("connected") ||
             output.Contains("paired") ||
             output.Contains("already connected"));

        if (!string.IsNullOrEmpty(error) && !hasSuccessMessage)
        {
            statusMessage = $"❗ ADB 오류:\n{error}";
        }
        else
        {
            string message = !string.IsNullOrEmpty(output) ? output : error;
            statusMessage = $"✅ 명령 실행 결과:\n{message}";
        }
    }

    private (string output, string error, int exitCode) RunProcessWithExitCode(string filePath, string arguments, string input = null)
    {
        try
        {
            using Process process = new();
            process.StartInfo.FileName = filePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = input != null;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

            process.Start();

            if (input != null)
            {
                process.StandardInput.WriteLine(input);
                process.StandardInput.Close();
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();
            int exitCode = process.ExitCode;

            // Exit code가 0이면 error는 무시 (ADB는 정상 메시지도 stderr로 출력함)
            if (exitCode == 0)
            {
                return (output + error, null, exitCode);
            }

            return (output, error, exitCode);
        }
        catch (System.Exception ex)
        {
            return (null, $"Exception: {ex.Message}", -1);
        }
    }
}
#endif