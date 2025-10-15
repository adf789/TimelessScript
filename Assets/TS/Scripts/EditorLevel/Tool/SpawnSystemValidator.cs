#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SpawnSystemValidator : EditorWindow
{
    [MenuItem("TS/Spawn System/Validator")]
    public static void ShowWindow()
    {
        GetWindow<SpawnSystemValidator>("Spawn System Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Spawn System Validation", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This spawn system implementation includes the following components:\n\n" +
            "• SpawnConfigComponent: Main spawn configuration\n" +
            "• SpawnAreaComponent: Defines spawn area and ground detection\n" +
            "• SpawnRequestComponent: Temporary spawn request\n" +
            "• SpawnedObjectComponent: Tracks spawned objects\n" +
            "• SpawnedEntityBuffer: Buffer for tracking spawned entities\n\n" +
            "Jobs:\n" +
            "• SpawnJob: Handles spawn logic and ground detection\n" +
            "• SpawnExecutionJob: Creates spawned objects\n" +
            "• GroundDetectionJob: Finds valid ground positions\n\n" +
            "System:\n" +
            "• SpawnSystem: Orchestrates all spawn-related jobs",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Sample Spawn Setup"))
        {
            CreateSampleSpawnSetup();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Usage Instructions:\n\n" +
            "1. Create a GameObject for the spawner\n" +
            "2. Add SpawnConfigAuthoring component\n" +
            "3. Assign a prefab with TSObjectAuthoring to 'spawnObjectPrefab'\n" +
            "4. Configure spawn parameters (max count, cooldown, etc.)\n" +
            "5. Set up spawn area and ground detection settings\n" +
            "6. The system will automatically spawn objects on terrain with GroundComponent and ColliderComponent\n\n" +
            "하위 오브젝트 생성:\n" +
            "• 프리팹의 모든 하위 오브젝트들이 자동으로 함께 생성됩니다\n" +
            "• 프리팹은 TSObjectAuthoring 컴포넌트가 있어야 합니다\n" +
            "• 하위 오브젝트들도 필요한 Authoring 컴포넌트들을 가져야 합니다",
            MessageType.Info);
    }

    private void CreateSampleSpawnSetup()
    {
        GameObject spawner = new GameObject("Gimmick Spawner");
        var spawnConfig = spawner.AddComponent<SpawnConfigAuthoring>();

        Selection.activeGameObject = spawner;
        EditorGUIUtility.PingObject(spawner);

        Debug.Log("Sample spawn setup created. Please assign a spawn object prefab in the inspector.");
    }
}
#endif