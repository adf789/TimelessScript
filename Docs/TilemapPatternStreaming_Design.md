# 타일맵 패턴 스트리밍 시스템 설계 문서

**작성일**: 2025-10-14
**버전**: 1.4
**상태**: Phase 5 구현 완료 (프로시저럴 맵 생성)

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

### Phase 3: FlowManager 통합 (완료) ✅

**목표**: 기존 게임 플로우에 타일맵 시스템 통합

**구현 완료**:
- ✅ `LoadingFlow.cs` 수정
- ✅ `TownFlow.cs` (HomeFlow) 수정
- ✅ SubScene 로딩과 타일맵 로딩 동기화
- ✅ 초기 패턴 로드 구현
- ✅ Flow 전환 시 패턴 자동 관리

**파일 위치**:
```
Assets/TS/Scripts/HighLevel/Flow/
├── LoadingFlow.cs (업데이트 - 98 lines)
└── HomeFlow.cs (TownFlow, 업데이트 - 88 lines)
```

**구현된 주요 기능**:

#### 1. LoadingFlow 통합
```csharp
[Header("Tilemap Settings")]
[SerializeField] private bool loadTilemapPatterns = true;
[SerializeField] private string tilemapSubSceneName = "";

public override async UniTask Enter()
{
    // 1. Scene 로드
    await OpenScene();

    // 2. Tilemap 패턴 로드 (옵션)
    if (loadTilemapPatterns)
    {
        await LoadTilemapPatterns();
    }

    // 3. UI 오픈
    OpenUI();
}

private async UniTask LoadTilemapPatterns()
{
    // TilemapStreamingManager 초기화 확인
    if (TilemapStreamingManager.Instance == null)
    {
        Debug.LogWarning("[LoadingFlow] TilemapStreamingManager is not initialized.");
        return;
    }

    // SubScene 이름 결정 (설정값 우선, 없으면 State 이름 사용)
    string subSceneName = string.IsNullOrEmpty(tilemapSubSceneName)
        ? State.ToString()
        : tilemapSubSceneName;

    // 초기 패턴 로드
    await TilemapStreamingManager.Instance.LoadInitialPatterns(subSceneName);
}
```

#### 2. TownFlow 통합
```csharp
// TownFlow도 LoadingFlow와 동일한 구조로 통합됨
// GameState.Town에 맞는 타일맵 패턴 로드
```

#### 3. Flow 전환 프로세스
```
FlowManager.ChangeFlow(newState)
    ↓
1. LoadingFlow.Enter()
   - Scene 로드
   - 타일맵 패턴 로드 (Loading용)
   - UI 오픈
    ↓
2. PreviousFlow.Exit()
   - 타일맵 패턴 언로드 (이전 Flow)
   - UI 닫기
   - Scene 언로드
    ↓
3. NewFlow.Enter()
   - Scene 로드
   - 타일맵 패턴 로드 (새 Flow용)
   - UI 오픈
    ↓
4. LoadingFlow.Exit()
   - 타일맵 패턴 언로드 (Loading용)
   - UI 닫기
   - Scene 언로드
```

#### 4. 설정 옵션
```csharp
// Inspector에서 설정 가능
- loadTilemapPatterns: true/false (타일맵 로딩 활성화)
- tilemapSubSceneName: "" (비어있으면 State 이름 사용)
```

**에러 처리**:
- TilemapStreamingManager 미초기화 시 Warning 로그 출력
- 패턴 로드 실패 시 예외 catch 및 Error 로그
- 크래시 방지 및 안전한 fallback

**로그 출력 예시**:
```
[LoadingFlow] Loading tilemap patterns for SubScene: Loading
[TilemapStreamingManager] Loading 1 initial patterns for Loading
[TilemapStreamingManager] Pattern loaded: TestPattern_01_0_0 at (0, 0, 0)
[LoadingFlow] Tilemap patterns loaded successfully for Loading

[TownFlow] Unloading tilemap patterns
[TilemapStreamingManager] Pattern unloaded: TestPattern_01_0_0

[TownFlow] Loading tilemap patterns for SubScene: Town
[TilemapStreamingManager] Loading 2 initial patterns for Town
[TilemapStreamingManager] Pattern loaded: TestPattern_02_0_0 at (0, 0, 0)
[TilemapStreamingManager] Pattern loaded: TestPattern_03_1_0 at (50, 0, 0)
[TownFlow] Tilemap patterns loaded successfully for Town
```

