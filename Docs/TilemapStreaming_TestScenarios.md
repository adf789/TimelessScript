# 타일맵 패턴 스트리밍 시스템 테스트 시나리오

**작성일**: 2025-10-14
**버전**: 1.0
**상태**: Phase 3 완료

---

## 📋 목차

1. [테스트 환경 설정](#테스트-환경-설정)
2. [Phase 1-2 통합 테스트](#phase-1-2-통합-테스트)
3. [Phase 3 FlowManager 통합 테스트](#phase-3-flowmanager-통합-테스트)
4. [성능 테스트](#성능-테스트)
5. [에러 처리 테스트](#에러-처리-테스트)
6. [엣지 케이스 테스트](#엣지-케이스-테스트)

---

## 테스트 환경 설정

### 필수 구성 요소

#### 1. TilemapPatternRegistry 설정
```
1. Project 창에서 우클릭
2. Create → TS → Tilemap → Pattern Registry
3. 이름: MainTilemapRegistry
4. Inspector 설정:
   - All Patterns: 테스트용 패턴 2-3개 추가
   - Initial Mappings:
     * SubSceneName: "Loading"
       Initial Patterns: [TestPattern_01]
     * SubSceneName: "Town"
       Initial Patterns: [TestPattern_02, TestPattern_03]
```

#### 2. TilemapPatternData 생성 (테스트용)
```
패턴 1: TestPattern_01
- PatternID: "TestPattern_01"
- GridSize: (50, 50)
- Type: Forest
- TilemapPrefab: 간단한 타일맵 프리팹 (빈 GameObject도 가능)
- UnloadDistance: 100f

패턴 2: TestPattern_02
- PatternID: "TestPattern_02"
- GridSize: (50, 50)
- Type: Cave
- TilemapPrefab: 다른 타일맵 프리팹
- UnloadDistance: 100f

패턴 3: TestPattern_03
- PatternID: "TestPattern_03"
- GridSize: (50, 50)
- Type: Bridge
- Connections:
  * North: ["TestPattern_02"]
  * East: ["TestPattern_01"]
```

#### 3. TilemapStreamingManager 설정
```
1. Hierarchy에 빈 GameObject 생성
2. 이름: TilemapStreamingManager
3. TilemapStreamingManager 컴포넌트 추가
4. Inspector 설정:
   - Pattern Registry: MainTilemapRegistry 연결
   - Max Loaded Patterns: 9
   - Update Interval: 0.5
   - Enable Auto Streaming: true
   - Show Debug Info: true
```

#### 4. LoadingFlow ScriptableObject 설정
```
1. Resources 폴더에서 LoadingFlow 찾기
2. Inspector 설정:
   - Load Tilemap Patterns: true
   - Tilemap SubScene Name: "Loading" (또는 비워두기)
```

#### 5. TownFlow ScriptableObject 설정
```
1. Resources 폴더에서 TownFlow 찾기
2. Inspector 설정:
   - Load Tilemap Patterns: true
   - Tilemap SubScene Name: "Town" (또는 비워두기)
```

---

## Phase 1-2 통합 테스트

### 테스트 1: 레지스트리 초기화
**목적**: TilemapPatternRegistry가 정상적으로 초기화되는지 확인

**절차**:
1. Play 모드 진입
2. Console 확인

**예상 결과**:
```
[TilemapPatternRegistry] Initialized with 3 patterns
[TilemapStreamingManager] Initialized. MaxPatterns: 9, UpdateInterval: 0.5s
```

**검증 항목**:
- ✅ 에러 없이 초기화
- ✅ 패턴 수 올바르게 표시
- ✅ 설정값 정상 로드

---

### 테스트 2: 패턴 수동 로드
**목적**: LoadPattern() 메서드가 정상 작동하는지 확인

**절차**:
1. Play 모드 진입
2. Console에서 다음 명령 실행:
```csharp
await TilemapStreamingManager.Instance.LoadPattern("TestPattern_01", Vector2Int.zero);
```

**예상 결과**:
```
[TilemapStreamingManager] Pattern loaded: TestPattern_01_0_0 at (0, 0, 0)
```

**검증 항목**:
- ✅ 패턴 인스턴스가 Hierarchy에 생성됨
- ✅ 위치가 (0, 0, 0)에 정확히 배치
- ✅ Scene View에서 Gizmo로 경계 표시됨 (녹색)
- ✅ LoadedPatternCount = 1

---

### 테스트 3: 패턴 중복 로드 방지
**목적**: 같은 패턴을 두 번 로드하려 할 때 중복 방지 확인

**절차**:
1. TestPattern_01 로드
2. 같은 패턴을 다시 로드 시도

**예상 결과**:
```
[TilemapStreamingManager] Pattern already loaded: TestPattern_01_0_0
```

**검증 항목**:
- ✅ Warning 로그 출력
- ✅ LoadedPatternCount 변화 없음
- ✅ 메모리 사용량 변화 없음

---

### 테스트 4: 여러 패턴 병렬 로드
**목적**: LoadInitialPatterns()가 여러 패턴을 병렬로 로드하는지 확인

**절차**:
1. Play 모드 진입
2. 다음 호출:
```csharp
await TilemapStreamingManager.Instance.LoadInitialPatterns("Town");
```

**예상 결과**:
```
[TilemapStreamingManager] Loading 2 initial patterns for Town
[TilemapStreamingManager] Pattern loaded: TestPattern_02_0_0 at (0, 0, 0)
[TilemapStreamingManager] Pattern loaded: TestPattern_03_1_0 at (50, 0, 0)
[TilemapStreamingManager] Initial patterns loaded: 2
```

**검증 항목**:
- ✅ 두 패턴 모두 로드됨
- ✅ 위치가 겹치지 않음
- ✅ LoadedPatternCount = 2
- ✅ Scene View에서 두 개의 경계 박스 표시

---

### 테스트 5: 패턴 언로드
**목적**: UnloadPattern()이 정상 작동하는지 확인

**절차**:
1. TestPattern_01 로드
2. 패턴 언로드:
```csharp
await TilemapStreamingManager.Instance.UnloadPattern("TestPattern_01", Vector2Int.zero);
```

**예상 결과**:
```
[TilemapStreamingManager] Pattern unloaded: TestPattern_01_0_0
```

**검증 항목**:
- ✅ Hierarchy에서 GameObject 제거됨
- ✅ LoadedPatternCount 감소
- ✅ Scene View에서 Gizmo 사라짐
- ✅ 메모리 해제됨 (Profiler 확인)

---

### 테스트 6: 거리 기반 자동 언로드
**목적**: UpdateStreamingByPosition()이 먼 패턴을 자동 언로드하는지 확인

**절차**:
1. TestPattern_01을 (0, 0) 위치에 로드
2. 플레이어 위치를 (200, 200)으로 설정:
```csharp
TilemapStreamingManager.Instance.SetPlayerPosition(new Vector3(200, 200, 0));
await TilemapStreamingManager.Instance.UpdateStreamingByPosition(new Vector3(200, 200, 0));
```

**예상 결과**:
```
[TilemapStreamingManager] Auto-unloaded 1 distant patterns
```

**검증 항목**:
- ✅ 패턴이 자동으로 언로드됨
- ✅ UnloadDistance (100) 기준 정확히 작동
- ✅ LoadedPatternCount = 0

---

## Phase 3 FlowManager 통합 테스트

### 테스트 7: LoadingFlow 통합
**목적**: LoadingFlow가 타일맵 패턴을 자동으로 로드하는지 확인

**절차**:
1. Play 모드 진입
2. FlowManager를 통해 Loading State 진입:
```csharp
await FlowManager.Instance.ChangeFlow(GameState.Loading);
```

**예상 결과**:
```
Open: Loading
[LoadingFlow] Loading tilemap patterns for SubScene: Loading
[TilemapStreamingManager] Loading 1 initial patterns for Loading
[TilemapStreamingManager] Pattern loaded: TestPattern_01_0_0
[LoadingFlow] Tilemap patterns loaded successfully for Loading
Close: Loading
```

**검증 항목**:
- ✅ LoadingFlow.Enter() 호출 시 타일맵 로드
- ✅ Scene 로드 → 타일맵 로드 → UI 오픈 순서 확인
- ✅ LoadingFlow.Exit() 호출 시 타일맵 언로드
- ✅ 전체 프로세스 완료

---

### 테스트 8: TownFlow 통합
**목적**: TownFlow가 타일맵 패턴을 자동으로 로드하는지 확인

**절차**:
1. Play 모드 진입
2. FlowManager를 통해 Town State 진입:
```csharp
await FlowManager.Instance.ChangeFlow(GameState.Town);
```

**예상 결과**:
```
Open: Loading
[LoadingFlow] Loading tilemap patterns for SubScene: Loading
[TilemapStreamingManager] Pattern loaded: TestPattern_01_0_0
Close: Loading

Open: Town
[TownFlow] Loading tilemap patterns for SubScene: Town
[TilemapStreamingManager] Loading 2 initial patterns for Town
[TilemapStreamingManager] Pattern loaded: TestPattern_02_0_0
[TilemapStreamingManager] Pattern loaded: TestPattern_03_1_0
[TownFlow] Tilemap patterns loaded successfully for Town
```

**검증 항목**:
- ✅ Loading 패턴 언로드됨
- ✅ Town 패턴 로드됨
- ✅ 총 2개 패턴 로드
- ✅ FlowManager 플로우 전환 정상 작동

---

### 테스트 9: Flow 전환 시 패턴 교체
**목적**: Flow 전환 시 이전 패턴은 언로드되고 새 패턴이 로드되는지 확인

**절차**:
1. Loading → Town 전환
2. Town → Loading 재전환

**예상 결과**:
```
// Town → Loading
[TownFlow] Unloading tilemap patterns
[TilemapStreamingManager] Pattern unloaded: TestPattern_02_0_0
[TilemapStreamingManager] Pattern unloaded: TestPattern_03_1_0

[LoadingFlow] Loading tilemap patterns for SubScene: Loading
[TilemapStreamingManager] Pattern loaded: TestPattern_01_0_0
```

**검증 항목**:
- ✅ 이전 패턴 완전히 언로드
- ✅ 새 패턴 정상 로드
- ✅ 메모리 누수 없음
- ✅ LoadedPatternCount 정확

---

## 성능 테스트

### 테스트 10: 로딩 시간 측정
**목적**: 패턴 로딩 시간이 성능 목표(100ms) 이내인지 확인

**절차**:
1. Profiler 오픈
2. 단일 패턴 로드 측정
3. 3개 패턴 병렬 로드 측정

**목표**:
- 단일 패턴: < 100ms
- 3개 병렬: < 300ms (각 100ms)

**검증 항목**:
- ✅ Addressables.InstantiateAsync() 시간
- ✅ 전체 LoadPattern() 메서드 시간
- ✅ CPU 사용률 < 30%

---

### 테스트 11: 메모리 사용량 측정
**목적**: 메모리 사용량이 목표(패턴당 80KB) 이내인지 확인

**절차**:
1. Memory Profiler 오픈
2. 패턴 로드 전후 메모리 스냅샷
3. 3개 패턴 로드 후 총 메모리 확인

**목표**:
- 패턴당: ~80KB
- 3개 패턴: ~240KB

**검증 항목**:
- ✅ 타일 데이터 메모리
- ✅ GameObject 오버헤드
- ✅ 언로드 후 메모리 해제 확인

---

### 테스트 12: 동시 로딩 제한
**목적**: 최대 동시 로딩 수(3개) 제한이 작동하는지 확인

**절차**:
1. 5개 패턴을 빠르게 로드 요청
2. 실제 동시 로드 수 확인

**예상 동작**:
- 처음 3개는 즉시 로드
- 나머지 2개는 대기열에서 순차 처리

**검증 항목**:
- ✅ 동시 로딩 수 ≤ 3
- ✅ 대기열 정상 작동
- ✅ 모든 패턴 최종 로드 완료

---

## 에러 처리 테스트

### 테스트 13: 레지스트리 미할당
**목적**: TilemapStreamingManager에 레지스트리가 없을 때 에러 처리

**절차**:
1. TilemapStreamingManager의 Pattern Registry를 None으로 설정
2. Play 모드 진입

**예상 결과**:
```
[TilemapStreamingManager] PatternRegistry is not assigned!
```

**검증 항목**:
- ✅ Error 로그 출력
- ✅ 크래시 없음
- ✅ 다른 시스템 정상 작동

---

### 테스트 14: 패턴 ID 없음
**목적**: 존재하지 않는 패턴 ID 로드 시도 시 에러 처리

**절차**:
```csharp
await TilemapStreamingManager.Instance.LoadPattern("NonExistentPattern", Vector2Int.zero);
```

**예상 결과**:
```
[TilemapStreamingManager] Pattern not found: NonExistentPattern
```

**검증 항목**:
- ✅ Error 로그 출력
- ✅ null 반환
- ✅ 크래시 없음

---

### 테스트 15: Addressable 참조 없음
**목적**: TilemapPrefab이 설정되지 않았을 때 에러 처리

**절차**:
1. 테스트 패턴의 TilemapPrefab을 None으로 설정
2. 해당 패턴 로드 시도

**예상 결과**:
```
[TilemapStreamingManager] Invalid Addressable reference for pattern: TestPattern_01
```

**검증 항목**:
- ✅ Error 로그 출력
- ✅ 로딩 실패
- ✅ 크래시 없음

---

## 엣지 케이스 테스트

### 테스트 16: 최대 패턴 수 초과
**목적**: MaxLoadedPatterns(9개) 초과 시 자동 언로드 확인

**절차**:
1. 9개 패턴 로드
2. 10번째 패턴 로드 시도

**예상 결과**:
```
[TilemapStreamingManager] Max loaded patterns reached (9). Unloading distant patterns...
[TilemapStreamingManager] Pattern loaded: TestPattern_10_X_Y
```

**검증 항목**:
- ✅ 가장 먼 패턴 자동 언로드
- ✅ 새 패턴 정상 로드
- ✅ LoadedPatternCount ≤ 9

---

### 테스트 17: 플레이어 위치 빠른 변경
**목적**: 플레이어가 빠르게 이동할 때 스트리밍 안정성 확인

**절차**:
1. 플레이어 위치를 1초마다 크게 변경 (100 유닛 이상)
2. 10회 반복

**예상 동작**:
- 자동 언로드/로드가 안정적으로 작동
- 메모리 누수 없음

**검증 항목**:
- ✅ 크래시 없음
- ✅ 메모리 안정적
- ✅ LoadedPatternCount 정상 범위

---

### 테스트 18: Scene 전환 중 패턴 로딩
**목적**: Scene이 완전히 로드되기 전 타일맵 로드 시도 시 안정성 확인

**절차**:
1. FlowManager로 빠르게 Scene 전환
2. LoadingFlow.Enter() 도중 강제 종료 시도

**예상 동작**:
- 안전하게 취소 또는 완료 대기

**검증 항목**:
- ✅ 크래시 없음
- ✅ 리소스 누수 없음
- ✅ 다음 전환 정상 작동

---

## 테스트 체크리스트

### Phase 1-2 통합
- [ ] 레지스트리 초기화
- [ ] 패턴 수동 로드
- [ ] 중복 로드 방지
- [ ] 병렬 로드
- [ ] 패턴 언로드
- [ ] 자동 거리 기반 언로드

### Phase 3 FlowManager
- [ ] LoadingFlow 통합
- [ ] TownFlow 통합
- [ ] Flow 전환 시 패턴 교체

### 성능
- [ ] 로딩 시간 < 100ms
- [ ] 메모리 ~80KB/패턴
- [ ] 동시 로딩 제한 작동

### 에러 처리
- [ ] 레지스트리 미할당
- [ ] 패턴 ID 없음
- [ ] Addressable 참조 없음

### 엣지 케이스
- [ ] 최대 패턴 수 초과
- [ ] 플레이어 빠른 이동
- [ ] Scene 전환 중 로딩

---

**문서 버전**: 1.0
**최종 수정**: 2025-10-14
**작성자**: Claude Code
**상태**: Phase 3 완료
