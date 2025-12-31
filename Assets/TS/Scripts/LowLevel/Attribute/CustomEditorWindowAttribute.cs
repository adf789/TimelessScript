using System;

/// <summary>
/// EditorWindow 네비게이션 시스템에 등록하기 위한 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CustomEditorWindowAttribute : System.Attribute
{
    /// <summary>
    /// 윈도우 표시 이름
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 카테고리 (예: "Tools", "Resources", "Debug" 등)
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// 정렬 순서 (낮을수록 먼저 표시)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 윈도우 설명 (툴팁)
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// CustomEditorWindowAttribute 생성자
    /// </summary>
    /// <param name="displayName">윈도우 표시 이름</param>
    /// <param name="category">카테고리</param>
    public CustomEditorWindowAttribute(string displayName, string category = "Tools")
    {
        DisplayName = displayName;
        Category = category;
        Order = 0;
        Description = string.Empty;
    }
}
