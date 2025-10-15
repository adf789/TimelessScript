# 타일맵 패턴 시스템 설계 문서

## 📋 시스템 개요

**프로젝트**: TimelessScript
**게임 타입**: 2D Side-Scrolling Simulation
**Unity 버전**: 6000.2.7f2
**작성일**: 2025-01-15

### 핵심 특징

- **6방향 연결 시스템**: TopLeft, TopRight, Left, Right, BottomLeft, BottomRight
- **3가지 패턴 형태**: Upper (사다리 위), Middle (평지), Lower (사다리 아래)
- **멀티 링크드 리스트 구조**: 각 패턴 노드가 6방향으로 연결
- **패턴 언락 시스템**: 게임 진행에 따라 패턴 순차 해금
- **SubScene 기반 로딩**: 각 패턴마다 고유 SubScene
- **수동 패턴 생성**: 개발자가 모든 패턴과 연결을 직접 설정
- **카메라 기반 스트리밍**: Orthographic 카메라 뷰포트 기준 동적 로드/언로드

---

## 🏗️ 시스템 아키텍처

### 계층 구조

```
LowLevel (Data Layer)
├── TilemapPatternData.cs          - ScriptableObject 패턴 정의
├── TilemapPatternNode.cs          - 멀티 링크드 리스트 노드
└── TilemapPatternRegistry.cs      - 패턴 중앙 관리

HighLevel (Manager Layer)
├── TilemapStreamingManager.cs     - Addressables 로딩/언로딩
├── TilemapGraphManager.cs         - 패턴 그래프 관리
└── PatternUnlockSystem.cs         - 패턴 언락 관리

EditorLevel (Editor Tools)
└── TilemapMappingWindow.cs        - 패턴 설정 에디터 툴
```

---

## 📐 데이터 구조

### 1. PatternDirection (6방향)

```csharp
public enum PatternDirection
{
    TopLeft,     // 좌상단 - 사다리로 위 + 왼쪽
    TopRight,    // 우상단 - 사다리로 위 + 오른쪽
    Left,        // 좌 - 수평 이동
    Right,       // 우 - 수평 이동
    BottomLeft,  // 좌하단 - 사다리로 아래 + 왼쪽
    BottomRight  // 우하단 - 사다리로 아래 + 오른쪽
}
```

### 2. PatternShape (3가지 형태)

```csharp
public enum PatternShape
{
    Upper,   // 상: 사다리로 위쪽 패턴과 연결 (TopLeft, TopRight)
    Middle,  // 중: 평지, 좌우 직선 이동 (Left, Right)
    Lower    // 하: 사다리로 아래쪽 패턴과 연결 (BottomLeft, BottomRight)
}
```

**사용 예시**:
- **Upper**: 언덕 위, 건물 옥상, 높은 플랫폼
- **Middle**: 평지, 복도, 일반 지형
- **Lower**: 계곡, 지하, 낮은 플랫폼

### 3. ConnectionPoint 구조

```csharp
[System.Serializable]
public struct ConnectionPoint
{
    public PatternDirection Direction;  // 연결 방향
    public Vector2Int LocalPosition;    // 패턴 내 정수 좌표
    public bool IsActive;               // 활성화 여부
    public bool IsLadder;               // 사다리 연결 (상/하 전용)
}
```

**설정 예시**:
```yaml
# Middle 패턴 (평지)
Connection 1:
  Direction: Left
  LocalPosition: (0, 25)
  IsActive: true
  IsLadder: false

Connection 2:
  Direction: Right
  LocalPosition: (49, 25)
  IsActive: true
  IsLadder: false

# Upper 패턴 (높은 지대)
Connection 3:
  Direction: BottomLeft
  LocalPosition: (10, 0)
  IsActive: true
  IsLadder: true
```

### 4. TilemapPatternNode (멀티 링크드 리스트)

