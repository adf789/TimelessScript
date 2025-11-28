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

    [Header("Addressable Reference")]
    [Tooltip("타일맵 프리팹 Addressable 참조")]
    public AssetReference TilemapPrefab;

    [Tooltip("씬 엔티티 Addressable 참조")]
    public EntitySceneReference SubScene;

    [HideInInspector]
    public int MinHeight { get; private set; }
    [HideInInspector]
    public int MaxHeight { get; private set; }

    [Tooltip("맵 연결부")]
    [HideInInspector]
    [SerializeField] private long[] _portValues;

    public bool CheckOverlap(TilemapPatternData data, FourDirection dir)
    {
        if (data == null)
            return false;

        if (!CheckPortCount() || !data.CheckPortCount())
            return false;

        var overlap = GetOverlap(data, dir);

        return overlap > 0;
    }

    public long GetOverlap(TilemapPatternData data, FourDirection dir)
    {
        if (!CheckPortCount())
            return 0;

        int dirIndex = (int) dir;

        if (data == null || !data.CheckPortCount())
            return _portValues[dirIndex];

        int oppsiteDirIndex = (int) GetOppositeDirection(dir);
        long compareValue = data._portValues[oppsiteDirIndex];

        return _portValues[dirIndex] | compareValue;
    }

    private FourDirection GetOppositeDirection(FourDirection dir)
    {
        return dir switch
        {
            FourDirection.Up => FourDirection.Down,
            FourDirection.Down => FourDirection.Up,
            FourDirection.Left => FourDirection.Right,
            FourDirection.Right => FourDirection.Left,
            _ => FourDirection.Up
        };
    }

    private bool CheckPortCount()
    {
        if (_portValues == null)
            return false;

        return _portValues.Length >= 4;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // PatternID가 비어있으면 파일명으로 설정
        if (string.IsNullOrEmpty(PatternID))
        {
            PatternID = name;
        }
    }

    public void SetPortValues(long[] values)
    {
        _portValues = values;
    }

    public void SetMinHeight(int value)
    {
        MinHeight = value;
    }

    public void SetMaxHeight(int value)
    {
        MaxHeight = value;
    }
#endif
}
