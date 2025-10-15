using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using Unity.Entities.Serialization;

namespace TS.LowLevel.Data.Config
{
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

        [Header("SubScene")]
        [Tooltip("이 패턴이 속한 SubScene 이름")]
        public string SubSceneName;

        [Header("Grid Configuration")]
        [Tooltip("타일맵 그리드 크기")]
        public Vector2Int GridSize = new Vector2Int(50, 50);

        [Tooltip("타일 크기 (월드 유닛)")]
        public Vector2 TileSize = new Vector2(1f, 1f);

        [Header("Addressable Reference")]
        [Tooltip("타일맵 프리팹 Addressable 참조")]
        public AssetReference TilemapPrefab;

        [Tooltip("씬 엔티티 Addressable 참조")]
        public EntitySceneReference SubScene;

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

        /// <summary>
        /// 패턴 월드 크기 계산 (GridSize * TileSize)
        /// </summary>
        public Vector2 WorldSize => new Vector2(GridSize.x * TileSize.x, GridSize.y * TileSize.y);

        /// <summary>
        /// 특정 방향의 활성화된 연결 지점이 있는지 확인
        /// </summary>
        public bool HasActiveConnection(PatternDirection direction)
        {
            return Connections.Exists(c => c.Direction == direction && c.IsActive);
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

            // GridSize가 0 이하면 기본값으로 설정
            if (GridSize.x <= 0) GridSize.x = 50;
            if (GridSize.y <= 0) GridSize.y = 50;

            // Connections 유효성 검증
            for (int i = 0; i < Connections.Count; i++)
            {
                var connection = Connections[i];

                // 사다리는 상/하 방향에만 허용
                if (connection.IsLadder)
                {
                    bool isVertical = connection.Direction == PatternDirection.TopLeft ||
                                      connection.Direction == PatternDirection.TopRight ||
                                      connection.Direction == PatternDirection.BottomLeft ||
                                      connection.Direction == PatternDirection.BottomRight;

                    if (!isVertical)
                    {
                        connection.IsLadder = false;
                        Connections[i] = connection;
                    }
                }
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
        public Vector2Int LocalPosition;

        [Tooltip("연결 지점 활성화 여부")]
        public bool IsActive;

        [Tooltip("사다리 연결 여부 (상/하 전용)")]
        public bool IsLadder;

        [Tooltip("연결된 패턴 ID (Port 연결)")]
        public string LinkedPatternID;
    }

    /// <summary>
    /// 6방향 정의 (2D Side-Scrolling)
    /// </summary>
    public enum PatternDirection
    {
        TopLeft,     // 좌상단
        TopRight,    // 우상단
        Left,        // 좌
        Right,       // 우
        BottomLeft,  // 좌하단
        BottomRight  // 우하단
    }
}