```csharp
public class TilemapPatternNode
{
    public string PatternID;
    public Vector2Int WorldGridPosition;
    public TilemapPatternData PatternData;

    // 6방향 포인터
    public TilemapPatternNode TopLeft;
    public TilemapPatternNode TopRight;
    public TilemapPatternNode Left;
    public TilemapPatternNode Right;
    public TilemapPatternNode BottomLeft;
    public TilemapPatternNode BottomRight;

    public bool IsLoaded;
    public GameObject LoadedInstance;
}
```

---

## 🎮 주요 시스템

### 1. TilemapGraphManager

**역할**: 패턴 노드 간 연결 관리 및 가시 영역 탐색

**핵심 기능**:
- `SetRootPattern(patternID, gridPosition)` - 초기 패턴 설정
- `ConnectPatterns(fromID, toID, direction)` - 양방향 연결 생성
- `FindVisibleNodes(cameraBounds)` - 카메라 뷰 내 노드 탐색

**연결 로직**:
```csharp
// A 패턴에서 Right 방향으로 B 패턴 연결
ConnectPatterns("PatternA", Vector2Int.zero, "PatternB", PatternDirection.Right);
// 결과: A.Right → B, B.Left → A (양방향)
```

**그리드 위치 계산**:
```csharp
Direction.Right       → currentGrid + (1, 0)
Direction.Left        → currentGrid + (-1, 0)
Direction.TopRight    → currentGrid + (1, 1)
Direction.TopLeft     → currentGrid + (-1, 1)
Direction.BottomRight → currentGrid + (1, -1)
Direction.BottomLeft  → currentGrid + (-1, -1)
```

### 2. PatternUnlockSystem

**역할**: 게임 진행에 따른 패턴 해금 관리

**핵심 기능**:
- `UnlockPattern(patternID)` - 단일 패턴 언락
- `IsPatternUnlocked(patternID)` - 언락 상태 확인
- `OnPatternUnlocked` - 언락 이벤트

**사용 예시**:
```csharp
// 게임 시작 시 초기 패턴 자동 언락
PatternUnlockSystem.Instance.InitialPatternID = "StartingVillage";

// 보스 클리어 시 새 지역 언락
void OnBossDefeated()
{
    PatternUnlockSystem.Instance.UnlockPattern("CastleEntrance");
}
```

### 3. TilemapStreamingManager

**역할**: Addressables 기반 패턴 동적 로드/언로드

**핵심 기능**:
- `LoadInitialPattern(patternID)` - 시작 패턴 로드
- `LoadPatternNode(node)` - 노드 기반 로드
- `UpdateStreamingByCameraView()` - 카메라 기반 자동 스트리밍

**설정 파라미터**:
```csharp
[SerializeField] int maxLoadedPatterns = 9;     // 3x3 최대
[SerializeField] float updateInterval = 0.5f;    // 스트리밍 체크 주기
[SerializeField] float loadBufferSize = 20f;     // 카메라 외곽 버퍼
```

---

## 🔄 워크플로우

### 게임 시작 시퀀스

```
1. PatternUnlockSystem.Start()
   └─> InitialPatternID 자동 언락

2. TilemapGraphManager.SetRootPattern(initialID, Vector2Int.zero)
   └─> 루트 노드 생성

3. TilemapStreamingManager.LoadInitialPattern(initialID)
   └─> Addressables로 프리팹 로드

4. TilemapStreamingManager.UpdateStreamingByCameraView()
   └─> 카메라 위치 기반 주변 패턴 로드 (언락된 것만)
```

### 패턴 확장 플로우

```
1. GraphManager.ConnectPatterns(currentID, newID, direction)
   ├─> TryGetNode(currentGrid) 또는 CreateNode()
   ├─> CalculateTargetGrid(direction)
   └─> SetNodeInDirection() 양방향 연결

2. StreamingManager.LoadPatternNode(newNode)
   ├─> UnlockSystem.IsPatternUnlocked(newID) 확인
   ├─> Addressables.InstantiateAsync(prefab)
   └─> node.IsLoaded = true
```

