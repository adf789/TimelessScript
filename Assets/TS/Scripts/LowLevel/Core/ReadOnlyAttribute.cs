using UnityEngine;

namespace TS
{
    /// <summary>
    /// 인스펙터에서 필드를 읽기 전용으로 표시하는 Attribute
    /// EditorLevel의 ReadOnlyDrawer와 함께 사용됨
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}
