#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ReadOnly Attribute용 PropertyDrawer
/// 인스펙터에서 필드를 읽기 전용(비활성화)으로 표시
/// </summary>
[CustomPropertyDrawer(typeof(TS.ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // GUI를 비활성화 상태로 설정
        GUI.enabled = false;

        // 기본 PropertyField 그리기
        EditorGUI.PropertyField(position, property, label, true);

        // GUI 활성화 상태 복원
        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 기본 높이 반환 (중첩된 프로퍼티도 지원)
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif
