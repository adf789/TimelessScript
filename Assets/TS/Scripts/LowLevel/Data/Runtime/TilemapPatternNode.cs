using UnityEngine;

/// <summary>
/// 타일맵 패턴 노드 - 6방향 멀티 링크드 리스트 구조
/// 각 노드는 6개의 방향으로 다른 패턴과 연결될 수 있음
/// </summary>
public class TilemapPatternNode
{
    /// <summary>
    /// 패턴 고유 ID
    /// </summary>
    public string PatternID { get; private set; }

    /// <summary>
    /// 월드 그리드 위치
    /// </summary>
    public Vector2Int WorldGridPosition { get; private set; }

    /// <summary>
    /// 현재 로드 상태
    /// </summary>
    public bool IsLoaded { get; set; }

    // 6방향 연결 노드
    public TilemapPatternNode[] LinkedNodes { get; set; }

    /// <summary>
    /// 생성자
    /// </summary>
    public TilemapPatternNode(string patternID, Vector2Int worldGridPosition)
    {
        PatternID = patternID;
        WorldGridPosition = worldGridPosition;
        IsLoaded = false;
        LinkedNodes = new TilemapPatternNode[IntDefine.DEFAULT_MAP_PATTERN_DIRECTION]; // 4방향
    }

    /// <summary>
    /// 특정 방향의 노드 가져오기
    /// </summary>
    public TilemapPatternNode GetNodeInDirection(PatternDirection direction)
    {
        return LinkedNodes[(int) direction];
    }

    /// <summary>
    /// 특정 방향에 노드 설정
    /// </summary>
    public void SetNodeInDirection(PatternDirection direction, TilemapPatternNode node)
    {
        LinkedNodes[(int) direction] = node;
    }

    /// <summary>
    /// 특정 방향에 연결이 있는지 확인
    /// </summary>
    public bool HasConnectionInDirection(PatternDirection direction)
    {
        return GetNodeInDirection(direction) != null;
    }
}
