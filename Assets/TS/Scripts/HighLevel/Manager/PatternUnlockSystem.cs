using System.Collections.Generic;
using UnityEngine;

namespace TS.HighLevel.Manager
{
    /// <summary>
    /// 패턴 언락 시스템
    /// 게임 진행에 따라 패턴을 순차적으로 해금
    /// </summary>
    public class PatternUnlockSystem : BaseManager<PatternUnlockSystem>
    {
        [Header("Initial Setup")]
        [SerializeField] private string initialPatternID;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // 언락된 패턴 목록
        private HashSet<string> _unlockedPatterns = new HashSet<string>();

        // 언락 이벤트
        public event System.Action<string> OnPatternUnlocked;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 초기화 - 시작 패턴 자동 언락
        /// </summary>
        private void Initialize()
        {
            if (!string.IsNullOrEmpty(initialPatternID))
            {
                UnlockPattern(initialPatternID);

                if (showDebugLogs)
                    Debug.Log($"[PatternUnlockSystem] Initial pattern unlocked: {initialPatternID}");
            }
            else
            {
                Debug.LogWarning("[PatternUnlockSystem] No initial pattern set!");
            }
        }

        /// <summary>
        /// 패턴 언락
        /// </summary>
        public void UnlockPattern(string patternID)
        {
            if (string.IsNullOrEmpty(patternID))
            {
                Debug.LogError("[PatternUnlockSystem] Cannot unlock null or empty pattern ID");
                return;
            }

            if (_unlockedPatterns.Contains(patternID))
            {
                if (showDebugLogs)
                    Debug.Log($"[PatternUnlockSystem] Pattern already unlocked: {patternID}");
                return;
            }

            _unlockedPatterns.Add(patternID);
            OnPatternUnlocked?.Invoke(patternID);

            if (showDebugLogs)
                Debug.Log($"[PatternUnlockSystem] Pattern unlocked: {patternID} (Total: {_unlockedPatterns.Count})");
        }

        /// <summary>
        /// 여러 패턴 동시 언락
        /// </summary>
        public void UnlockPatterns(IEnumerable<string> patternIDs)
        {
            foreach (var patternID in patternIDs)
            {
                UnlockPattern(patternID);
            }
        }

        /// <summary>
        /// 패턴이 언락되었는지 확인
        /// </summary>
        public bool IsPatternUnlocked(string patternID)
        {
            return _unlockedPatterns.Contains(patternID);
        }

        /// <summary>
        /// 언락된 모든 패턴 목록
        /// </summary>
        public IReadOnlyCollection<string> UnlockedPatterns => _unlockedPatterns;

        /// <summary>
        /// 언락된 패턴 개수
        /// </summary>
        public int UnlockedCount => _unlockedPatterns.Count;

        /// <summary>
        /// 초기 패턴 ID
        /// </summary>
        public string InitialPatternID => initialPatternID;

        /// <summary>
        /// 모든 패턴 언락 (디버그용)
        /// </summary>
        public void UnlockAll(List<string> allPatternIDs)
        {
            foreach (var patternID in allPatternIDs)
            {
                _unlockedPatterns.Add(patternID);
            }

            if (showDebugLogs)
                Debug.Log($"[PatternUnlockSystem] All patterns unlocked: {_unlockedPatterns.Count}");
        }

        /// <summary>
        /// 모든 언락 초기화
        /// </summary>
        public void ResetUnlocks()
        {
            _unlockedPatterns.Clear();
            Initialize();

            if (showDebugLogs)
                Debug.Log("[PatternUnlockSystem] All unlocks reset");
        }

        #if UNITY_EDITOR
        [ContextMenu("Debug: Show Unlocked Patterns")]
        private void DebugShowUnlockedPatterns()
        {
            Debug.Log($"[PatternUnlockSystem] Unlocked Patterns ({_unlockedPatterns.Count}):");
            foreach (var patternID in _unlockedPatterns)
            {
                Debug.Log($"  - {patternID}");
            }
        }
        #endif
    }
}
