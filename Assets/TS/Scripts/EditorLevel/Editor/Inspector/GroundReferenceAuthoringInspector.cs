
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Entities.Serialization;

[CustomEditor(typeof(GroundReferenceAuthoring))]
public class GroundReferenceAuthoringInspector : Editor
{
    private GroundReferenceAuthoring _inspectorTarget;
    private TilemapPatternData _pattern;
    private SerializedProperty _groundsProperty;
    private SerializedProperty _laddersProperty;
    private SerializedProperty _groundParentProperty;
    private SerializedProperty _ladderParentProperty;
    private Vector2 _groundScrollPosition;
    private Vector2 _ladderScrollPosition;
    private string[] _groundNames;
    private string[] _filteredGroundNames;
    private long[] _ports;
    private int _selectTopGroundIndex;
    private int _selectBottomGroundIndex;
    private void OnEnable()
    {
        _inspectorTarget = (GroundReferenceAuthoring) target;
        _groundsProperty = serializedObject.FindProperty("_grounds");
        _laddersProperty = serializedObject.FindProperty("_ladders");
        _groundParentProperty = serializedObject.FindProperty("_groundParent");
        _ladderParentProperty = serializedObject.FindProperty("_ladderParent");

        InitializeGroundNames();

        InitializePorts();
    }

    private void InitializeGroundNames()
    {
        _groundNames = new string[_groundsProperty.arraySize];
        _selectTopGroundIndex = 0;

        for (int i = 0; i < _groundsProperty.arraySize; i++)
        {
            SerializedProperty element = _groundsProperty.GetArrayElementAtIndex(i);

            // Ground fields
            SerializedProperty groundProp = element.FindPropertyRelative("_ground");

            _groundNames[i] = groundProp.objectReferenceValue != null ? groundProp.objectReferenceValue.name : "None";
        }

        InitializeFilteredGroundNames();
    }

    private void InitializeFilteredGroundNames()
    {
        _selectBottomGroundIndex = 0;
        List<string> groundNames = new List<string>();
        TSGroundAuthoring selectedGround = _groundsProperty
        .GetArrayElementAtIndex(_selectTopGroundIndex)
        .FindPropertyRelative("_ground")
        .objectReferenceValue as TSGroundAuthoring;

        for (int i = 0; i < _groundsProperty.arraySize; i++)
        {
            SerializedProperty element = _groundsProperty.GetArrayElementAtIndex(i);
            SerializedProperty groundProp = element.FindPropertyRelative("_ground");

            if (groundProp.objectReferenceValue is not TSGroundAuthoring ground)
                continue;

            if (selectedGround != null
            && selectedGround.transform.position.y >= ground.transform.position.y)
                continue;

            groundNames.Add(ground.name);
        }

        _filteredGroundNames = groundNames.ToArray();
    }