### 카메라 이동 시

```
1. Update() - 주기적 체크 (updateInterval)
   └─> UpdateStreamingByCameraView()
       ├─> GetCameraBounds() - 카메라 + 버퍼 영역
       ├─> GraphManager.FindVisibleNodes(bounds)
       │   └─> 멀티 링크드 리스트 순회
       ├─> LoadPatternNode() - 보이는데 안 로드된 것
       └─> UnloadPattern() - 영역 벗어난 것
```

---

## 🎯 개발자 가이드

### 1. 새 패턴 생성

1. **ScriptableObject 생성**
   - Unity: `Create → TS → Tilemap → Pattern Data`
   - PatternID: 고유 식별자 (예: "ForestArea_01")
   - SubSceneName: 이 패턴의 SubScene 이름
   - Shape: Upper/Middle/Lower 선택

2. **타일맵 프리팹 작업**
   - Tilemap으로 50x50 그리드 디자인
   - Addressables에 등록
   - TilemapPrefab 필드에 참조 연결

3. **연결 지점 설정**
   - Inspector에서 ConnectionPoints 배열 편집
   - Direction, LocalPosition, IsActive, IsLadder 설정
   - 사다리는 TopLeft/TopRight/BottomLeft/BottomRight만 가능

4. **Registry 등록**
   - TilemapPatternRegistry.AllPatterns에 추가
   - 에디터 툴: `TS → Tilemap → Pattern Editor`

### 2. 패턴 연결 설정

**에디터에서**:
```csharp
// TS/Tilemap/Pattern Editor 툴 사용
1. 좌측에서 패턴 선택
2. SubScene Name 입력
3. Inspector에서 ConnectionPoints 수동 편집
```

**코드에서**:
```csharp
// 런타임에 동적 연결 (테스트용)
TilemapGraphManager.Instance.ConnectPatterns(
    "Village_Center",
    Vector2Int.zero,
    "Village_East",
    PatternDirection.Right
);
```

### 3. 패턴 언락 트리거

```csharp
// 예시: 퀘스트 완료 시
public class QuestSystem : MonoBehaviour
{
    public void OnQuestCompleted(string questID)
    {
        if (questID == "FindOldKey")
        {
            PatternUnlockSystem.Instance.UnlockPattern("SecretCave");
            Debug.Log("새 지역 해금: 비밀 동굴");
        }
    }
}
```

---

## ⚙️ 성능 고려사항

### 메모리

- **패턴당 메모리**: ~3-6MB (50x50, 스프라이트 + 콜라이더)
- **최대 동시 로드**: 9-16 패턴 권장 (~35-100MB)
- **SubScene 분리**: 각 패턴이 독립적으로 로드/언로드

### CPU

**로딩 시간**:
- Addressables 로드: ~50-150ms
- GameObject 인스턴스화: ~20-50ms
- Tilemap 초기화: ~30-80ms
- 총 패턴당: ~140-380ms

**스트리밍 최적화**:
```csharp
updateInterval = 0.5f;        // 낮을수록 반응↑, CPU↑
loadBufferSize = 20f;         // 클수록 안정적, 메모리↑
maxConcurrentLoads = 3;       // 동시 로딩 제한
```

### 카메라 이동 속도 권장

**패턴 크기**: 50 타일 × 1 유닛 = 50 월드 유닛

**권장 속도**:
- 일반 이동: 20-40 유닛/초
- 대시/스킬: 60-100 유닛/초
- 극한 속도: 150 유닛/초 (버퍼 증가 필요)

**계산식**:
```
안전 거리 = 카메라 속도 × 로딩 시간 + 버퍼
100 유닛/초 × 0.25초 + 20 유닛 = 45 유닛 필요
→ loadBufferSize = 30f 이상 권장
```

---

## 🔧 에디터 툴 사용법

