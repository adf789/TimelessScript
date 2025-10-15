using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

namespace TS.LowLevel.Data.Config
{
    /// <summary>
    /// 타일맵 패턴 데이터 정의
    /// 50x50 크기의 작은 타일맵 패턴을 정의하고 Addressables로 로드
    /// </summary>
    [CreateAssetMenu(fileName = "TilemapPattern", menuName = "TS/Tilemap/Pattern Data")]
    public class TilemapPatternData : ScriptableObject
    {
        [Header("Pattern Identification")]
        [Tooltip("고유 패턴 ID (예: Forest_01, Cave_02)")]
        public string PatternID;

        [Tooltip("패턴 표시 이름")]
        public string DisplayName;

        [Tooltip("패턴 설명")]
        [TextArea(3, 5)]
        public string Description;

        [Header("Grid Configuration")]
        [Tooltip("타일맵 그리드 크기 (기본: 50x50)")]
        public Vector2Int GridSize = new Vector2Int(50, 50);

        [Tooltip("타일 크기 (월드 유닛)")]
        public Vector2 TileSize = new Vector2(1f, 1f);

        [Header("Pattern Type")]
        [Tooltip("패턴 타입 (Forest, Cave, Bridge 등)")]
        public TilemapPatternType Type;

        [Header("Addressable Reference")]
        [Tooltip("타일맵 프리팹 Addressable 참조")]
        public AssetReference TilemapPrefab;

        [Header("Streaming Settings")]
        [Tooltip("로딩 우선순위 (높을수록 먼저 로드)")]
        [Range(0, 100)]
        public int LoadPriority = 50;

        [Tooltip("플레이어로부터 이 거리 이상이면 언로드 (월드 유닛)")]
        public float UnloadDistance = 100f;

        [Tooltip("미리 로드할 거리 (월드 유닛)")]
        public float PreloadDistance = 150f;

        [Header("Connection Points")]
        [Tooltip("다른 패턴과 연결 가능한 지점")]
        public List<ConnectionPoint> Connections = new List<ConnectionPoint>();

        [Header("Visual Preview")]
        [Tooltip("에디터 프리뷰용 썸네일")]
        public Texture2D PreviewThumbnail;

        /// <summary>
        /// 패턴 월드 크기 계산 (GridSize * TileSize)
        /// </summary>
        public Vector2 WorldSize => new Vector2(GridSize.x * TileSize.x, GridSize.y * TileSize.y);

        /// <summary>
        /// 특정 방향으로 연결 가능한 패턴 ID 목록 반환
        /// </summary>
        public List<string> GetValidNextPatterns(Direction direction)
        {
            var connectionIndex = Connections.FindIndex(c => c.Direction == direction);

            // 해당 방향의 연결이 없으면 빈 리스트 반환
            if (connectionIndex < 0)
                return new List<string>();

            var connection = Connections[connectionIndex];

            // ValidNextPatterns가 null이면 빈 리스트 반환
            return connection.ValidNextPatterns ?? new List<string>();
        }

        /// <summary>
        /// 특정 방향의 연결 지점이 있는지 확인
        /// </summary>
        public bool HasConnection(Direction direction)
        {
            return Connections.Exists(c => c.Direction == direction);
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

            // GridSize가 0 이하면 기본값으로 설정
            if (GridSize.x <= 0) GridSize.x = 50;
            if (GridSize.y <= 0) GridSize.y = 50;

            // Connections의 ValidNextPatterns 초기화 보장
            for (int i = 0; i < Connections.Count; i++)
            {
                var connection = Connections[i];
                if (connection.ValidNextPatterns == null)
                {
                    connection.ValidNextPatterns = new List<string>();
                    Connections[i] = connection; // struct이므로 다시 할당 필요
                }
            }
        }
#endif
    }

    /// <summary>
    /// 타일맵 패턴 타입
    /// </summary>
    public enum TilemapPatternType
    {
        Forest,      // 숲
        Cave,        // 동굴
        Bridge,      // 다리
        Village,     // 마을
        Dungeon,     // 던전
        Boss,        // 보스방
        Tutorial,    // 튜토리얼
        Custom       // 커스텀
    }

    /// <summary>
    /// 패턴 연결 지점 정의
    /// </summary>
    [System.Serializable]
    public struct ConnectionPoint
    {
        [Tooltip("연결 방향")]
        public Direction Direction;

        [Tooltip("연결 지점의 그리드 위치")]
        public Vector2Int GridPosition;

        [Tooltip("이 방향으로 연결 가능한 패턴 ID 목록")]
        public List<string> ValidNextPatterns;

        [Tooltip("연결 지점 활성화 여부")]
        public bool IsActive;
    }

    /// <summary>
    /// 4방향 정의
    /// </summary>
    public enum Direction
    {
        North,  // 북 (위)
        South,  // 남 (아래)
        East,   // 동 (오른쪽)
        West    // 서 (왼쪽)
    }
}
