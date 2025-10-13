# TimelessScript

Unity DOTS 기반 2D 시뮬레이션 게임

## 개요

Unity DOTS (ECS, Burst, Job System)를 활용한 고성능 2D 시뮬레이션 게임 프로젝트입니다.

## 기술 스택

**Core**
- Unity 6000.2.0b12
- Unity Entities (DOTS) 1.3.14
- Burst Compiler
- Job System
- UniTask

**Rendering**
- Universal Render Pipeline 17.2.0
- Custom 2D Physics System

## 아키텍처

### 4계층 Assembly 구조

```
EditorLevel     에디터 도구 (윈도우, 인스펙터)
    ↓
HighLevel       게임 관리 (Manager, System, Flow)
    ↓
MiddleLevel     비즈니스 로직 (Job, 물리, 뷰)
    ↓
LowLevel        데이터 레이어 (Component, Enum, Model)
```

**계층별 주요 구성:**
- **LowLevel**: 27개 IComponentData, 열거형, 데이터 모델
- **MiddleLevel**: 21개 Burst Job (물리, 스폰, 행동, 애니메이션)
- **HighLevel**: 9개 ECS System, Manager 클래스, Flow 관리
- **EditorLevel**: 커스텀 에디터 윈도우 및 검증 도구

## 핵심 시스템

### 1. 최적화된 물리 시스템
- Burst 컴파일로 네이티브 성능
- Ground 오브젝트 Persistent 캐싱
- Job System 병렬 처리
- 커스텀 중력, 드래그, AABB 충돌

### 2. 동적 스폰 시스템
- 계층 구조 오브젝트 생성 (부모-자식)
- 지형 기반 자동 배치
- 실시간 생성/제거 관리
- 쿨다운 및 최대 개수 제한

### 3. 네비게이션 시스템
- Waypoint 기반 경로 탐색
- 사다리 등반 (ClimbUp/ClimbDown)
- 지형 인식 및 상호작용

### 4. 행동 시스템 (AI)
- 네비게이션 통합
- 애니메이션 동기화
- 상태 기반 행동 전환

## 프로젝트 구조

```
Assets/TS/
├── Scenes/                  게임 씬 (Intro, Loading, Town)
├── Scripts/
│   ├── LowLevel/           데이터 및 컴포넌트
│   ├── MiddleLevel/        Job 구현 및 비즈니스 로직
│   ├── HighLevel/          System 및 Manager
│   └── EditorLevel/        에디터 도구
├── Resources/              리소스 파일
└── Prefabs/               프리팹 에셋
```

## 성능 최적화

**Burst Compilation**
- 모든 Job과 주요 System에 적용
- 네이티브 수준 실행 속도

**Job System**
- 멀티코어 CPU 병렬 처리
- 메인 스레드 부하 분산

**메모리 최적화**
- Ground 오브젝트 캐싱 (1회 할당)
- NativeArray 활용
- EntityQuery 재사용

## 게임 플로우

```
Intro (인트로) → Loading (리소스 로드) → Town (메인 게임)
```

**주요 Manager:**
- GameManager: 게임 전역 관리, 카메라 드래그
- FlowManager: 게임 상태 전환 (GameState.Intro/Loading/Town)

## 개발 환경

**필수 요구사항:**
- Unity 6000.2.0b12 이상
- .NET 9.0
- Visual Studio 2022 또는 Rider

**설치:**
1. Unity 6000.2.0b12 설치
2. 프로젝트 열기
3. UniTask 자동 설치 확인
4. Town 씬 실행

## 주요 컴포넌트

**Physics:**
- PhysicsComponent: 물리 시뮬레이션 데이터
- ColliderComponent: 충돌 검사
- GroundCollisionComponent: 지면 충돌

**Spawn:**
- SpawnConfigComponent: 스폰 설정
- SpawnedObjectComponent: 생성된 오브젝트 추적

**Navigation:**
- NavigationComponent: 경로 탐색 데이터
- NavigationWaypoint: 이동 경로 버퍼

**Actor:**
- TSActorComponent: 액터 행동 및 이동 상태
- TSObjectComponent: 오브젝트 기본 데이터

## 주요 System 실행 순서

```
Fixed Step:
  OptimizedPhysicsSystem (물리 시뮬레이션)
    ↓
Simulation:
  ControlSystem (입력 처리)
    ↓
  NavigationSystem (경로 탐색)
    ↓
  BehaviorSystem (AI 행동)
    ↓
  SpawnSystem (오브젝트 생성/관리)
    ↓
  CollisionSystem (충돌 처리)
    ↓
  AnimationSystem (애니메이션)
```

## 에디터 도구

**Scene Manager Window** (Ctrl+Alt+S)
- 모든 씬 목록 및 관리
- 씬 열기/Play 모드 시작
- Build Settings 필터링

**Spawn System Validator**
- 스폰 시스템 검증
- 샘플 설정 생성

## 프로젝트 통계

- ECS Systems: 9개
- Burst Jobs: 21개
- IComponentData: 27개
- 총 스크립트: 200+개

## 참고 자료

- [Unity DOTS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Burst Compiler Manual](https://docs.unity3d.com/Packages/com.unity.burst@latest)
- [UniTask GitHub](https://github.com/Cysharp/UniTask)

## 기입 날짜
- 2025.10.13