### Phase 4: 에디터 도구 (완료) ✅

**목표**: 개발 편의성 향상

**구현 완료**:
- ✅ `TilemapPatternValidator.cs`: 패턴 데이터 검증 도구
- ✅ `TilemapPatternPreview.cs`: Scene View 패턴 프리뷰
- ✅ `TilemapMappingWindow.cs`: SubScene-Pattern 매핑 관리

**파일 위치**:
```
Assets/TS/Scripts/EditorLevel/Editor/Tilemap/
├── TilemapPatternValidator.cs (457 lines)
├── TilemapPatternPreview.cs (406 lines)
└── TilemapMappingWindow.cs (438 lines)
```

**구현된 주요 기능**:

#### 1. TilemapPatternValidator (검증 도구)

**목적**: 패턴 데이터의 무결성을 자동으로 검증

**주요 기능**:
```csharp
[MenuItem("TS/Tilemap/Pattern Validator")]
public static void ShowWindow()

private void ValidateAll()
{
    ValidateDuplicateIDs();           // 중복 PatternID 검사
    ValidateAddressableReferences();  // Addressable 참조 검증
    ValidateConnections();             // Connection 유효성 확인
    ValidateSubSceneMappings();        // SubScene 매핑 검증
    ValidatePatternCategories();       // 카테고리 검증
}
```

**검증 항목**:
- **중복 ID**: 같은 PatternID를 사용하는 패턴 감지
- **Addressable 참조**: TilemapPrefab이 유효한 Addressable인지 확인
- **연결 패턴**: ValidNextPatterns에 존재하지 않는 패턴 ID 감지
- **SubScene 매핑**: null 참조, 빈 이름, 중복 매핑 검사
- **카테고리**: 빈 카테고리 이름, null 패턴 참조 확인

**출력 결과**:
- ❌ Error: 심각한 문제, 즉시 수정 필요
- ⚠️ Warning: 잠재적 문제, 검토 권장
- ℹ️ Info: 참고 정보

**사용 방법**:
```
Unity Editor 상단 메뉴
→ TS → Tilemap → Pattern Validator
→ 'Validate All' 버튼 클릭
→ 검증 결과 확인 및 수정
```

#### 2. TilemapPatternPreview (프리뷰 도구)

**목적**: Scene View에서 패턴 배치 미리보기 및 연결 지점 시각화

**주요 기능**:
```csharp
[MenuItem("TS/Tilemap/Pattern Preview")]
public static void ShowWindow()

private void OnSceneGUI(SceneView sceneView)
{
    DrawPatternInScene(preview, isSelected);  // 패턴 경계 및 그리드
    DrawConnectionsInScene(preview);           // 연결 지점 시각화
}
```

**시각화 요소**:
- **패턴 경계**: 흰색 와이어프레임 (선택 시 노란색)
- **그리드**: 10타일 간격 그리드 라인 (회색, 투명도 30%)
- **연결 지점**: 녹색 원형 마커
- **방향 화살표**: 연결 방향 표시 (North, South, East, West)
- **레이블**: PatternID 및 GridSize 표시

**인터랙티브 기능**:
- 패턴 추가/제거
- 패턴 선택 및 GridOffset 조정
- 프리뷰 뷰포트 이동 및 스케일 조정
- 그리드/연결/레이블 표시 토글

**사용 방법**:
```
Unity Editor 상단 메뉴
→ TS → Tilemap → Pattern Preview
→ Registry 선택
→ Available Patterns에서 패턴 선택
→ Scene View에서 배치 확인
→ GridOffset 조정으로 위치 변경
```

#### 3. TilemapMappingWindow (매핑 관리 도구)

**목적**: SubScene과 패턴 간의 매핑을 시각적으로 관리

**주요 기능**:
```csharp
[MenuItem("TS/Tilemap/Mapping Manager")]
public static void ShowWindow()

// SubScene 관리
private void AddNewSubSceneMapping()
private void RemoveSubSceneMapping(int index)

// 패턴 관리
private void AddPatternToMapping(int mappingIndex, TilemapPatternData pattern)
private void RemovePatternFromMapping(int mappingIndex, int patternIndex)
private void MovePattern(int mappingIndex, int fromIndex, int toIndex)

// 레지스트리 저장
private void SaveRegistry()
```

**UI 구성**:

**좌측 패널 (55% 너비)**:
- 새 SubScene 추가 입력란
- 기존 SubScene 매핑 목록
- 선택된 SubScene의 패턴 목록
- 패턴 순서 조정 버튼 (↑/↓)
- 패턴 제거 버튼 (−)

**우측 패널 (40% 너비)**:
- 패턴 검색 필터
- 사용 가능한 모든 패턴 목록
- 패턴 정보 (Type, GridSize)
- 패턴 추가 버튼 (Add →)

**주요 작업**:
1. **SubScene 생성**: 새 SubSceneName 입력 → Add 버튼
2. **패턴 추가**: 좌측에서 SubScene 선택 → 우측에서 패턴 선택 → Add → 버튼
3. **패턴 순서 변경**: 패턴 목록에서 ↑/↓ 버튼 사용
4. **패턴 제거**: 패턴 목록에서 − 버튼 클릭
5. **저장**: 상단 Save 버튼으로 레지스트리에 저장

**데이터 검증**:
- 중복 SubSceneName 방지
- 중복 패턴 추가 방지
- null 패턴 경고
- 저장 전 EditorUtility.SetDirty() 호출

**사용 방법**:
```
Unity Editor 상단 메뉴
→ TS → Tilemap → Mapping Manager
→ Registry 선택 (또는 'Find' 버튼)
→ 새 SubScene 추가 또는 기존 선택
→ 우측에서 패턴 추가
→ 순서 조정 및 제거
→ 'Save' 버튼으로 저장
```

**개발 워크플로우**:
```
1. TilemapPatternValidator로 패턴 검증
   ↓
2. TilemapPatternPreview로 Scene View 배치 확인
   ↓
3. TilemapMappingWindow로 SubScene 매핑 설정
   ↓
4. 게임 실행 및 테스트
```

**에디터 도구 통합**:
- 모든 도구는 동일한 TilemapPatternRegistry 공유
- 자동 레지스트리 검색 (Find 버튼)
- 즉시 저장 및 반영 (EditorUtility.SetDirty)
- Unity Inspector와 완전 호환

### Phase 5: 프로시저럴 확장 (완료) ✅

**목표**: 동적 맵 생성 시스템

**구현 완료**:
- ✅ `ProceduralMapGenerator.cs`: 프로시저럴 맵 생성 매니저
- ✅ `ProceduralMapPlayer.cs`: 플레이어 위치 추적 컴포넌트
- ✅ 방향 기반 패턴 선택 로직
- ✅ 연결 규칙 기반 자동 확장
- ✅ 플레이어 거리 기반 자동 확장

**파일 위치**:
```
Assets/TS/Scripts/HighLevel/Manager/
└── ProceduralMapGenerator.cs (446 lines)

Assets/TS/Scripts/HighLevel/Controller/
└── ProceduralMapPlayer.cs (72 lines)
```

**구현된 주요 기능**:

#### 1. ProceduralMapGenerator (프로시저럴 맵 생성기)

**목적**: 플레이어 위치 기반으로 패턴을 동적으로 확장하여 무한 맵 생성

**주요 기능**:
```csharp
[SerializeField] private TilemapPatternRegistry patternRegistry;
[SerializeField] private TilemapStreamingManager streamingManager;
[SerializeField] private bool enableAutoExpansion = true;
[SerializeField] private float expansionDistance = 75f;
[SerializeField] private int maxGeneratedPatterns = 50;
[SerializeField] private float checkInterval = 1f;

// 초기화
public override void Initialize()

// 플레이어 등록
public void SetPlayerTransform(Transform player)

// 시드 패턴 등록
public void RegisterSeedPattern(string patternID, Vector2Int gridOffset)
public void RegisterLoadedPatternsAsSeed()

// 맵 확장
public async UniTask<bool> ExpandToDirection(Vector2Int currentGrid, Direction direction)
private async void CheckAndExpandAroundPlayer(Vector3 playerPosition)

// 패턴 선택
private string GetValidNextPattern(string currentPatternID, Direction direction)

// 유틸리티
public int GeneratedPatternCount
public bool IsGridGenerated(Vector2Int gridOffset)
public List<Vector2Int> GetAllGeneratedGrids()
public void ClearGeneratedPatterns()
```

