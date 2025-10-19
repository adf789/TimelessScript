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

    [Header("Grid Configuration")]
    [Tooltip("타일맵 그리드 크기")]
    [SerializeField]
    private Vector2Int gridSize = new Vector2Int(50, 50);

    public Vector2Int GridSize => gridSize;

    // 런타임 캐시
    private Dictionary<string, TilemapPatternData> _patternCache;
    private Dictionary<PatternDirection, List<TilemapPatternData>> _shapeCache;
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
        _shapeCache = new Dictionary<PatternDirection, List<TilemapPatternData>>();
        foreach (PatternDirection direction in System.Enum.GetValues(typeof(PatternDirection)))
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
    public List<TilemapPatternData> GetPatternsByShape(PatternDirection direction)
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
    public TilemapPatternData GetRandomPattern(PatternDirection direction)
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

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: 패턴 검증
    /// </summary>
    public void ValidatePatterns()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 중복 ID 검사
        var idGroups = AllPatterns
            .Where(p => p != null)
            .GroupBy(p => p.PatternID)
            .Where(g => g.Count() > 1);

        foreach (var group in idGroups)
        {
            errors.Add($"Duplicate PatternID: {group.Key} ({group.Count()} occurrences)");
        }

        // 빈 Addressable 참조 검사
        foreach (var pattern in AllPatterns)
        {
            if (pattern == null) continue;

            if (pattern.TilemapPrefab == null || !pattern.TilemapPrefab.RuntimeKeyIsValid())
            {
                warnings.Add($"Pattern '{pattern.PatternID}' has invalid Addressable reference");
            }
        }

        // 결과 출력
        if (errors.Count > 0)
        {
            Debug.LogError($"[TilemapPatternRegistry] Validation found {errors.Count} errors:\n" + string.Join("\n", errors));
        }

        if (warnings.Count > 0)
        {
            Debug.LogWarning($"[TilemapPatternRegistry] Validation found {warnings.Count} warnings:\n" + string.Join("\n", warnings));
        }

        if (errors.Count == 0 && warnings.Count == 0)
        {
            Debug.Log("[TilemapPatternRegistry] Validation passed!");
        }
    }

    private void OnValidate()
    {
        // 에디터에서 값이 변경될 때마다 캐시 무효화
        _isInitialized = false;

        // GridSize가 0 이하면 기본값으로 설정
        if (gridSize.x <= 0) gridSize.x = 1;
        if (gridSize.y <= 0) gridSize.y = 1;
    }
#endif
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
