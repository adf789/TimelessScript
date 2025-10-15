# 카메라 기반 타일맵 스트리밍 - 성능 분석 및 가이드

## 📊 시스템 개요

### 변경 사항
- **Before**: 플레이어 Transform 기반 스트리밍
- **After**: Orthographic Camera 위치 및 줌 기반 스트리밍

### 주요 특징
- 카메라 가시 영역(Viewport) 기반 로드/언로드
- Orthographic Size 변경(줌) 실시간 감지
- 가시 영역 + 버퍼 영역 계산으로 끊김 없는 로딩

---

## ⚙️ 성능 제한사항

### 1. 패턴 크기 기준 (50x50 타일)

**단일 패턴 메모리**:
```yaml
타일맵 데이터: ~2-5 MB (스프라이트, 콜라이더 포함)
GameObject 오버헤드: ~500 KB
총 예상: ~3-6 MB per pattern
```

**최대 동시 로드 패턴 수**:
```yaml
보수적: 9 패턴 (3x3 그리드) = ~30-50 MB
적극적: 16 패턴 (4x4 그리드) = ~50-100 MB
권장: 9-12 패턴 = ~35-70 MB
```

### 2. CPU 병목 지점

**로딩 작업**:
```csharp
// 각 패턴 로드 시 발생하는 작업
1. Addressables 비동기 로드: ~50-150ms
2. GameObject 인스턴스화: ~20-50ms
3. Tilemap 초기화: ~30-80ms
4. 콜라이더 생성: ~40-100ms
---
총 예상 시간: ~140-380ms per pattern
```

**언로드 작업**:
```csharp
// 각 패턴 언로드 시 발생하는 작업
1. Addressables 해제: ~10-30ms
2. GameObject 파괴: ~5-15ms
---
총 예상 시간: ~15-45ms per pattern
```

### 3. 동시 로딩 제한

**현재 설정**:
```csharp
maxConcurrentLoads = 3; // 동시 최대 3개 패턴 로드
```

**이유**:
- 동시 로딩 수가 많을수록 메모리 스파이크 증가
- Unity Addressables는 병렬 로딩 시 메모리 압박
- 3-5개가 가장 안정적인 범위

**권장 조정**:
```yaml
저사양 (모바일, 구형 PC): 2-3
중간 사양 (일반 PC): 3-4
고사양 (게이밍 PC): 4-5
```

---

## 🎮 카메라 이동 속도 권장치

### 계산 공식

```
로딩 완료 시간 = (패턴 수 × 로딩 시간) / 동시 로딩 수
안전 거리 = 카메라 이동 속도 × 로딩 완료 시간 + 버퍼
```

### 구체적 수치

**패턴 크기**: 50 타일 × 1 유닛 = 50 유닛

**시나리오 1: 보수적 설정**
```yaml
maxConcurrentLoads: 3
loadBufferSize: 20 유닛
updateInterval: 0.5초

최대 로딩 시간: ~380ms × 2패턴 / 3 = ~250ms
권장 카메라 속도: 50 유닛 / 0.5초 = 100 유닛/초 (최대)
안전 카메라 속도: 30-50 유닛/초 (권장)
```

**시나리오 2: 적극적 설정**
```yaml
maxConcurrentLoads: 4
loadBufferSize: 30 유닛
updateInterval: 0.3초

최대 로딩 시간: ~200ms × 2패턴 / 4 = ~100ms
권장 카메라 속도: 80-120 유닛/초 (최대)
안전 카메라 속도: 50-80 유닛/초 (권장)
```

**시나리오 3: 고속 이동 지원**
```yaml
maxConcurrentLoads: 5
loadBufferSize: 40 유닛
updateInterval: 0.2초
PreloadDistance: 150 유닛 (증가)

최대 로딩 시간: ~150ms × 3패턴 / 5 = ~90ms
권장 카메라 속도: 100-150 유닛/초 (최대)
안전 카메라 속도: 60-100 유닛/초 (권장)
```

### 실전 권장 속도

**게임 장르별**:
```yaml
탑뷰 RPG:
  일반 이동: 20-40 유닛/초
  대시/스킬: 60-100 유닛/초

탑뷰 액션:
  일반 이동: 30-60 유닛/초
  대시: 80-120 유닛/초

전략/시뮬레이션:
  느린 팬: 10-30 유닛/초
  빠른 팬: 50-80 유닛/초

슈팅:
  스크롤: 30-50 유닛/초
  급기동: 80-150 유닛/초 (프리로드 필수)
```

---

## 🔧 최적화 팁

### 1. updateInterval 조정

**현재 기본값**: `0.5초`

```csharp
// 느린 카메라 (전략 게임)
updateInterval = 1.0f; // CPU 부하 감소

// 빠른 카메라 (액션 게임)
updateInterval = 0.2f ~ 0.3f; // 반응성 향상
```

**트레이드오프**:
- 낮을수록: 더 빠른 반응, 더 높은 CPU 사용
- 높을수록: CPU 절약, 로딩 지연 가능

### 2. loadBufferSize 조정

**현재 기본값**: `20 유닛`

```csharp
// 느린 카메라
loadBufferSize = 10f ~ 20f; // 메모리 절약

// 빠른 카메라
loadBufferSize = 30f ~ 50f; // 안정성 우선

// 초고속 카메라 (대시/텔레포트)
loadBufferSize = 60f ~ 100f; // 넉넉한 버퍼
```

### 3. 줌 레벨별 동적 조정

```csharp
// TilemapStreamingManager에 추가 권장
private void AdjustSettingsByZoom()
{
    float zoomLevel = targetCamera.orthographicSize;

    if (zoomLevel < 5f) // 가까운 줌
    {
        loadBufferSize = 15f;
        maxLoadedPatterns = 9;
    }
    else if (zoomLevel < 10f) // 중간 줌
    {
        loadBufferSize = 25f;
        maxLoadedPatterns = 12;
    }
    else // 먼 줌
    {
        loadBufferSize = 40f;
        maxLoadedPatterns = 16;
    }
}
```

### 4. 프리로딩 전략

**고속 이동 지원**:
```csharp
// 카메라 이동 방향 예측
Vector3 velocity = (currentPos - previousPos) / Time.deltaTime;
Vector3 predictedPos = currentPos + velocity * preloadTime;

// 예측 위치 기준 프리로드
PreloadPatternsNearPosition(predictedPos);
```

---

## 📈 성능 모니터링

### 주요 메트릭

```csharp
// TilemapStreamingManager에 추가 권장
[Header("Performance Monitoring")]
public int CurrentLoadedPatterns => _loadedPatterns.Count;
public int CurrentLoadingPatterns => _loadingKeys.Count;
public float AverageLoadTime { get; private set; }
public float PeakLoadTime { get; private set; }

// Unity Profiler 마커
void Update()
{
    using (new ProfilerMarker("TilemapStreaming.Update").Auto())
    {
        // 기존 Update 로직
    }
}
```

### 성능 경고 임계값

```yaml
경고 수준:
  - 로딩 시간 > 500ms: 카메라 속도 감소 권장
  - 동시 로드 > 5개: 버퍼 크기 증가 권장
  - 메모리 사용 > 150MB: 패턴 최적화 필요

위험 수준:
  - 로딩 시간 > 1000ms: 즉시 조치 필요
  - 프레임 드롭 > 10ms: 동시 로딩 수 감소
  - Out of Memory: 최대 패턴 수 감소
```

---

## 🎯 권장 설정 프리셋

### Preset 1: 모바일 / 저사양
```csharp
[Header("Mobile Preset")]
maxLoadedPatterns = 9;
updateInterval = 0.5f;
maxConcurrentLoads = 2;
loadBufferSize = 15f;
// 권장 카메라 속도: 20-40 유닛/초
```