    private async void InitializePorts()
    {
        if (_ports == null)
            _ports = new long[4];

        if (EditorApplication.isPlaying)
            return;

        if (_pattern == null)
        {
            var registry = await ResourcesTypeRegistry.Get().LoadAsyncWithName<TilemapPatternRegistry>("TilemapPatternRegistry");

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            _pattern = registry.GetPattern(activeScene.name);

            // Scene의 GUID 가져오기
            var scenePath = activeScene.path;
            var guid = AssetDatabase.GUIDFromAssetPath(scenePath);

            var sceneRef = new EntitySceneReference(new Unity.Entities.Hash128(guid.ToString()), 0);

            if (_pattern == null)
            {
                _pattern = registry.AddPattern(activeScene.name, sceneRef);
            }
        }

        bool top = false, bottom = false;
        int topMinX = int.MaxValue, topMaxX = -1, topY = -1;
        int bottomMinX = int.MaxValue, bottomMaxX = -1, bottomY = int.MaxValue;

        for (int i = 0; i < _groundsProperty.arraySize; i++)
        {
            SerializedProperty element = _groundsProperty.GetArrayElementAtIndex(i);
            Vector2Int min = element.FindPropertyRelative("_min").vector2IntValue;
            Vector2Int max = element.FindPropertyRelative("_max").vector2IntValue;

            if (min.x == 0)
                _ports[(int) FourDirection.Left] |= 1L << max.y;

            if (max.x == IntDefine.MAP_TOTAL_GRID_WIDTH - 1)
                _ports[(int) FourDirection.Right] |= 1L << max.y;

            if (max.y > topY)
            {
                topMinX = min.x;
                topMaxX = max.x;
                topY = max.y;
                top = true;
            }

            if (max.y < bottomY)
            {
                bottomMinX = min.x;
                bottomMaxX = max.x;
                bottomY = max.y;
                bottom = true;
            }
        }

        if (top)
        {
            for (int num = topMinX; num <= topMaxX; num++)
            {
                _ports[(int) FourDirection.Up] |= 1L << num;
            }
        }

        if (bottom)
        {
            for (int num = bottomMinX; num <= bottomMaxX; num++)
            {
                _ports[(int) FourDirection.Down] |= 1L << num;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawParents();

        EditorGUILayout.Space();

        DrawPorts();

        EditorGUILayout.Space();

        DrawGroundEntities();

        EditorGUILayout.Space();

        DrawLadderEntities();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawParents()
    {
        EditorGUILayout.LabelField("Object Parent", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_groundParentProperty);
        EditorGUILayout.PropertyField(_ladderParentProperty);
    }

    private void DrawPorts()
    {
        EditorGUILayout.LabelField("Ports", EditorStyles.boldLabel);

        for (FourDirection direction = FourDirection.Up; System.Enum.IsDefined(typeof(FourDirection), direction); direction++)
        {
            EditorGUILayout.LabelField($"{direction}: {_ports[(int) direction]}");
        }
    }

    private void DrawGroundEntities()
    {
        EditorGUILayout.LabelField("Ground Entries", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // Scroll view
        _groundScrollPosition = EditorGUILayout.BeginScrollView(_groundScrollPosition, GUILayout.Height(300));
        {
            for (int i = 0; i < _groundsProperty.arraySize; i++)
            {
                SerializedProperty element = _groundsProperty.GetArrayElementAtIndex(i);

                // Ground fields
                SerializedProperty groundProp = element.FindPropertyRelative("_ground");
                SerializedProperty minProp = element.FindPropertyRelative("_min");
                SerializedProperty maxProp = element.FindPropertyRelative("_max");

                EditorGUILayout.BeginVertical("box");
                {
                    // Header with index and remove button
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField($"Ground [{i}]", EditorStyles.boldLabel);
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            RemoveGroundAtIndex(i);
                            InitializeGroundNames();
                            SetSaveDirty();
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    {
                        EditorGUILayout.PropertyField(groundProp, new GUIContent("지형"));
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginChangeCheck();
                    {
                        EditorGUILayout.PropertyField(minProp, new GUIContent("왼쪽-아래 모서리"));
                        CheckMinRange(minProp);

                        EditorGUILayout.PropertyField(maxProp, new GUIContent("오른쪽-위 모서리"));
                        CheckMaxRange(minProp, maxProp);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        InitializePorts();
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);

                // 지형 위치 지정
                if (InitializeGround(groundProp, minProp, maxProp))
                    SetSaveDirty();
            }
        }
        EditorGUILayout.EndScrollView();

        // Add button
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Add Ground"))
        {
            AddLastGround();
            InitializeGroundNames();
            SetSaveDirty();
        }
    }

    private void DrawLadderEntities()
    {
        EditorGUILayout.LabelField("Ladder Entries", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // Scroll view
        _ladderScrollPosition = EditorGUILayout.BeginScrollView(_ladderScrollPosition, GUILayout.Height(200));
        {
            for (int i = 0; i < _laddersProperty.arraySize; i++)
            {
                SerializedProperty element = _laddersProperty.GetArrayElementAtIndex(i);

                // Ladder fields
                SerializedProperty ladderProp = element.FindPropertyRelative("_ladder");
                SerializedProperty positionProp = element.FindPropertyRelative("_position");

                EditorGUILayout.BeginVertical("box");
                {
                    // Header with index and remove button
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField($"Ladder [{i}]", EditorStyles.boldLabel);
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            RemoveLadderAtIndex(i);
                            InitializeGroundNames();
                            SetSaveDirty();
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    {
                        EditorGUILayout.PropertyField(ladderProp, new GUIContent("사다리"));
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.PropertyField(positionProp, new GUIContent("사다리 포지션"));
                    CheckLadderRange(ladderProp, positionProp);

                    // Link Ground fields
                    if (ladderProp.objectReferenceValue is TSLadderAuthoring ladder)
                    {
                        string topGround = ladder.GetTopConnectGround()?.name;
                        string bottomGround = ladder.GetBottomConnectGround()?.name;

                        EditorGUILayout.LabelField($"Link: {topGround}-{bottomGround}");
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);

                // 지형 위치 지정
                if (InitializeLadder(ladderProp, positionProp))
                    SetSaveDirty();
            }
        }
        EditorGUILayout.EndScrollView();

        // Add button
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Create Ladder", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        {
            _selectTopGroundIndex = EditorGUILayout.Popup("Connect Top Ground", _selectTopGroundIndex, _groundNames);
        }
        if (EditorGUI.EndChangeCheck())
            InitializeFilteredGroundNames();

        if (_filteredGroundNames.Length > 0)
        {
            _selectBottomGroundIndex = EditorGUILayout.Popup("Connect Bottom Ground", _selectBottomGroundIndex, _filteredGroundNames);

            if (GUILayout.Button("Add Ladder"))
            {
                AddLastLadder(GetTopGround(), GetBottomGround());
                InitializeGroundNames();
                SetSaveDirty();
            }
        }
    }

    private TSGroundAuthoring GetTopGround()
    {
        return _groundsProperty
        .GetArrayElementAtIndex(_selectTopGroundIndex)
        .FindPropertyRelative("_ground")
        .objectReferenceValue as TSGroundAuthoring;
    }

    private TSGroundAuthoring GetBottomGround()
    {
        if (_filteredGroundNames.Length == 0 || _filteredGroundNames.Length <= _selectBottomGroundIndex)
            return null;

        string findGroundName = _filteredGroundNames[_selectBottomGroundIndex];

        for (int i = 0; i < _groundsProperty.arraySize; i++)
        {
            SerializedProperty element = _groundsProperty.GetArrayElementAtIndex(i);
            SerializedProperty groundProp = element.FindPropertyRelative("_ground");

            if (groundProp.objectReferenceValue is not TSGroundAuthoring ground)
                continue;

            if (ground.name == findGroundName)
                return ground;
        }

        return null;
    }

    private void CheckMinRange(SerializedProperty minProp)
    {
        Vector2Int min = minProp.vector2IntValue;

        min.x = Mathf.Clamp(min.x, 0, IntDefine.MAP_TOTAL_GRID_WIDTH);
        min.y = Mathf.Clamp(min.y, 0, IntDefine.MAP_TOTAL_GRID_HEIGHT);

        minProp.vector2IntValue = min;
    }

    private void CheckMaxRange(SerializedProperty minProp, SerializedProperty maxProp)
    {
        Vector2Int min = minProp.vector2IntValue;
        Vector2Int max = maxProp.vector2IntValue;

        max.x = Mathf.Clamp(max.x, min.x, IntDefine.MAP_TOTAL_GRID_WIDTH);
        max.y = Mathf.Clamp(max.y, min.y, IntDefine.MAP_TOTAL_GRID_HEIGHT);

        maxProp.vector2IntValue = max;
    }

    private void CheckLadderRange(SerializedProperty ladderProp, SerializedProperty positionProp)
    {
        if (ladderProp.boxedValue is not TSLadderAuthoring ladderAuthoring)
            return;

        var topGround = ladderAuthoring.GetTopConnectGround();
        var bottomGround = ladderAuthoring.GetBottomConnectGround();

        if (!topGround || !bottomGround)
        {
            positionProp.intValue = 0;
            return;
        }

        var topGroundGrid = ConvertToGrid(topGround);
        var bottomGroundGrid = ConvertToGrid(bottomGround);

        int min = Mathf.Max(topGroundGrid.min.x, bottomGroundGrid.min.x);
        int max = Mathf.Min(topGroundGrid.max.x, bottomGroundGrid.max.x);

        positionProp.intValue = Mathf.Clamp(positionProp.intValue, min, max);
    }

    private (Vector2 position, Vector2Int size) ConvertFromGrid(Vector2Int min, Vector2Int max)
    {
        float halfWidth = IntDefine.MAP_TOTAL_GRID_WIDTH * 0.5f;
        float halfHeight = IntDefine.MAP_TOTAL_GRID_HEIGHT * 0.5f;
        float halfGridSize = IntDefine.MAP_GRID_SIZE * 0.5f;
        float averageX = (min.x + max.x) * 0.5f;
        float averageY = (min.y + max.y) * 0.5f;
        float x = (averageX - halfWidth) * IntDefine.MAP_GRID_SIZE;
        float y = (averageY - halfHeight) * IntDefine.MAP_GRID_SIZE;
        int sizeX = max.x - min.x + IntDefine.MAP_GRID_SIZE;
        int sizeY = max.y - min.y + IntDefine.MAP_GRID_SIZE;

        x += halfGridSize;
        y += halfGridSize;

        return (new Vector2(x, y), new Vector2Int(sizeX, sizeY));
    }

    private (Vector2Int min, Vector2Int max) ConvertToGrid(TSGroundAuthoring ground)
    {
        if (ground == null)
            return (Vector2Int.zero, Vector2Int.zero);

        float halfWidth = IntDefine.MAP_TOTAL_GRID_WIDTH * 0.5f;
        float halfHeight = IntDefine.MAP_TOTAL_GRID_HEIGHT * 0.5f;
        float halfGridSize = IntDefine.MAP_GRID_SIZE * 0.5f;

        // Remove half grid offset
        float x = ground.Position.x - halfGridSize;
        float y = ground.Position.y - halfGridSize;

        // Convert to grid coordinates
        float gridX = (x / IntDefine.MAP_GRID_SIZE) + halfWidth;
        float gridY = (y / IntDefine.MAP_GRID_SIZE) + halfHeight;

        // Calculate size in grid units
        int gridSizeX = (int) ground.Size.x - IntDefine.MAP_GRID_SIZE;
        int gridSizeY = (int) ground.Size.y - IntDefine.MAP_GRID_SIZE;

        // Calculate min/max from center position and size
        float halfSizeX = gridSizeX * 0.5f;
        float halfSizeY = gridSizeY * 0.5f;

        int minX = Mathf.RoundToInt(gridX - halfSizeX);
        int minY = Mathf.RoundToInt(gridY - halfSizeY);
        int maxX = Mathf.RoundToInt(gridX + halfSizeX);
        int maxY = Mathf.RoundToInt(gridY + halfSizeY);

        return (new Vector2Int(minX, minY), new Vector2Int(maxX, maxY));
    }

    private bool InitializeGround(SerializedProperty groundProp,
    SerializedProperty minProp,
    SerializedProperty maxProp)
    {
        if (groundProp.boxedValue is not TSGroundAuthoring ground)
            return false;

        Vector2Int min = minProp.vector2IntValue;
        Vector2Int max = maxProp.vector2IntValue;
        var result = ConvertFromGrid(min, max);
        bool isModify = false;

        if (ground.Position.x != result.position.x
        || ground.Position.y != result.position.y)
        {
            ground.SetPosition(result.position.x, result.position.y);
            isModify = true;
        }

        if (ground.Size.x != result.size.x || ground.Size.y != result.size.y)
        {
            ground.SetSize(result.size.x, result.size.y);
            isModify = true;
        }

        return isModify;
    }

    private bool InitializeLadder(SerializedProperty ladderProp,
    SerializedProperty positionProp)
    {
        if (ladderProp.boxedValue is not TSLadderAuthoring ladder)
            return false;

        var topGround = ladder.GetTopConnectGround();
        var bottomGround = ladder.GetBottomConnectGround();

        if (!topGround || !bottomGround)
            return false;

        float halfWidth = IntDefine.MAP_TOTAL_GRID_WIDTH * 0.5f;
        float halfGridSize = IntDefine.MAP_GRID_SIZE * 0.5f;
        int positionX = positionProp.intValue;
        float x = (positionX - halfWidth) * IntDefine.MAP_GRID_SIZE;
        float y = (topGround.Position.y + bottomGround.Position.y) * 0.5f;

        x += halfGridSize;

        if (ladder.transform.localPosition.x != x
        || ladder.transform.localPosition.y != y)
        {
            ladder.transform.localPosition = new Vector2(x, y);
            return true;
        }

        return false;
    }

    private void RemoveGroundAtIndex(int index)
    {
        if (index < 0 || index >= _groundsProperty.arraySize)
            return;

        SerializedProperty element = _groundsProperty.GetArrayElementAtIndex(index);
        SerializedProperty groundProp = element.FindPropertyRelative("_ground");

        // GameObject 삭제
        if (groundProp.objectReferenceValue != null)
        {
            TSGroundAuthoring ground = groundProp.objectReferenceValue as TSGroundAuthoring;
            if (ground != null) DestroyImmediate(ground.gameObject);
        }

        // 배열에서 제거
        _groundsProperty.DeleteArrayElementAtIndex(index);

        serializedObject.ApplyModifiedProperties();
    }

    private void RemoveLadderAtIndex(int index)
    {
        if (index < 0 || index >= _laddersProperty.arraySize)
            return;

        SerializedProperty element = _laddersProperty.GetArrayElementAtIndex(index);
        SerializedProperty ladderProp = element.FindPropertyRelative("_ladder");

        // GameObject 삭제
        if (ladderProp.objectReferenceValue != null)
        {
            TSLadderAuthoring ladder = ladderProp.objectReferenceValue as TSLadderAuthoring;
            if (ladder != null) DestroyImmediate(ladder.gameObject);
        }

        // 배열에서 제거
        _laddersProperty.DeleteArrayElementAtIndex(index);

        serializedObject.ApplyModifiedProperties();
    }

    private void AddLastGround()
    {
        Transform parent = _groundParentProperty.boxedValue as Transform;
        int lastIndex = _groundsProperty.arraySize;

        // 지형 오브젝트 생성
        GameObject newGround = CreateGround(parent);
        newGround.name = $"Ground{lastIndex}";

        // 지형 배열 추가
        _groundsProperty.arraySize++;

        SerializedProperty prop = _groundsProperty.GetArrayElementAtIndex(lastIndex);
        SerializedProperty groundProp = prop.FindPropertyRelative("_ground");
        SerializedProperty minProp = prop.FindPropertyRelative("_min");
        SerializedProperty maxProp = prop.FindPropertyRelative("_max");

        // TSGroundAuthoring 컴포넌트를 GroundEntry._ground에 할당
        TSGroundAuthoring ground = newGround.GetComponent<TSGroundAuthoring>();
        groundProp.objectReferenceValue = ground;

        // 위치 기준 초기화
        minProp.vector2IntValue = Vector2Int.zero;
        maxProp.vector2IntValue = Vector2Int.zero;

        serializedObject.ApplyModifiedProperties();
    }

    private void AddLastLadder(TSGroundAuthoring topGround, TSGroundAuthoring bottomGround)
    {
        if (topGround == null || bottomGround == null)
            return;

        Transform parent = _ladderParentProperty.boxedValue as Transform;
        int lastIndex = _laddersProperty.arraySize;

        // 사다리 오브젝트 생성
        GameObject newLadder = CreateLadder(parent, topGround, bottomGround);
        newLadder.name = $"Ladder{lastIndex}";

        // 사다리 배열 추가
        _laddersProperty.arraySize++;

        SerializedProperty prop = _laddersProperty.GetArrayElementAtIndex(lastIndex);
        SerializedProperty ladderProp = prop.FindPropertyRelative("_ladder");
        SerializedProperty positionProp = prop.FindPropertyRelative("_position");

        // 사다리 참조 할당
        TSLadderAuthoring ladder = newLadder.GetComponent<TSLadderAuthoring>();
        ladderProp.objectReferenceValue = ladder;

        // 위치 기준 초기화
        var topGroundGrid = ConvertToGrid(topGround);
        var bottomGroundGrid = ConvertToGrid(bottomGround);

        positionProp.intValue = Mathf.Max(topGroundGrid.min.x, bottomGroundGrid.min.x);

        serializedObject.ApplyModifiedProperties();
    }

    private GameObject CreateGround(Transform parent)
    {
        GameObject newGround = new GameObject();

        newGround.AddComponent<TSGroundAuthoring>();
        newGround.AddComponent<PickedAuthoring>();

        if (parent != null)
        {
            newGround.transform.SetParent(parent);
            newGround.transform.localPosition = Vector3.zero;
        }

        return newGround;
    }

    private GameObject CreateLadder(Transform parent, TSGroundAuthoring topGround, TSGroundAuthoring bottomGround)
    {
        GameObject newLadder = new GameObject();

        var ladder = newLadder.AddComponent<TSLadderAuthoring>();

        ladder.SetFirstConnectGround(topGround);
        ladder.SetSecondConnectGround(bottomGround);

        if (parent != null)
        {
            newLadder.transform.SetParent(parent);
            newLadder.transform.localPosition = Vector3.zero;
        }

        return newLadder;
    }

    private void SetSaveDirty()
    {
        EditorUtility.SetDirty(target);

        // For prefabs
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().scene);
        }
        // For scene objects
        else
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                ((Component) target).gameObject.scene);
        }
    }
}