**핵심 로직**:

**1. 시드 패턴 등록**:
```csharp
// SubScene 초기 로드 시 시드 패턴 등록
RegisterLoadedPatternsAsSeed();
// → 로드된 모든 패턴을 확장의 시작점으로 등록
```

**2. 자동 확장 프로세스**:
```
플레이어 이동
    ↓
Update() → checkInterval마다 체크 (기본 1초)
    ↓
CheckAndExpandAroundPlayer(playerPos)
    ↓
FindNearbyGrids(playerPos, distance*2) → 근처 그리드 탐색
    ↓
각 그리드의 4방향(North, South, East, West) 체크
    ↓
IsPlayerNearBoundary(playerPos, grid, direction, expansionDistance)
    ↓ (플레이어가 경계 75 유닛 이내)
ExpandToDirection(grid, direction)
    ↓
GetValidNextPattern(currentPatternID, direction)
    ↓ (연결 규칙 기반 패턴 선택)
streamingManager.LoadPattern(nextPatternID, nextGrid)
    ↓
_generatedGrids[nextGrid] = nextPatternID
```

**3. 연결 규칙 기반 패턴 선택**:
```csharp
private string GetValidNextPattern(string currentPatternID, Direction direction)
{
    var currentPattern = patternRegistry.GetPattern(currentPatternID);

    // 1. 해당 방향의 ConnectionPoint 확인
    var connection = currentPattern.Connections
        .FirstOrDefault(c => c.Direction == direction && c.IsActive);

    // 2. ValidNextPatterns에서 랜덤 선택
    if (connection.ValidNextPatterns != null && connection.ValidNextPatterns.Count > 0)
    {
        int randomIndex = Random.Range(0, connection.ValidNextPatterns.Count);
        return connection.ValidNextPatterns[randomIndex];
    }

    // 3. 연결 규칙이 없으면 같은 타입의 랜덤 패턴
    var randomPattern = patternRegistry.GetRandomPattern(currentPattern.Type);
    return randomPattern?.PatternID;
}
```

**4. 경계 감지 로직**:
```csharp
private bool IsPlayerNearBoundary(Vector3 playerPos, Vector2Int grid, Direction direction, float threshold)
{
    // 그리드의 월드 위치 계산
    Vector3 gridWorldPos = new Vector3(
        grid.x * pattern.WorldSize.x,
        grid.y * pattern.WorldSize.y,
        0
    );

    // 방향별 경계 확인
    switch (direction)
    {
        case Direction.North:
            float northBoundary = gridWorldPos.y + pattern.WorldSize.y;
            return playerPosition.y > northBoundary - threshold;

        // South, East, West 동일 방식
    }
}
```

**설정 파라미터**:
- **enableAutoExpansion**: 자동 확장 활성화 (기본: true)
- **expansionDistance**: 확장 트리거 거리 (기본: 75 유닛)
  - 플레이어가 패턴 경계로부터 이 거리 내에 오면 확장
- **maxGeneratedPatterns**: 최대 생성 패턴 수 (기본: 50개)
  - 메모리 관리를 위한 제한
- **checkInterval**: 확장 체크 주기 (기본: 1초)
  - 성능 최적화를 위한 주기적 체크

**생성 추적**:
```csharp
// _generatedGrids: 생성된 모든 그리드 추적
Dictionary<Vector2Int, string> _generatedGrids;
// Vector2Int: 그리드 오프셋 (0,0), (1,0), (0,1) 등
// string: 패턴 ID

// _seedGrids: 시드 패턴 목록 (확장의 시작점)
List<Vector2Int> _seedGrids;
```

**디버그 시각화**:
```csharp
private void OnDrawGizmos()
{
    // 생성된 그리드: 파란색 와이어프레임
    // 시드 그리드: 녹색 와이어프레임
    // 플레이어: 노란색 구체
    // 확장 거리: 반투명 노란색 구체
}
```

#### 2. ProceduralMapPlayer (플레이어 컴포넌트)

**목적**: 플레이어 오브젝트에 붙여서 ProceduralMapGenerator에 자동 등록

**주요 기능**:
```csharp
[SerializeField] private bool registerOnStart = true;
[SerializeField] private bool showDebugLogs = false;

private void Start()
{
    if (registerOnStart)
        RegisterToGenerator();
}

public void RegisterToGenerator()
{
    _generator = ProceduralMapGenerator.Instance;
    _generator.SetPlayerTransform(transform);
}

public void UnregisterFromGenerator()
{
    _generator.SetPlayerTransform(null);
}
```