### Pattern Editor (TS → Tilemap → Pattern Editor)

**기능**:
1. **Initial Pattern 설정** - 게임 시작 패턴 지정
2. **패턴 검색** - PatternID, Shape로 필터링
3. **SubScene 설정** - 각 패턴의 SubScene 이름 편집
4. **Connection 확인** - 연결 지점 시각화 (읽기 전용)

**워크플로우**:
1. Find 버튼으로 Registry 자동 탐색
2. Initial Pattern 드롭다운에서 시작 패턴 선택
3. 좌측 리스트에서 패턴 선택
4. 우측 패널에서 SubScene Name 입력
5. Open in Inspector로 ConnectionPoints 상세 편집
6. Save 버튼으로 저장

---

## 📊 시스템 제약사항

### 설계 제약

❌ **프로시저럴 생성 없음**
- 모든 패턴과 연결을 개발자가 수동 설정
- 랜덤 맵 생성 불가 (의도된 디자인)

❌ **저장/로드 시스템 없음**
- 패턴 언락 상태 저장 안 됨
- 게임 재시작 시 초기화

✅ **6방향 고정**
- 8방향 확장 불가 (코드 구조상 가능하지만 디자인 제약)

✅ **SubScene per Pattern**
- 각 패턴이 독립 SubScene
- 대규모 월드 구조 시 관리 복잡도 증가

### 확장 가능성

**향후 추가 가능 기능**:
- Save/Load 시스템 구현
- 패턴 언락 진행도 저장
- 동적 연결 에디터 툴
- 비주얼 노드 그래프 에디터
- 패턴 프리뷰 생성기

---

## 🐛 트러블슈팅

### 문제 1: 패턴이 로드되지 않음

**원인**:
- 패턴이 언락되지 않음
- Addressable 참조 무효

**해결**:
```csharp
// 1. 언락 확인
Debug.Log(PatternUnlockSystem.Instance.IsPatternUnlocked("PatternID"));

// 2. Addressable 검증
TilemapPatternRegistry.Instance.ValidatePatterns();
```

### 문제 2: 연결이 작동하지 않음

**원인**:
- ConnectionPoint.IsActive = false
- 방향 불일치 (사다리는 대각선만)

**해결**:
1. Inspector에서 ConnectionPoint 확인
2. IsLadder가 true면 TopLeft/TopRight/BottomLeft/BottomRight만 사용
3. Left/Right는 IsLadder = false

### 문제 3: 카메라 이동 시 끊김

**원인**:
- 로딩 속도 < 카메라 속도
- loadBufferSize 부족

**해결**:
```csharp
// TilemapStreamingManager 설정 조정
loadBufferSize = 40f;        // 버퍼 증가
maxConcurrentLoads = 4;      // 동시 로딩 증가
updateInterval = 0.3f;       // 체크 주기 단축
```

---

## 📝 체크리스트

### 패턴 생성 체크리스트

- [ ] TilemapPatternData ScriptableObject 생성
- [ ] PatternID 고유 식별자 설정
- [ ] SubSceneName 지정
- [ ] PatternShape 선택 (Upper/Middle/Lower)
- [ ] 타일맵 프리팹 50x50 디자인
- [ ] Addressables 등록
- [ ] ConnectionPoints 설정 (방향, 좌표, 활성화)
- [ ] TilemapPatternRegistry.AllPatterns 등록
- [ ] ValidatePatterns() 실행

### 게임 시작 체크리스트

- [ ] TilemapPatternRegistry에 InitialPatternID 설정
- [ ] PatternUnlockSystem에 초기 패턴 설정
- [ ] TilemapGraphManager SetRootPattern 호출
- [ ] TilemapStreamingManager 카메라 참조 확인
- [ ] 최소 1개 패턴 Addressables 빌드

---

*Last Updated: 2025-01-15*
*System Version: 1.0*
*Unity: 6000.2.7f2*
