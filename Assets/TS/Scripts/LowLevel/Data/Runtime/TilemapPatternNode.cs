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
    /// 패턴 데이터 참조
    /// </summary>
    public TilemapPatternData PatternData { get; private set; }

    /// <summary>
    /// 현재 로드 상태
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// 로드된 GameObject 인스턴스
    /// </summary>
    public GameObject LoadedInstance { get; set; }

    // 6방향 연결 노드
    public TilemapPatternNode TopLeft { get; set; }
    public TilemapPatternNode TopRight { get; set; }
    public TilemapPatternNode Left { get; set; }
    public TilemapPatternNode Right { get; set; }
    public TilemapPatternNode BottomLeft { get; set; }
    public TilemapPatternNode BottomRight { get; set; }

    /// <summary>
    /// 생성자
    /// </summary>
    public TilemapPatternNode(string patternID, Vector2Int worldGridPosition, TilemapPatternData patternData)
    {
        PatternID = patternID;
        WorldGridPosition = worldGridPosition;
        PatternData = patternData;
        IsLoaded = false;
        LoadedInstance = null;
    }

    /// <summary>
    /// 특정 방향의 노드 가져오기
    /// </summary>
    public TilemapPatternNode GetNodeInDirection(PatternDirection direction)
    {
        return direction switch
        {
            PatternDirection.TopLeft => TopLeft,
            PatternDirection.TopRight => TopRight,
            PatternDirection.Left => Left,
            PatternDirection.Right => Right,
            PatternDirection.BottomLeft => BottomLeft,
            PatternDirection.BottomRight => BottomRight,
            _ => null
        };
    }

    /// <summary>
    /// 특정 방향에 노드 설정
    /// </summary>
    public void SetNodeInDirection(PatternDirection direction, TilemapPatternNode node)
    {
        switch (direction)
        {
            case PatternDirection.TopLeft:
                TopLeft = node;
                break;
            case PatternDirection.TopRight:
                TopRight = node;
                break;
            case PatternDirection.Left:
                Left = node;
                break;
            case PatternDirection.Right:
                Right = node;
                break;
            case PatternDirection.BottomLeft:
                BottomLeft = node;
                break;
            case PatternDirection.BottomRight:
                BottomRight = node;
                break;
        }
    }

    /// <summary>
    /// 특정 방향에 연결이 있는지 확인
    /// </summary>
    public bool HasConnectionInDirection(PatternDirection direction)
    {
        return GetNodeInDirection(direction) != null;
    }

    /// <summary>
    /// 모든 연결된 노드 가져오기
    /// </summary>
    public System.Collections.Generic.List<TilemapPatternNode> GetAllConnectedNodes()
    {
        var nodes = new System.Collections.Generic.List<TilemapPatternNode>();

        if (TopLeft != null) nodes.Add(TopLeft);
        if (TopRight != null) nodes.Add(TopRight);
        if (Left != null) nodes.Add(Left);
        if (Right != null) nodes.Add(Right);
        if (BottomLeft != null) nodes.Add(BottomLeft);
        if (BottomRight != null) nodes.Add(BottomRight);

        return nodes;
    }

    /// <summary>
    /// 월드 위치 계산 (패턴의 중심점)
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        if (PatternData == null) return Vector3.zero;

        return new Vector3(
            WorldGridPosition.x * PatternData.GridSize.x + PatternData.GridSize.x * 0.5f,
            WorldGridPosition.y * PatternData.GridSize.y + PatternData.GridSize.y * 0.5f,
            0
        );
    }

    /// <summary>
    /// 패턴의 월드 바운드 계산
    /// </summary>
    public Bounds GetWorldBounds()
    {
        if (PatternData == null) return new Bounds();

        Vector3 center = GetWorldPosition();
        Vector3 size = new Vector3(PatternData.GridSize.x, PatternData.GridSize.y, 0);

        return new Bounds(center, size);
    }
}
