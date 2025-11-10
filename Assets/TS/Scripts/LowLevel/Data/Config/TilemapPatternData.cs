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
