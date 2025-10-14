# 타일맵 패턴 스트리밍 시스템 설계 문서

**작성일**: 2025-10-14
**버전**: 1.1
**상태**: Phase 2 구현 완료

---

## 📋 목차

1. [개요](#개요)
2. [설계 목표](#설계-목표)
3. [시스템 아키텍처](#시스템-아키텍처)
4. [구현 단계](#구현-단계)
5. [데이터 구조](#데이터-구조)
6. [스트리밍 전략](#스트리밍-전략)
7. [메모리 최적화](#메모리-최적화)
8. [사용 가이드](#사용-가이드)
9. [향후 계획](#향후-계획)

---

## 개요

### 배경

기존 ECS 기반 타일맵 시스템은 전체 타일맵을 하나의 엔티티로 관리했습니다. 이 방식은 다음과 같은 문제가 있었습니다:

- **메모리 비효율**: 전체 타일맵이 항상 메모리에 상주
- **확장성 부족**: 프로시저럴 맵 생성에 부적합
- **재사용성 결여**: 타일맵 패턴 재사용 불가능
- **SubScene 결합**: 타일맵과 SubScene이 강하게 결합

### 새로운 접근 방식

**Key-Value 매핑 기반 타일맵 패턴 스트리밍 시스템**

- 50x50 크기의 작은 타일맵 패턴을 독립적으로 관리
- SubScene과 타일맵 패턴을 분리하여 매핑
- Addressables를 통한 동적 로딩/언로딩
- 패턴 조합을 통한 동적 맵 확장

### 타겟 플랫폼

- **모바일**: iOS, Android
- **PC**: Windows, macOS

### 성능 목표

- **메모리**: 패턴당 ~80KB, 동시 로드 3-5개 (240-400KB)
- **로딩 시간**: < 100ms per pattern
- **언로딩**: 플레이어로부터 100 유닛 이상 거리 시 자동
- **메모리 절감**: 기존 대비 70-80%

---

## 설계 목표

### 핵심 목표

1. **패턴 재사용성**
   - 동일한 타일맵 패턴을 여러 위치에서 재사용
   - 타입별(Forest, Cave, Bridge 등) 패턴 관리

2. **동적 맵 확장**
   - 플레이어 이동에 따른 실시간 패턴 로드/언로드
   - 프로시저럴 맵 생성 지원

3. **메모리 최적화**
   - 필요한 패턴만 메모리에 유지
   - 거리 기반 자동 언로드

4. **독립적 관리**
   - SubScene과 타일맵 분리
   - 타일맵 독립 업데이트 가능

5. **에디터 친화적**
   - ScriptableObject 기반 설정
   - 비주얼 에디터 도구 제공

---

## 시스템 아키텍처

### 전체 구조

```
┌─────────────────────────────────────────────────────────────┐
│                     Game Entry Point                        │
│                      (GameManager)                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    FlowManager                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  IntroFlow   │→│ LoadingFlow  │→│   GameFlow   │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              TilemapStreamingManager                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Pattern Loading/Unloading                           │  │
│  │  - LoadPattern(patternID, gridOffset)                │  │
│  │  - UnloadPattern(patternID, gridOffset)              │  │
│  │  - UpdateStreamingByPosition(playerPos)              │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Loaded Patterns Cache                               │  │
│  │  Dictionary<string, LoadedPattern>                   │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│            TilemapPatternRegistry (ScriptableObject)        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Pattern Database                                    │  │
│  │  - AllPatterns: List<TilemapPatternData>            │  │
│  │  - InitialMappings: SubScene → Patterns            │  │
│  │  - Categories: Type-based grouping                  │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│          TilemapPatternData (ScriptableObject)              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Pattern Info                                        │  │
│  │  - PatternID: string                                 │  │
│  │  - GridSize: Vector2Int (50x50)                     │  │
│  │  - Type: TilemapPatternType                         │  │
│  │  - TilemapPrefab: AssetReference                    │  │
│  │  - Connections: List<ConnectionPoint>               │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  Addressables System                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Tilemap Prefab Loading                              │  │
│  │  - Addressables.InstantiateAsync()                   │  │
│  │  - Addressables.ReleaseInstance()                    │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 데이터 흐름

```
User Input
    │
    ▼
PlayerMovement
    │
    ▼
TilemapStreamingManager.UpdateStreamingByPosition()
    │
    ├─→ 거리 계산
    ├─→ 먼 패턴 언로드
    └─→ 필요한 패턴 로드
         │
         ├─→ TilemapPatternRegistry.GetPattern(patternID)
         │
         ├─→ Addressables.InstantiateAsync(prefab)
         │
         └─→ 월드 위치 설정 & 렌더링
```

---

## 구현 단계

### Phase 1: 기반 구조 (완료) ✅

**목표**: 데이터 구조 및 레지스트리 시스템 구축

**구현 내용**:
- ✅ `TilemapPatternData.cs`: 개별 패턴 정의
- ✅ `TilemapPatternRegistry.cs`: 패턴 관리 레지스트리
- ✅ ScriptableObject 기반 설정 시스템

**파일 위치**:
```
Assets/TS/Scripts/LowLevel/Data/Config/
├── TilemapPatternData.cs
└── TilemapPatternRegistry.cs
```

**주요 기능**:
- 패턴 ID 기반 캐싱
- SubScene 매핑 시스템
- 타입별 패턴 분류
- 연결 지점(Connection Point) 정의

### Phase 2: 스트리밍 매니저 (완료) ✅

**목표**: 런타임 패턴 로딩/언로딩 시스템

**구현 완료**:
- ✅ `TilemapStreamingManager.cs`: BaseManager 상속
- ✅ Addressables 통합
- ✅ 로딩/언로딩 로직
- ✅ 거리 기반 자동 관리
- ✅ 로딩 대기열 시스템
- ✅ 디버그 시각화 (Gizmos)

**파일 위치**:
```
Assets/TS/Scripts/HighLevel/Manager/
└── TilemapStreamingManager.cs (503 lines)
```

**구현된 주요 기능**:

#### 1. 초기화 시스템
```csharp
public void Initialize()
- 레지스트리 검증 및 초기화
- 자동 업데이트 시스템 시작
```

#### 2. 패턴 로딩
```csharp
public async UniTask LoadInitialPatterns(string subSceneName)
- SubScene 초기 패턴 일괄 로드
- 병렬 로딩 지원

public async UniTask<GameObject> LoadPattern(string patternID, Vector2Int gridOffset)
- Addressables 기반 동적 로드
- 중복 로드 방지
- 최대 패턴 수 제한 (기본 9개)
- 월드 위치 자동 계산 및 배치
```

#### 3. 패턴 언로딩
```csharp
public async UniTask UnloadPattern(string patternID, Vector2Int gridOffset)
- 개별 패턴 언로드

public async UniTask UnloadAllPatterns()
- 전체 패턴 일괄 언로드

public async UniTask UnloadDistantPatterns(Vector3 playerPosition, int count)
- 거리 기반 선택적 언로드
```

#### 4. 자동 스트리밍
```csharp
public async UniTask UpdateStreamingByPosition(Vector3 playerPosition)
- 플레이어 위치 기반 자동 관리
- 거리 계산 (UnloadDistance 기준)
- 주기적 업데이트 (기본 0.5초)

public void SetPlayerPosition(Vector3 position)
- 외부에서 플레이어 위치 업데이트
```

#### 5. 로딩 대기열
```csharp
public void EnqueueLoadRequest(string patternID, Vector2Int gridOffset, int priority)
- 우선순위 기반 로딩 대기열
- 동시 로딩 수 제한 (기본 3개)

private async UniTask ProcessLoadQueue()
- 대기열 자동 처리
```

#### 6. 유틸리티 & 디버그
```csharp
public int LoadedPatternCount
- 현재 로드된 패턴 수

public List<string> GetLoadedPatternKeys()
- 로드된 패턴 키 목록

public bool IsPatternLoaded(string patternID, Vector2Int gridOffset)
- 패턴 로드 여부 확인

private void OnDrawGizmos()
- Scene View에서 로드된 패턴 시각화
- 플레이어 위치 표시
```

**성능 최적화**:
- 비동기 로딩 (UniTask)
- 로딩 중 중복 요청 방지
- 최대 동시 로딩 수 제한
- 거리 기반 자동 언로드
- 캐시 기반 빠른 조회

**에러 처리**:
- Addressable 참조 검증
- 로딩 실패 시 예외 처리
- 로그 레벨별 디버깅 지원

### Phase 3: FlowManager 통합 (예정) ⏳

**목표**: 기존 게임 플로우에 타일맵 시스템 통합

**구현 계획**:
- `LoadingFlow.cs` 수정
- SubScene 로딩과 타일맵 로딩 동기화
- 초기 패턴 로드 구현

**주요 변경사항**:
```csharp
// LoadingFlow.cs
protected override async UniTask OnEnter()
{
    // 1. SubScene 로드
    await LoadSubScene(targetSceneName);

    // 2. 타일맵 패턴 로드 (NEW!)
    await TilemapStreamingManager.Instance.LoadInitialPatterns(targetSceneName);

    // 3. 기타 초기화
    await InitializeGameSystems();

    // 4. 완료
    FlowManager.Instance.ChangeFlow<GameFlow>();
}
```

### Phase 4: 에디터 도구 (예정) ⏳

**목표**: 개발 편의성 향상

**구현 계획**:
- 패턴 검증 도구
- 패턴 프리뷰 도구
- 매핑 관리 윈도우

**파일 위치**:
```
Assets/TS/Scripts/EditorLevel/Editor/Tilemap/
├── TilemapPatternValidator.cs
├── TilemapPatternPreview.cs
└── TilemapMappingWindow.cs
```

**주요 기능**:
- 패턴 ID 중복 검사
- Addressable 참조 검증
- Connection 유효성 확인
- Scene View 프리뷰

### Phase 5: 프로시저럴 확장 (선택) ⭐

**목표**: 동적 맵 생성 시스템

**구현 계획**:
- `ProceduralMapGenerator.cs`: 프로시저럴 맵 생성기
- 방향 기반 패턴 선택
- 자동 로드/언로드

**파일 위치**:
```
Assets/TS/Scripts/HighLevel/Manager/
└── ProceduralMapGenerator.cs
```

**주요 기능**:
- `ExpandToDirection(currentGrid, direction)`: 방향으로 확장
- 연결 규칙 기반 패턴 선택
- 무한 맵 생성 지원

---

## 데이터 구조

### TilemapPatternData

**목적**: 개별 타일맵 패턴의 모든 정보를 담는 ScriptableObject

**주요 필드**:

```csharp
public class TilemapPatternData : ScriptableObject
{
    // 식별 정보
    public string PatternID;              // 고유 ID
    public string DisplayName;            // 표시 이름
    public string Description;            // 설명

    // 그리드 설정
    public Vector2Int GridSize;           // 50x50 (기본)
    public Vector2 TileSize;              // 1x1 (기본)

    // 타입
    public TilemapPatternType Type;       // Forest, Cave, etc.

    // Addressable 참조
    public AssetReference TilemapPrefab;  // 실제 타일맵 프리팹

    // 스트리밍 설정
    public int LoadPriority;              // 0-100
    public float UnloadDistance;          // 100f (기본)
    public float PreloadDistance;         // 150f (기본)

    // 연결 지점
    public List<ConnectionPoint> Connections;

    // 프리뷰
    public Texture2D PreviewThumbnail;
}
```

**패턴 타입**:
```csharp
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
```

**연결 지점**:
```csharp
public struct ConnectionPoint
{
    public Direction Direction;                  // North, South, East, West
    public Vector2Int GridPosition;              // 연결 위치
    public List<string> ValidNextPatterns;       // 연결 가능한 패턴 ID
    public bool IsActive;                        // 활성화 여부
}

public enum Direction
{
    North,  // 위
    South,  // 아래
    East,   // 오른쪽
    West    // 왼쪽
}
```

### TilemapPatternRegistry

**목적**: 모든 패턴을 관리하고 SubScene과 매핑

**주요 필드**:

```csharp
public class TilemapPatternRegistry : ScriptableObject
{
    // 패턴 데이터베이스
    public List<TilemapPatternData> AllPatterns;

    // SubScene 매핑
    public List<SubScenePatternMapping> InitialMappings;

    // 카테고리
    public List<PatternCategory> Categories;

    // 런타임 캐시
    private Dictionary<string, TilemapPatternData> _patternCache;
    private Dictionary<string, List<TilemapPatternData>> _subSceneCache;
    private Dictionary<TilemapPatternType, List<TilemapPatternData>> _typeCache;
}
```

**SubScene 매핑**:
```csharp
public class SubScenePatternMapping
{
    public string SubSceneName;                      // "Level1_SubScene"
    public List<TilemapPatternData> InitialPatterns; // 초기 로드 패턴
    public PatternLoadingOptions LoadingOptions;     // 로딩 옵션
}

public struct PatternLoadingOptions
{
    public bool LoadWithSubScene;      // SubScene과 함께 로드
    public bool UnloadWithSubScene;    // SubScene과 함께 언로드
    public int Priority;               // 우선순위
}
```

**주요 메서드**:

```csharp
// 초기화
public void Initialize()

// 패턴 가져오기
public TilemapPatternData GetPattern(string patternID)
public List<TilemapPatternData> GetPatternsForSubScene(string subSceneName)
public List<TilemapPatternData> GetPatternsByType(TilemapPatternType type)

// 연결 패턴 찾기
public List<string> GetValidNextPatterns(string currentPatternID, Direction direction)

// 랜덤 선택
public TilemapPatternData GetRandomPattern(TilemapPatternType? type = null)
public TilemapPatternData GetRandomNextPattern(string currentPatternID, Direction direction)

// 검증 (에디터 전용)
public void ValidatePatterns()
```

---

## 스트리밍 전략

### 로딩 전략

**1. 초기 로딩 (SubScene 진입 시)**

```
SubScene "Level1" 로드
    ↓
TilemapPatternRegistry.GetPatternsForSubScene("Level1")
    ↓
ForestPattern_Start, BridgePattern_01 반환
    ↓
각 패턴을 Addressables로 로드
    ↓
월드 위치 설정 (gridOffset 적용)
    ↓
로드 완료
```

**2. 동적 로딩 (플레이어 이동 시)**

```
플레이어 위치 업데이트 (매 0.5초)
    ↓
UpdateStreamingByPosition(playerPos) 호출
    ↓
모든 로드된 패턴의 거리 계산
    ↓
┌─────────────────────┬─────────────────────┐
│  거리 > 100 유닛   │  거리 < 150 유닛   │
│  (언로드)          │  (로드)            │
└─────────────────────┴─────────────────────┘
    ↓                      ↓
UnloadPattern()        LoadPattern()
```

**3. 우선순위 로딩**

```
로딩 대기열
    │
    ├─→ Priority 100 (현재 패턴) - 즉시 로드
    ├─→ Priority 80  (인접 패턴) - 0.1초 지연
    └─→ Priority 50  (먼 패턴)   - 0.5초 지연
```

### 언로딩 전략

**1. 거리 기반 자동 언로딩**

```csharp
// TilemapStreamingManager.cs
private bool IsDistant(LoadedPattern pattern, Vector3 playerPos)
{
    var patternCenter = CalculatePatternCenter(pattern);
    var distance = Vector3.Distance(playerPos, patternCenter);

    return distance > pattern.Data.UnloadDistance;
}
```

**2. 메모리 압박 시 강제 언로딩**

```
메모리 사용량 > 80%
    ↓
가장 먼 패턴부터 언로드
    ↓
메모리 사용량 < 70%까지 반복
```

**3. SubScene 언로드 시**

```
SubScene 언로드 이벤트
    ↓
해당 SubScene의 모든 패턴 확인
    ↓
LoadingOptions.UnloadWithSubScene == true인 패턴 언로드
```

---

## 메모리 최적화

### 메모리 사용량 계산

**단일 패턴 (50x50)**:
```
타일 데이터: 50 × 50 × 32 bytes = 80,000 bytes ≈ 78KB
메타데이터: ~2KB
총 합계: ~80KB
```

**동시 로드 시나리오**:

| 패턴 수 | 메모리 사용량 | 적용 시나리오 |
|---------|---------------|---------------|
| 1개     | ~80KB         | 튜토리얼      |
| 3개     | ~240KB        | 일반 플레이   |
| 5개     | ~400KB        | 복잡한 맵     |
| 9개     | ~720KB        | 최대 (3×3)    |

### 최적화 기법

**1. 패턴 재사용**

```
동일 패턴 여러 위치 사용
→ 메모리: N × 80KB
→ Addressable 캐싱으로 중복 로드 방지
→ 실제 메모리: 80KB + (N-1) × 인스턴스 오버헤드
```

**2. 컬링 시스템 통합**

```
화면 밖 패턴 = 렌더링 스킵
→ CPU/GPU 부하 감소
→ 메모리는 유지 (빠른 재표시)
```

**3. LOD (Level of Detail)**

```
거리별 타일 디테일 조정
- 가까운 패턴: Full detail
- 중간 거리: Medium detail (타일 2×2 병합)
- 먼 거리: Low detail (타일 4×4 병합)
```

### 모바일 최적화

**메모리 제한**:
- iOS/Android: 동시 로드 최대 5개 패턴 권장
- 저사양 기기: 3개 패턴으로 제한

**배터리 최적화**:
- 업데이트 주기: 0.5초 (기본) → 1초 (절전 모드)
- 백그라운드: 모든 패턴 언로드

---

## 사용 가이드

### 1. 패턴 생성

**Step 1**: ScriptableObject 생성
```
프로젝트 창 우클릭
→ Create → TS → Tilemap → Pattern Data
→ 파일명: "Forest_01"
```

**Step 2**: 패턴 설정
```
Inspector에서:
- PatternID: "Forest_01"
- DisplayName: "숲 패턴 01"
- GridSize: (50, 50)
- Type: Forest
- TilemapPrefab: Addressable 타일맵 프리팹 지정
```

**Step 3**: 연결 지점 설정
```
Connections:
- North: Forest_02, Bridge_01
- South: Cave_01
- East: Village_01
- West: (비활성화)
```

### 2. 레지스트리 설정

**Step 1**: 레지스트리 생성
```
프로젝트 창 우클릭
→ Create → TS → Tilemap → Pattern Registry
→ 파일명: "MainTilemapRegistry"
```

**Step 2**: 패턴 등록
```
Inspector에서:
- All Patterns: 생성한 모든 패턴 드래그 앤 드롭
```

**Step 3**: SubScene 매핑
```
Initial Mappings:
- SubSceneName: "Level1_SubScene"
  Initial Patterns:
    - Forest_01 (시작 패턴)
    - Bridge_01 (연결 패턴)
```

### 3. 게임에 적용

**Step 1**: 레지스트리 할당
```csharp
// TilemapStreamingManager.cs Inspector
public TilemapPatternRegistry patternRegistry;
```

**Step 2**: 초기화 코드
```csharp
// LoadingFlow.cs
await TilemapStreamingManager.Instance.LoadInitialPatterns("Level1_SubScene");
```

**Step 3**: 플레이어 이동 연동
```csharp
// Update or FixedUpdate
if (Time.time - lastUpdateTime > updateInterval)
{
    await streamingManager.UpdateStreamingByPosition(player.transform.position);
    lastUpdateTime = Time.time;
}
```

### 4. 프로시저럴 확장 (선택)

```csharp
// 플레이어가 패턴 경계 근처 도달 시
if (IsNearBoundary(playerPos, currentPattern, Direction.North))
{
    await proceduralGenerator.ExpandToDirection(currentGrid, Direction.North);
}
```

---

## 향후 계획

### Phase 2 (완료) ✅

- [x] `TilemapStreamingManager.cs` 구현
- [x] Addressables 통합
- [x] 로딩/언로딩 로직 구현
- [x] 자동 스트리밍 시스템

### Phase 3 (단기)

- [ ] LoadingFlow 통합
- [ ] 초기 패턴 로딩 검증
- [ ] 성능 테스트 및 최적화

### Phase 4 (중기)

- [ ] 에디터 검증 도구
- [ ] 패턴 프리뷰 시스템
- [ ] 자동화된 테스트 케이스

### Phase 5 (장기)

- [ ] 프로시저럴 맵 생성기
- [ ] AI 기반 패턴 배치
- [ ] 동적 난이도 조절

### 추가 기능 (검토 중)

- [ ] 패턴 전환 애니메이션
- [ ] 멀티플레이어 동기화
- [ ] 패턴 번들 압축
- [ ] 클라우드 기반 패턴 공유

---

## 참고 자료

### 관련 파일

```
LowLevel (데이터 구조):
├── Data/Config/TilemapPatternData.cs (완료)
└── Data/Config/TilemapPatternRegistry.cs (완료)

HighLevel (매니저):
└── Manager/TilemapStreamingManager.cs (완료 - 503 lines)

EditorLevel (에디터 도구 - 예정):
└── Editor/Tilemap/TilemapPatternValidator.cs
```

### 외부 라이브러리

- **Unity Addressables**: 1.21.x
- **UniTask**: 2.x
- **Unity Entities**: 1.3.14 (선택적 통합)

### 성능 벤치마크 (예상)

| 항목 | 기존 시스템 | 새 시스템 | 개선율 |
|------|-------------|-----------|--------|
| 메모리 | ~1.2MB (전체) | ~240KB (3패턴) | 80% |
| 로딩 시간 | ~500ms | ~100ms/패턴 | 40% |
| 확장성 | 제한적 | 무제한 | - |

---

**문서 버전**: 1.1
**최종 수정**: 2025-10-14
**작성자**: Claude Code
**상태**: Phase 2 완료, Phase 3 준비 중

---

## 변경 이력

### v1.1 (2025-10-14)
- ✅ Phase 2 완료: TilemapStreamingManager 구현
- ✅ Addressables 통합 완료
- ✅ 자동 스트리밍 시스템 구현
- ✅ 로딩 대기열 시스템 추가
- ✅ 디버그 시각화 기능 추가

### v1.0 (2025-10-14)
- ✅ Phase 1 완료: 기반 데이터 구조
- ✅ TilemapPatternData 생성
- ✅ TilemapPatternRegistry 생성
- ✅ 초기 설계 문서 작성
