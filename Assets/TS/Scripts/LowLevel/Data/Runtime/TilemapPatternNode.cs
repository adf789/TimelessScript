using Unity.Mathematics;

/// <summary>
/// 타일맵 패턴 노드
/// </summary>
public class MapNode
{
    /// <summary>
    /// 패턴 고유 ID
    /// </summary>
    public string PatternID { get; private set; }

    /// <summary>
    /// 월드 그리드 위치
    /// </summary>
    public int2 WorldGridPosition { get; private set; }

    /// <summary>
    /// 현재 로드 상태
    /// </summary>
    public bool IsLoaded { get; set; }

    // 4방향 연결 노드
    private MapLink[] _linkedNodes;

    /// <summary>
    /// 생성자
    /// </summary>
    public MapNode(string patternID, int2 worldGridPosition)
    {
        PatternID = patternID;
        WorldGridPosition = worldGridPosition;
        IsLoaded = false;
        _linkedNodes = new MapLink[IntDefine.MAP_PATTERN_DIRECTION]; // 4방향
    }

    /// <summary>
    /// 특정 방향의 노드 가져오기
    /// </summary>
    public MapLink GetLink(FourDirection direction)
    {
        return _linkedNodes[(int) direction];
    }

    /// <summary>
    /// 특정 방향에 노드 설정
    /// </summary>
    public void SetNodeInDirection(MapNode node, FourDirection direction, int2 fromPosition)
    {
        _linkedNodes[(int) direction] = new MapLink()
        {
            Node = node,
            Direction = direction,
            FromPosition = fromPosition,
        };
    }

    /// <summary>
    /// 특정 방향에 연결이 있는지 확인
    /// </summary>
    public bool HasConnectionInDirection(FourDirection direction)
    {
        return !default(MapLink).Equals(GetLink(direction));
    }
}