**사용 방법**:
```
1. 플레이어 GameObject 선택
2. Add Component → ProceduralMapPlayer
3. Inspector 설정:
   - Register On Start: true (자동 등록)
   - Show Debug Logs: false (디버그 로그)
4. 플레이어가 이동하면 자동으로 맵 확장
```

**통합 워크플로우**:
```
GameManager 초기화
    ↓
TilemapStreamingManager 초기화
    ↓
ProceduralMapGenerator 초기화
    ↓
LoadingFlow.Enter()
    ↓
LoadInitialPatterns(subSceneName)
    ↓
ProceduralMapGenerator.RegisterLoadedPatternsAsSeed()
    ↓
플레이어 생성
    ↓
ProceduralMapPlayer.RegisterToGenerator()
    ↓
플레이어 이동 시작
    ↓
자동 맵 확장 시작
```

**성능 최적화**:
- **주기적 체크**: 매 프레임이 아닌 checkInterval마다 체크
- **최대 패턴 제한**: maxGeneratedPatterns로 메모리 관리
- **근처 그리드만 체크**: FindNearbyGrids로 범위 제한
- **중복 생성 방지**: _generatedGrids로 이미 생성된 그리드 스킵
- **비동기 로딩**: UniTask로 프레임 드랍 방지

**에러 처리**:
- 레지스트리 미할당 시 에러 로그
- 스트리밍 매니저 미발견 시 자동 찾기
- 최대 패턴 수 도달 시 경고
- 패턴 로드 실패 시 예외 처리

**디버그 지원**:
- showDebugLogs: 상세 로그 출력
- showDebugGizmos: Scene View 시각화
- 생성된 그리드 수 추적
- 시드 패턴 구분 표시

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

### 4. 프로시저럴 맵 생성 사용

**Step 1**: ProceduralMapGenerator 설정
```
Hierarchy 우클릭
→ Create Empty → "ProceduralMapGenerator"
→ Add Component → ProceduralMapGenerator

Inspector 설정:
- Pattern Registry: MainTilemapRegistry 할당
- Streaming Manager: (자동 찾기, 비워도 됨)
- Enable Auto Expansion: true
- Expansion Distance: 75 (기본값)
- Max Generated Patterns: 50 (기본값)
- Check Interval: 1 (기본값)
```

**Step 2**: 플레이어 설정
```
플레이어 GameObject 선택
→ Add Component → ProceduralMapPlayer

Inspector 설정:
- Register On Start: true
- Show Debug Logs: false
```

**Step 3**: 초기화 코드 (LoadingFlow 또는 GameManager)
```csharp
// SubScene 로드 후
await TilemapStreamingManager.Instance.LoadInitialPatterns("Level1_SubScene");

// 로드된 패턴을 시드로 등록
ProceduralMapGenerator.Instance.RegisterLoadedPatternsAsSeed();

// 또는 수동으로 시드 패턴 등록
ProceduralMapGenerator.Instance.RegisterSeedPattern("Forest_01", Vector2Int.zero);
```

**Step 4**: 패턴 연결 규칙 설정
```
패턴 ScriptableObject에서:
- Connections 리스트 설정
- Direction: North, South, East, West
- Valid Next Patterns: 연결 가능한 패턴 ID 목록
- Is Active: true

예시:
Forest_01 패턴:
- North Connection: [Forest_02, Bridge_01, Cave_01]
- East Connection: [Village_01]
- South Connection: [Forest_01, Forest_02]
- West Connection: (비활성화)
```

**Step 5**: 게임 실행 및 테스트
```
1. Play 버튼 클릭
2. 플레이어를 패턴 경계로 이동
3. 경계로부터 75 유닛 이내에 도달하면 자동 확장
4. Scene View에서 Gizmos로 생성된 그리드 확인
   - 녹색: 시드 패턴
   - 파란색: 생성된 패턴
   - 노란색 구체: 플레이어
```

