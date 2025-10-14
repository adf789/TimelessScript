using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TS.LowLevel.Data.Config
{
    /// <summary>
    /// 타일맵 패턴 레지스트리
    /// 모든 타일맵 패턴을 관리하고 SubScene과의 매핑을 담당
    /// </summary>
    [CreateAssetMenu(fileName = "TilemapPatternRegistry", menuName = "TS/Tilemap/Pattern Registry")]
    public class TilemapPatternRegistry : ScriptableObject
    {
        [Header("Pattern Database")]
        [Tooltip("등록된 모든 타일맵 패턴")]
        public List<TilemapPatternData> AllPatterns = new List<TilemapPatternData>();

        [Header("SubScene Initial Patterns")]
        [Tooltip("각 SubScene이 시작할 때 로드할 패턴들")]
        public List<SubScenePatternMapping> InitialMappings = new List<SubScenePatternMapping>();

        [Header("Pattern Categories")]
        [Tooltip("타입별로 분류된 패턴")]
        public List<PatternCategory> Categories = new List<PatternCategory>();

        // 런타임 캐시
        private Dictionary<string, TilemapPatternData> _patternCache;
        private Dictionary<string, List<TilemapPatternData>> _subSceneCache;
        private Dictionary<TilemapPatternType, List<TilemapPatternData>> _typeCache;
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

            // SubScene 캐시 생성
            _subSceneCache = new Dictionary<string, List<TilemapPatternData>>();
            foreach (var mapping in InitialMappings)
            {
                if (string.IsNullOrEmpty(mapping.SubSceneName)) continue;
                _subSceneCache[mapping.SubSceneName] = new List<TilemapPatternData>(mapping.InitialPatterns);
            }

            // 타입별 캐시 생성
            _typeCache = new Dictionary<TilemapPatternType, List<TilemapPatternData>>();
            foreach (TilemapPatternType type in System.Enum.GetValues(typeof(TilemapPatternType)))
            {
                _typeCache[type] = AllPatterns.Where(p => p != null && p.Type == type).ToList();
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
        /// SubScene 이름으로 초기 패턴 목록 가져오기
        /// </summary>
        public List<TilemapPatternData> GetPatternsForSubScene(string subSceneName)
        {
            if (!_isInitialized) Initialize();

            if (string.IsNullOrEmpty(subSceneName))
            {
                Debug.LogWarning("[TilemapPatternRegistry] SubSceneName is null or empty");
                return new List<TilemapPatternData>();
            }

            if (_subSceneCache.TryGetValue(subSceneName, out var patterns))
            {
                return new List<TilemapPatternData>(patterns);
            }

            Debug.LogWarning($"[TilemapPatternRegistry] No patterns found for SubScene: {subSceneName}");
            return new List<TilemapPatternData>();
        }

        /// <summary>
        /// 현재 패턴의 특정 방향으로 연결 가능한 패턴 ID 목록 가져오기
        /// </summary>
        public List<string> GetValidNextPatterns(string currentPatternID, Direction direction)
        {
            var pattern = GetPattern(currentPatternID);
            if (pattern == null) return new List<string>();

            return pattern.GetValidNextPatterns(direction);
        }

        /// <summary>
        /// 패턴 타입으로 패턴 목록 가져오기
        /// </summary>
        public List<TilemapPatternData> GetPatternsByType(TilemapPatternType type)
        {
            if (!_isInitialized) Initialize();

            if (_typeCache.TryGetValue(type, out var patterns))
            {
                return new List<TilemapPatternData>(patterns);
            }

            return new List<TilemapPatternData>();
        }

        /// <summary>
        /// 랜덤 패턴 가져오기
        /// </summary>
        public TilemapPatternData GetRandomPattern(TilemapPatternType? type = null)
        {
            if (!_isInitialized) Initialize();

            List<TilemapPatternData> targetList;

            if (type.HasValue)
            {
                targetList = GetPatternsByType(type.Value);
            }
            else
            {
                targetList = AllPatterns.Where(p => p != null).ToList();
            }

            if (targetList.Count == 0)
            {
                Debug.LogWarning("[TilemapPatternRegistry] No patterns available for random selection");
                return null;
            }

            return targetList[Random.Range(0, targetList.Count)];
        }

        /// <summary>
        /// 특정 방향으로 연결 가능한 랜덤 패턴 가져오기
        /// </summary>
        public TilemapPatternData GetRandomNextPattern(string currentPatternID, Direction direction)
        {
            var validPatternIDs = GetValidNextPatterns(currentPatternID, direction);

            if (validPatternIDs.Count == 0)
            {
                Debug.LogWarning($"[TilemapPatternRegistry] No valid next patterns for {currentPatternID} in direction {direction}");
                return null;
            }

            var randomID = validPatternIDs[Random.Range(0, validPatternIDs.Count)];
            return GetPattern(randomID);
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

            // Connection 검증
            foreach (var pattern in AllPatterns)
            {
                if (pattern == null) continue;

                foreach (var connection in pattern.Connections)
                {
                    foreach (var nextPatternID in connection.ValidNextPatterns)
                    {
                        if (!AllPatterns.Any(p => p != null && p.PatternID == nextPatternID))
                        {
                            warnings.Add($"Pattern '{pattern.PatternID}' references non-existent pattern '{nextPatternID}' in {connection.Direction} connection");
                        }
                    }
                }
            }

            // SubScene 매핑 검증
            foreach (var mapping in InitialMappings)
            {
                if (string.IsNullOrEmpty(mapping.SubSceneName))
                {
                    warnings.Add("SubScene mapping has empty SubSceneName");
                    continue;
                }

                foreach (var pattern in mapping.InitialPatterns)
                {
                    if (pattern == null)
                    {
                        warnings.Add($"SubScene '{mapping.SubSceneName}' has null pattern reference");
                    }
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
        }
#endif
    }

    /// <summary>
    /// SubScene과 타일맵 패턴 매핑
    /// </summary>
    [System.Serializable]
    public class SubScenePatternMapping
    {
        [Tooltip("SubScene 이름")]
        public string SubSceneName;

        [Tooltip("SubScene 로드 시 함께 로드할 초기 패턴들")]
        public List<TilemapPatternData> InitialPatterns = new List<TilemapPatternData>();

        [Tooltip("초기 패턴 로딩 옵션")]
        public PatternLoadingOptions LoadingOptions;
    }

    /// <summary>
    /// 패턴 로딩 옵션
    /// </summary>
    [System.Serializable]
    public struct PatternLoadingOptions
    {
        [Tooltip("SubScene과 함께 로드할지 여부")]
        public bool LoadWithSubScene;

        [Tooltip("SubScene과 함께 언로드할지 여부")]
        public bool UnloadWithSubScene;

        [Tooltip("로딩 우선순위 (높을수록 먼저 로드)")]
        public int Priority;

        public static PatternLoadingOptions Default => new PatternLoadingOptions
        {
            LoadWithSubScene = true,
            UnloadWithSubScene = true,
            Priority = 100
        };
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
}
