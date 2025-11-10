using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 타일맵 패턴 레지스트리
/// 모든 타일맵 패턴을 관리하고 초기 패턴 설정
/// </summary>
[CreateAssetMenu(fileName = "TilemapPatternRegistry", menuName = "TS/Tilemap/Pattern Registry")]
public class TilemapPatternRegistry : ScriptableObject
{
    [Header("Pattern Database")]
    [Tooltip("등록된 모든 타일맵 패턴")]
    public List<TilemapPatternData> AllPatterns = new List<TilemapPatternData>();

    [Header("Pattern Categories")]
    [Tooltip("Shape별로 분류된 패턴")]
    public List<PatternCategory> Categories = new List<PatternCategory>();

    public Vector2Int GridSize { get; private set; } = new Vector2Int(IntDefine.MAP_TOTAL_GRID_WIDTH, IntDefine.MAP_TOTAL_GRID_WIDTH);

    // 런타임 캐시
    private Dictionary<string, TilemapPatternData> _patternCache;
    private Dictionary<FourDirection, List<TilemapPatternData>> _shapeCache;
    private bool _isInitialized = false;
    /// <summary>
    /// 레지스트리 초기화 (런타임에 최초 1회 호출)
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        // 패턴 ID 캐시 생성
        _patternCache = new Dictionary<string, TilemapPatternData>();
        foreach (var pattern in AllPatterns)
        {
            if (pattern == null) continue;

            if (_patternCache.ContainsKey(pattern.PatternID))
            {
                Debug.LogWarning($"[TilemapPatternRegistry] Duplicate PatternID found: {pattern.PatternID}");
                continue;
            }

            _patternCache[pattern.PatternID] = pattern;
        }

        // Shape별 캐시 생성
        _shapeCache = new Dictionary<FourDirection, List<TilemapPatternData>>();
        foreach (FourDirection direction in System.Enum.GetValues(typeof(FourDirection)))
        {
            _shapeCache[direction] = AllPatterns.Where(p => p != null && p.Connections.Any(c => c.Direction == direction)).ToList();
        }

        _isInitialized = true;
        Debug.Log($"[TilemapPatternRegistry] Initialized with {_patternCache.Count} patterns");
    }

    /// <summary>
    /// 패턴 ID로 패턴 데이터 가져오기
    /// </summary>
    public TilemapPatternData GetPattern(string patternID)
    {
        if (!_isInitialized) Initialize();

        if (string.IsNullOrEmpty(patternID))
        {
            Debug.LogWarning("[TilemapPatternRegistry] PatternID is null or empty");
            return null;
        }

        if (_patternCache.TryGetValue(patternID, out var pattern))
        {
            return pattern;
        }

        Debug.LogWarning($"[TilemapPatternRegistry] Pattern not found: {patternID}");
        return null;
    }

    /// <summary>
    /// 패턴 Shape으로 패턴 목록 가져오기
    /// </summary>
    public List<TilemapPatternData> GetPatternsByShape(FourDirection direction)
    {
        if (!_isInitialized) Initialize();

        if (_shapeCache.TryGetValue(direction, out var patterns))
        {
            return new List<TilemapPatternData>(patterns);
        }

        return new List<TilemapPatternData>();
    }

    /// <summary>
    /// 랜덤 패턴 가져오기
    /// </summary>
    public TilemapPatternData GetRandomPattern(FourDirection direction)
    {
        if (!_isInitialized) Initialize();

        List<TilemapPatternData> targetList = GetPatternsByShape(direction);

        if (targetList.Count == 0)
        {
            Debug.LogWarning("[TilemapPatternRegistry] No patterns available for random selection");
            return null;
        }

        return targetList[Random.Range(0, targetList.Count)];
    }

    /// <summary>
    /// 패턴이 존재하는지 확인
    /// </summary>
    public bool HasPattern(string patternID)
    {
        if (!_isInitialized) Initialize();
        return _patternCache.ContainsKey(patternID);
    }

    /// <summary>
    /// 모든 패턴 ID 목록 가져오기
    /// </summary>
    public List<string> GetAllPatternIDs()
    {
        if (!_isInitialized) Initialize();
        return new List<string>(_patternCache.Keys);
    }
}

/// <summary>
/// 패턴 카테고리 (에디터 조직화용)
/// </summary>
[System.Serializable]
public class PatternCategory
{
    [Tooltip("카테고리 이름")]
    public string CategoryName;

    [Tooltip("카테고리에 속한 패턴들")]
    public List<TilemapPatternData> Patterns = new List<TilemapPatternData>();

    [Tooltip("카테고리 색상 (에디터 표시용)")]
    public Color CategoryColor = Color.white;
}