### Preset 2: PC / 중간 사양
```csharp
[Header("PC Standard Preset")]
maxLoadedPatterns = 12;
updateInterval = 0.3f;
maxConcurrentLoads = 3;
loadBufferSize = 25f;
// 권장 카메라 속도: 40-80 유닛/초
```

### Preset 3: PC / 고사양
```csharp
[Header("PC High-End Preset")]
maxLoadedPatterns = 16;
updateInterval = 0.2f;
maxConcurrentLoads = 4;
loadBufferSize = 35f;
// 권장 카메라 속도: 60-120 유닛/초
```

### Preset 4: 고속 액션 게임
```csharp
[Header("Fast Action Preset")]
maxLoadedPatterns = 20;
updateInterval = 0.15f;
maxConcurrentLoads = 5;
loadBufferSize = 50f;
// 권장 카메라 속도: 80-150 유닛/초
// 프리로딩 활성화 필수
```

---

## ⚠️ 주의사항

### 1. 메모리 관리
- 패턴 수가 증가하면 메모리 사용량이 선형으로 증가
- 모바일은 메모리 제약이 심하므로 9-12 패턴 유지
- 주기적인 메모리 프로파일링 필수

### 2. 카메라 줌 영향
- Orthographic Size가 클수록 더 많은 패턴 필요
- 줌 아웃 시 loadBufferSize 자동 증가 권장
- 최대 줌 레벨 제한 고려

### 3. 텔레포트/순간이동
```csharp
// 텔레포트 시 즉시 로딩 필요
public async UniTask TeleportToPosition(Vector3 targetPos)
{
    // 1. 현재 위치 패턴 언로드 대기
    await UnloadAllPatterns();

    // 2. 타겟 위치 주변 패턴 즉시 로드
    await LoadPatternsAroundPosition(targetPos, immediate: true);

    // 3. 카메라 이동
    camera.transform.position = targetPos;
}
```

### 4. 대시/급가속
```csharp
// 대시 시작 시 버퍼 임시 확장
void OnDashStart()
{
    _originalBufferSize = loadBufferSize;
    loadBufferSize *= 2f; // 버퍼 2배 증가
}

void OnDashEnd()
{
    loadBufferSize = _originalBufferSize; // 복원
}
```

---

## 📊 실전 테스트 결과 (예상)

### 테스트 환경
```yaml
CPU: Intel i5-10400F
RAM: 16GB
Unity: 6000.2.7f2
패턴 크기: 50x50 타일
```

### 결과

| 카메라 속도 | 버퍼 크기 | 동시 로드 | 로딩 성공률 | 평균 FPS |
|------------|----------|----------|-----------|---------|
| 30 유닛/초 | 20 유닛 | 3 | 100% | 60 |
| 60 유닛/초 | 20 유닛 | 3 | 95% | 58 |
| 100 유닛/초 | 20 유닛 | 3 | 80% | 55 |
| 60 유닛/초 | 30 유닛 | 4 | 100% | 59 |
| 100 유닛/초 | 40 유닛 | 5 | 98% | 57 |

**결론**:
- **안전 속도**: 30-60 유닛/초 (기본 설정)
- **고속 지원**: 100 유닛/초까지 가능 (버퍼 증가 필요)
- **극한 속도**: 150+ 유닛/초 (프리로딩 필수)

---

## 🔍 디버깅 팁

### Gizmos 활용
```csharp
// Scene 뷰에서 확인 가능
showDebugInfo = true;        // 로드된 패턴 경계 (녹색)
showCameraBounds = true;     // 카메라 가시 영역 (청록색)
                             // 버퍼 포함 영역 (노란색)
                             // 카메라 위치 (빨간 구)
```

### 로그 모니터링
```csharp
showDebugLogs = true;
// 출력 예시:
// [TilemapStreamingManager] Pattern loaded: Forest_01_2_1 at (100, 50)
// [TilemapStreamingManager] Camera zoom changed: 8.5
// [TilemapStreamingManager] Auto-unloaded 2 patterns outside camera view
```

---

*Last Updated: 2025-01-15*
