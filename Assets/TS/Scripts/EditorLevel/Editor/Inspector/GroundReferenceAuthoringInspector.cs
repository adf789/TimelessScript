
using UnityEngine;
using Unity.Entities;
using UnityEditor;

[CustomEditor(typeof(GroundReferenceAuthoring))]
public class GroundReferenceAuthoringInspector : Editor
{
    private GroundReferenceAuthoring inspectorTarget;
    private SerializedProperty _groundsProperty;
    private Vector2 _scrollPosition;

    private void OnEnable()
    {
        inspectorTarget = (GroundReferenceAuthoring) target;
        _groundsProperty = serializedObject.FindProperty("_grounds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Ground Entries", EditorStyles.boldLabel);

        // Array size control
        EditorGUILayout.BeginHorizontal();
        {
            int newSize = EditorGUILayout.IntField("Size", _groundsProperty.arraySize);
            if (newSize != _groundsProperty.arraySize)
            {
                _groundsProperty.arraySize = newSize;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Scroll view
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(400));
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
                        if (GUILayout.Button("Initialize", GUILayout.Width(70)))
                        {
                            if (InitializeGround(groundProp, minProp, maxProp))
                                SetSaveDirty();
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            _groundsProperty.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(groundProp, new GUIContent("지형"));

                    EditorGUILayout.PropertyField(minProp, new GUIContent("왼쪽-아래 모서리"));
                    CheckMinRange(minProp);

                    EditorGUILayout.PropertyField(maxProp, new GUIContent("오른쪽-위 모서리"));
                    CheckMaxRange(minProp, maxProp);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }
        EditorGUILayout.EndScrollView();

        // Add button
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Add Ground Entry"))
        {
            _groundsProperty.arraySize++;
        }

        serializedObject.ApplyModifiedProperties();
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

    private bool InitializeGround(SerializedProperty groundProp,
    SerializedProperty minProp,
    SerializedProperty maxProp)
    {
        if (groundProp.boxedValue is not TSGroundAuthoring groundAuthoring)
            return false;

        Vector2Int min = minProp.vector2IntValue;
        Vector2Int max = maxProp.vector2IntValue;
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

        groundAuthoring.SetPosition(x, y);
        groundAuthoring.SetSize(sizeX, sizeY);

        return true;
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