**수동 확장 (선택)**:
```csharp
// 특정 방향으로 수동 확장
Vector2Int currentGrid = new Vector2Int(0, 0);
await ProceduralMapGenerator.Instance.ExpandToDirection(currentGrid, Direction.North);

// 생성된 그리드 확인
bool isGenerated = ProceduralMapGenerator.Instance.IsGridGenerated(new Vector2Int(1, 0));
int totalPatterns = ProceduralMapGenerator.Instance.GeneratedPatternCount;
```

---

## 향후 계획

### Phase 2 (완료) ✅

- [x] `TilemapStreamingManager.cs` 구현
- [x] Addressables 통합
- [x] 로딩/언로딩 로직 구현
- [x] 자동 스트리밍 시스템

### Phase 3 (완료) ✅

- [x] LoadingFlow 통합
- [x] TownFlow 통합
- [x] 초기 패턴 로딩 구현
- [x] Flow 전환 시 패턴 자동 관리
- [x] 에러 처리 구현
- [x] 테스트 시나리오 작성

**다음 단계**:
- [ ] 성능 테스트 및 최적화
- [ ] 실제 게임 패턴 생성 및 테스트

### Phase 4 (완료) ✅

- [x] 에디터 검증 도구 (TilemapPatternValidator.cs)
- [x] 패턴 프리뷰 시스템 (TilemapPatternPreview.cs)
- [x] 매핑 관리 윈도우 (TilemapMappingWindow.cs)

**다음 단계**:
- [ ] 에디터 도구 실사용 테스트
- [ ] 워크플로우 개선 피드백 반영

### Phase 5 (완료) ✅

- [x] 프로시저럴 맵 생성기 (ProceduralMapGenerator.cs)
- [x] 플레이어 위치 추적 (ProceduralMapPlayer.cs)
- [x] 연결 규칙 기반 패턴 선택
- [x] 자동 맵 확장 시스템

**다음 단계**:
- [ ] 실제 게임에서 프로시저럴 생성 테스트
- [ ] 다양한 패턴 조합 테스트
- [ ] 성능 최적화 및 튜닝

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

HighLevel (매니저 & Flow):
├── Manager/TilemapStreamingManager.cs (완료 - 503 lines)
└── Flow/LoadingFlow.cs (완료 - 98 lines)
└── Flow/HomeFlow.cs (TownFlow, 완료 - 88 lines)

Docs (문서):
├── TilemapPatternStreaming_Design.md (완료 - v1.4)
└── TilemapStreaming_TestScenarios.md (완료 - v1.0)

EditorLevel (에디터 도구 - 완료):
├── Editor/Tilemap/TilemapPatternValidator.cs (완료 - 457 lines)
├── Editor/Tilemap/TilemapPatternPreview.cs (완료 - 406 lines)
└── Editor/Tilemap/TilemapMappingWindow.cs (완료 - 438 lines)

HighLevel (프로시저럴 맵 생성 - 완료):
├── Manager/ProceduralMapGenerator.cs (완료 - 446 lines)
└── Controller/ProceduralMapPlayer.cs (완료 - 72 lines)
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

**문서 버전**: 1.4
**최종 수정**: 2025-10-14
**작성자**: Claude Code
**상태**: Phase 5 완료, 프로시저럴 맵 생성 시스템 구축 완료

---

## 변경 이력

### v1.4 (2025-10-14) - Phase 5 완료 + 어셈블리 재배치
- ✅ 프로시저럴 맵 생성 시스템 구축 완료
- ✅ `ProceduralMapGenerator.cs`: 프로시저럴 맵 생성 매니저 (446 lines)
- ✅ `ProceduralMapPlayer.cs`: 플레이어 위치 추적 컴포넌트 (72 lines)
- ✅ 플레이어 거리 기반 자동 확장
- ✅ 연결 규칙 기반 패턴 선택
- ✅ 무한 맵 생성 지원
- ✅ 어셈블리 레벨 규칙 준수 및 파일 재배치
- ✅ `TilemapPatternData.cs` 버그 수정 (FindIndex null 체크 문제)

**주요 변경사항**:
- **ProceduralMapGenerator**: 방향 기반 맵 확장, 시드 패턴 등록, 자동 확장 로직
- **자동 확장 프로세스**: 플레이어 위치 감지 → 경계 체크 → 연결 규칙 기반 패턴 선택 → 패턴 로드
- **연결 규칙**: ConnectionPoint의 ValidNextPatterns 활용, 없으면 같은 타입 랜덤 선택
- **성능 최적화**: 주기적 체크(1초), 최대 패턴 제한(50개), 근처 그리드만 확인
- **디버그 시각화**: Scene View에서 생성된 그리드, 시드 패턴, 플레이어 위치 표시
- **ProceduralMapPlayer**: 플레이어에 붙여서 자동 등록, Start 시 자동 초기화
- **통합 워크플로우**: TilemapStreamingManager와 완전 통합

