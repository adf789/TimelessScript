using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using Unity.Entities.Serialization;

/// <summary>
/// 타일맵 패턴 데이터 정의
/// 2D Side-Scrolling 게임용 패턴 시스템
/// </summary>
[CreateAssetMenu(fileName = "TilemapPattern", menuName = "TS/Tilemap/Pattern Data")]
public class TilemapPatternData : ScriptableObject
{
    [Header("Pattern Identification")]
    [Tooltip("고유 패턴 ID")]
    public string PatternID;

    [Tooltip("패턴 표시 이름")]
    public string DisplayName;

    [Tooltip("패턴 설명")]
    [TextArea(3, 5)]
    public string Description;

    [Header("Addressable Reference")]
    [Tooltip("타일맵 프리팹 Addressable 참조")]
    public AssetReference TilemapPrefab;

    [Tooltip("씬 엔티티 Addressable 참조")]
    public EntitySceneReference SubScene;

    [Header("Connection Points")]
    [Tooltip("다른 패턴과 연결 가능한 지점")]
    public List<ConnectionPoint> Connections = new List<ConnectionPoint>();

    /// <summary>
    /// 특정 방향의 활성화된 연결 지점이 있는지 확인
    /// </summary>
    public bool HasActiveConnection(PatternDirection direction)
    {
        return Connections.Exists(c => c.Direction == direction);
    }

    /// <summary>
    /// 특정 방향의 연결 지점 가져오기
    /// </summary>
    public ConnectionPoint? GetConnection(PatternDirection direction)
    {
        int index = Connections.FindIndex(c => c.Direction == direction);
        if (index < 0) return null;
        return Connections[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // PatternID가 비어있으면 파일명으로 설정
        if (string.IsNullOrEmpty(PatternID))
        {
            PatternID = name;
        }

        // DisplayName이 비어있으면 PatternID로 설정
        if (string.IsNullOrEmpty(DisplayName))
        {
            DisplayName = PatternID;
        }
    }
#endif
}

/// <summary>
/// 패턴 연결 지점 정의 (6방향)
/// </summary>
[System.Serializable]
public struct ConnectionPoint
{
    [Tooltip("연결 방향 (6방향)")]
    public PatternDirection Direction;

    [Tooltip("패턴 내 정수형 연결 좌표")]
    public int Position;
}

/// <summary>
/// 4방향 정의 (2D Side-Scrolling)
/// </summary>
public enum PatternDirection
{
    Top,         // 상단
    Bottom,      // 하단
    Left,        // 좌
    Right,       // 우
}
