using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 타일맵 패턴 노드
/// </summary>
public class MapNodeEntry
{
    /// <summary>
    /// 패턴 고유 ID
    /// </summary>
    public string PatternID { get; private set; }
    public int2 GridOffset;
    public TilemapPatternData PatternData;
    public GameObject TilemapInstance;
    public Entity SubSceneEntity;
    public Entity MinGroundEntity;
    public Entity MaxGroundEntity;
    public bool IsLoaded => TilemapInstance != null;

    /// <summary>
    /// 생성자
    /// </summary>
    public MapNodeEntry(string patternID)
    {
        PatternID = patternID;
    }
}