**어셈블리 재배치**:
- **ProceduralMapPlayer.cs**: MiddleLevel/Support → HighLevel/Controller로 이동
  - 이유: HighLevel의 ProceduralMapGenerator 참조 (의존성 규칙 위반 해결)
  - namespace: TS.MiddleLevel.Support → TS.HighLevel.Controller
- **TilemapPatternData.cs**: GetValidNextPatterns() 버그 수정
  - FindIndex는 int 반환이므로 null 체크 불가능 문제 해결
  - OnValidate에서 ValidNextPatterns 초기화 보장 추가
- **ProceduralMapGenerator.cs**: GetValidNextPattern() 개선
  - FirstOrDefault 대신 FindIndex 사용으로 명확한 인덱스 체크

### v1.3 (2025-10-14) - Phase 4 완료
- ✅ 에디터 도구 3종 구축 완료
- ✅ `TilemapPatternValidator.cs`: 패턴 데이터 검증 도구 (457 lines)
- ✅ `TilemapPatternPreview.cs`: Scene View 패턴 프리뷰 (406 lines)
- ✅ `TilemapMappingWindow.cs`: SubScene-Pattern 매핑 관리 (438 lines)
- ✅ 통합 개발 워크플로우 완성
- ✅ Unity Editor 메뉴 통합 (TS/Tilemap/)

**주요 변경사항**:
- **TilemapPatternValidator**: 중복 ID, Addressable 참조, 연결 패턴, SubScene 매핑, 카테고리 검증
- **TilemapPatternPreview**: Scene View 시각화, 패턴 경계/그리드/연결 지점 표시, 인터랙티브 편집
- **TilemapMappingWindow**: SubScene-Pattern 매핑 관리, 패턴 추가/제거/순서 조정, 검색 필터
- **개발 워크플로우**: Validator → Preview → Mapping → 게임 테스트
- **에디터 통합**: 모든 도구가 동일한 Registry 공유, 자동 검색, 즉시 저장

### v1.2 (2025-10-14) - Phase 3 완료
- ✅ LoadingFlow 통합 완료
- ✅ TownFlow (HomeFlow) 통합 완료
- ✅ FlowManager와의 완전한 통합
- ✅ Flow 전환 시 패턴 자동 관리
- ✅ 에러 처리 및 Fallback 구현
- ✅ 테스트 시나리오 문서 작성 (18개 테스트 케이스)
- ✅ Inspector 설정 옵션 추가

**주요 변경사항**:
- LoadingFlow.cs: Enter/Exit 메서드에 타일맵 로딩/언로딩 추가 (98 lines)
- HomeFlow.cs (TownFlow): 동일 구조로 통합 (88 lines)
- 로그 시스템 추가: 각 Flow별 상세 로그 출력
- 설정 옵션: loadTilemapPatterns, tilemapSubSceneName

### v1.1 (2025-10-14) - Phase 2 완료
- ✅ Phase 2 완료: TilemapStreamingManager 구현
- ✅ Addressables 통합 완료
- ✅ 자동 스트리밍 시스템 구현
- ✅ 로딩 대기열 시스템 추가
- ✅ 디버그 시각화 기능 추가

**주요 구현사항**:
- TilemapStreamingManager.cs: 503 lines
- 6개 주요 기능 영역 (초기화, 로딩, 언로딩, 자동 스트리밍, 대기열, 디버그)
- 성능 최적화: 비동기, 중복 방지, 동시성 제한, 캐싱

### v1.0 (2025-10-14) - Phase 1 완료
- ✅ Phase 1 완료: 기반 데이터 구조
- ✅ TilemapPatternData 생성
- ✅ TilemapPatternRegistry 생성
- ✅ 초기 설계 문서 작성

**주요 데이터 구조**:
- TilemapPatternData: 패턴 정의 ScriptableObject
- TilemapPatternRegistry: 패턴 관리 레지스트리
- ConnectionPoint: 패턴 간 연결 시스템
