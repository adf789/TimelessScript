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

    public Vector2Int GridSize { get; private set; } = new Vector2Int(IntDefine.MAP_TOTAL_GRID_WIDTH, IntDefine.MAP_TOTAL_GRID_HEIGHT);

    // 런타임 캐시
    private Dictionary<string, TilemapPatternData> _patternCache;
    private bool _isInit = false;

    private void OnValidate()
    {
        _isInit = false;
    }

    /// <summary>
    /// 레지스트리 초기화 (런타임에 최초 1회 호출)
    /// </summary>
    public void Initialize()
    {
        if (_isInit) return;

        // 패턴 ID 캐시 생성
        _patternCache = new Dictionary<string, TilemapPatternData>();
        foreach (var pattern in AllPatterns)
        {
            if (pattern == null) continue;

            if (_patternCache.ContainsKey(pattern.PatternID))
            {
                this.DebugLogWarning($"Duplicate PatternID found: {pattern.PatternID}");
                continue;
            }

            _patternCache[pattern.PatternID] = pattern;
        }

        _isInit = true;
        this.DebugLog($"Initialized with {_patternCache.Count} patterns");
    }

    /// <summary>
    /// 패턴 ID로 패턴 데이터 가져오기
    /// </summary>
    public TilemapPatternData GetPattern(string patternID)
    {
        if (!_isInit)
            Initialize();

        if (string.IsNullOrEmpty(patternID))
        {
            this.DebugLogWarning("PatternID is null or empty");
            return null;
        }

        if (_patternCache.TryGetValue(patternID, out var pattern))
        {
            return pattern;
        }

        this.DebugLogWarning($"Pattern not found: {patternID}");
        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 패턴 ID로 패턴 데이터 생성, 있는 경우는 기존 데이터를 가져옴
    /// </summary>
    public TilemapPatternData AddPattern(string patternID, Unity.Entities.Serialization.EntitySceneReference sceneRef)
    {
        var data = GetPattern(patternID);

        if (data == null)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(this);
            string fileName = System.IO.Path.GetFileName(path);
            path = path.Substring(0, path.Length - fileName.Length);
            data = System.Activator.CreateInstance<TilemapPatternData>();
            data.PatternID = patternID;
            data.SubScene = sceneRef;

            AllPatterns.Add(data);
            _patternCache[data.PatternID] = data;

            UnityEditor.AssetDatabase.CreateAsset(data, path);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.EditorGUIUtility.PingObject(data);
        }

        return data;
    }
#endif

    /// <summary>
    /// 패턴이 존재하는지 확인
    /// </summary>
    public bool HasPattern(string patternID)
    {
        if (!_isInit)
            Initialize();

        return _patternCache.ContainsKey(patternID);
    }

    /// <summary>
    /// 모든 패턴 ID 목록 가져오기
    /// </summary>
    public List<string> GetAllPatternIDs()
    {
        if (!_isInit)
            Initialize();

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
