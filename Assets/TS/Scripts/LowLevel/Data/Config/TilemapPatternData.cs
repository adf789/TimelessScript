using UnityEngine;
using UnityEngine.AddressableAssets;
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

    public int MinHeight => _mapLinkInfo.GetVerticalY(FourDirection.Down);
    public int MaxHeight => _mapLinkInfo.GetVerticalY(FourDirection.Up);

    [Tooltip("맵 연결부")]
    [HideInInspector]
    [SerializeField] private MapLinkInfo _mapLinkInfo;

    public bool CheckOverlap(TilemapPatternData data, FourDirection dir)
    {
        if (data == null)
            return false;

        var oppositeDirection = GetOppositeDirection(dir);

        switch (dir)
        {
            case FourDirection.Left:
            case FourDirection.Right:
                {
                    long baseValue = _mapLinkInfo.GetHorizontal(dir);
                    long compareValue = data._mapLinkInfo.GetHorizontal(oppositeDirection);

                    return (baseValue | compareValue) > 0;
                }

            case FourDirection.Up:
            case FourDirection.Down:
                {
                    var baseValue = _mapLinkInfo.GetVertical(dir);
                    var compareValue = data._mapLinkInfo.GetVertical(oppositeDirection);

                    return baseValue.min <= compareValue.max && compareValue.min <= baseValue.max;
                }

            default:
                return false;
        }
    }

    public (int min, int max) GetHighGroundSize()
    {
        return _mapLinkInfo.GetVertical(FourDirection.Up);
    }

    public (int min, int max) GetLowGroundSize()
    {
        return _mapLinkInfo.GetVertical(FourDirection.Down);
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        // PatternID가 비어있으면 파일명으로 설정
        if (string.IsNullOrEmpty(PatternID))
        {
            PatternID = name;
        }
    }

    public void SetLinkInfo(MapLinkInfo info)
    {
        _mapLinkInfo = info;
    }
#endif
}
