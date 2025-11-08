# CLAUDE.md - Unity Project Guide

> 간결성 | Unity 6000.2.7f2 | 4-Layer Assembly | Hybrid MonoBehaviour + ECS

## Project Description
- 2D Side-Scrolling Slmulation
- Control Mouse or Touch
- Mobile Environment

## 📋 Quick Reference

### Project Stack
- **Unity**: 6000.2.7f2 (Beta)
- **Async**: UniTask (`https://github.com/Cysharp/UniTask.git`)
- **ECS**: Unity Entities 1.3.14 + Burst
- **Rendering**: URP 17.2.0 + Custom ToonLitSprite
- **Input**: Input System 1.14.2

### Core Patterns
- **Managers**: `BaseManager<T>` singleton (MonoBehaviour)
- **SubManagers**: `SubBaseManager<T>` singleton (Non-MonoBehaviour)
- **Flows**: `BaseFlow` state management
- **Resources**: Type-based registry with `ResourcesPath` attribute
- **Physics**: Custom `LightweightPhysics2D`
- **ECS**: Hybrid authoring + runtime separation

### Singleton Naming Convention
| Type | Base Class | Suffix | Location | Use Case |
|------|-----------|--------|----------|----------|
| MonoBehaviour Singleton | `BaseManager<T>` | `Manager` | HighLevel/Manager | GameObject lifecycle, Start/Update, scene objects |
| Non-MonoBehaviour Singleton | `SubBaseManager<T>` | `SubManager` | MiddleLevel/SubManager | Pure data/logic, no GameObject needed |

**Examples**:
```csharp
// ✅ MonoBehaviour Singleton
public class GameManager : BaseManager<GameManager> { }
public class ProceduralMapManager : BaseManager<ProceduralMapManager> { }

// ✅ Non-MonoBehaviour Singleton
public class DatabaseSubManager : SubBaseManager<DatabaseSubManager> { }
public class GameDataSubManager : SubBaseManager<GameDataSubManager> { }
```

---

## 🏗️ Assembly Architecture (CRITICAL)

### 4-Layer Dependency Rule
```
EditorLevel (#if UNITY_EDITOR)
    ↓ can reference all
HighLevel (Managers, Controllers, Flows, ECS Systems)
    ↓ can reference: Low, Middle
MiddleLevel (MonoBehaviour, Views, Jobs, Authoring)
    ↓ can reference: Low only
LowLevel (Data, Enums, ScriptableObjects)
    ↓ independent
```

### Layer Responsibilities

| Layer | ✅ Allowed | ❌ Forbidden | Path |
|-------|-----------|-------------|------|
| **LowLevel** | ScriptableObject, struct, enum, data | MonoBehaviour, Manager refs, scene objects | `Assets/TS/Scripts/LowLevel/` |
| **MiddleLevel** | MonoBehaviour, Views, SubManager, Jobs, Authoring | HighLevel refs (Manager/Controller/Flow) | `Assets/TS/Scripts/MiddleLevel/` |
| **HighLevel** | `BaseManager<T>`, Controllers, Flows, ECS Systems | Direct View manipulation | `Assets/TS/Scripts/HighLevel/` |
| **EditorLevel** | Editor tools, inspectors, all refs | Code without `#if UNITY_EDITOR` | `Assets/TS/Scripts/EditorLevel/` |

### Common Violations
```csharp
// ❌ WRONG: MiddleLevel referencing HighLevel
namespace TS.MiddleLevel.Support
{
    public class Player : MonoBehaviour
    {
        GameManager manager; // ❌ GameManager is in HighLevel
    }
}

// ✅ CORRECT: Move to HighLevel
namespace TS.HighLevel.Controller
{
    public class PlayerController : MonoBehaviour
    {
        GameManager manager; // ✅ Both in HighLevel
    }
}

// ❌ WRONG: LowLevel with MonoBehaviour
namespace TS.LowLevel.Data
{
    public class Config : MonoBehaviour { } // ❌ Use ScriptableObject
}

// ✅ CORRECT: LowLevel data-only
namespace TS.LowLevel.Data.Config
{
    [CreateAssetMenu(...)]
    public class ConfigData : ScriptableObject { } // ✅ Data only
}
```

---

## 🎯 Unity-Specific Guidelines

### Code Generation Rules

**❌ DO NOT create namespace declarations** - Files already have namespaces
```csharp
// ❌ WRONG: Don't generate namespace
namespace TS.HighLevel.Manager
{
    public class MyManager : BaseManager<MyManager>
    {
    }
}

// ✅ CORRECT: Only generate class body
public class MyManager : BaseManager<MyManager>
{
    // Implementation only
}
```

### Variable Naming Convention
**멤버 변수는 반드시 `_` 접두사 사용**
```csharp
// ✅ CORRECT: Member variables with _ prefix
public class MyComponent : MonoBehaviour
{
    private MonoScript _selectedScript;        // ✅ Member variable
    private List<string> _items = null;        // ✅ Member variable
    private Vector2 _scrollPosition;           // ✅ Member variable
    private bool _isEnabled = true;            // ✅ Member variable

    private void Update()
    {
        string scriptName = _selectedScript.name;  // ✅ Local variable (no prefix)
        int count = _items.Count;                  // ✅ Local variable (no prefix)
    }
}

// ❌ WRONG: Member variables without _ prefix
public class MyComponent : MonoBehaviour
{
    private MonoScript selectedScript;  // ❌ Missing _ prefix
    private List<string> items;         // ❌ Missing _ prefix
}
```

### MonoBehaviour Lifecycle
```csharp
// BaseManager<T> Pattern
public class MyManager : BaseManager<MyManager>
{
    // ❌ NO: public override void Initialize()
    // ✅ YES: Use MonoBehaviour lifecycle

    private void Start()  // ✅ Initialization
    {
        // BaseManager.Awake() handles singleton setup
        // Use Start() for your initialization
    }

    private void OnDestroy()  // ✅ Cleanup
    {
        // Release resources
    }
}
```

### ScriptableObject Validation
```csharp
[CreateAssetMenu(fileName = "Data", menuName = "TS/Data")]
public class MyData : ScriptableObject
{
#if UNITY_EDITOR
    private void OnValidate()  // ✅ Editor-time validation
    {
        // Initialize lists, validate references
        // Fix struct field initialization
    }
#endif
}
```

### Struct vs Class
```csharp
// ⚠️ Structs are value types
[Serializable]
public struct ConnectionPoint
{
    public List<string> ValidNextPatterns;
}

// ❌ WRONG: Null check on struct
var point = list.FirstOrDefault();
if (point == null) { } // ❌ Compile error

// ✅ CORRECT: Index-based check
int index = list.FindIndex(p => p.IsValid);
if (index < 0) return; // ✅ Check index
var point = list[index];
if (point.ValidNextPatterns == null) { } // ✅ Check field
```

### Namespace Convention
```
TS.LowLevel.Data.{Category}
TS.LowLevel.Data.Config
TS.MiddleLevel.{Category}
TS.MiddleLevel.Job.Physics
TS.HighLevel.{Category}
TS.HighLevel.Manager
TS.HighLevel.Controller
TS.EditorLevel.Editor.{Category}
```

---

## 🤖 AI Interaction Guidelines

### 코드 생성 전 필수 체크
```yaml
Before_Code_Generation:
  - "❌ Namespace 생성 금지 → 클래스 바디만 생성"
  - "✅ 멤버 변수 _ 접두사 필수 → private Type _variableName"
  - "이 클래스가 어느 Assembly에 속하나?" (LowLevel/MiddleLevel/HighLevel/EditorLevel)
  - "MonoBehaviour가 필요한가? → MiddleLevel 이상"
  - "Manager를 참조하나? → HighLevel"
  - "ScriptableObject 데이터인가? → LowLevel"
  - "Editor 전용인가? → #if UNITY_EDITOR 필수"

Naming_Convention:
  - "멤버 변수 → _camelCase (private int _count;)"
  - "로컬 변수 → camelCase (int count = _count;)"
  - "MonoBehaviour 싱글톤 → BaseManager<T> + ~Manager 접미사"
  - "Non-MonoBehaviour 싱글톤 → SubBaseManager<T> + ~SubManager 접미사"
  - "GameObject 필요? → Manager | 불필요? → SubManager"

Struct_Usage:
  - "Null 체크 불가 → FindIndex 사용"
  - "List 필드 → OnValidate()에서 초기화"
  - "값 복사 주의 → 수정 후 다시 할당"

MonoBehaviour_Lifecycle:
  - "BaseManager<T> → Awake(singleton), Start(init)"
  - "Initialize() override 불가 → Start() 사용"
  - "async 작업 → UniTask 사용"
```

### 효과적인 질문 형식
```markdown
**좋은 질문 형식**:
- "HighLevel에서 TilemapPatternData(LowLevel)를 참조하는 Manager 생성"
- "MiddleLevel MonoBehaviour가 ProceduralMapGenerator를 참조 → 어디로 이동?"
- "ScriptableObject의 List<struct> 필드 초기화 방법"

**피해야 할 질문**:
- "컴포넌트 만들어줘" (어느 레벨? MonoBehaviour? ScriptableObject?)
- "Manager 추가" (GameObject 있는 Manager? SubManager?)
- "데이터 클래스" (ScriptableObject? 일반 class?)
```

### 에러 보고 시 포함 정보
```yaml
Essential_Info:
  - 파일 경로: "Assets/TS/Scripts/{Level}/{Category}/{File}.cs:LineNumber"
  - 에러 메시지: "정확한 컴파일 에러 또는 런타임 예외"
  - 관련 타입: "MonoBehaviour/ScriptableObject/struct/class"
  - Assembly: "LowLevel/MiddleLevel/HighLevel/EditorLevel"
  - Unity 버전: "6000.2.7f2"

Example:
  "Assets/TS/Scripts/MiddleLevel/Support/Player.cs:23
   CS0246: The type or namespace name 'GameManager' could not be found
   → Player (MiddleLevel) referencing GameManager (HighLevel)
   → Move Player to HighLevel/Controller"
```

### 코드 리뷰 체크리스트
- [ ] ❌ **Namespace 생성하지 않았나?** (클래스 바디만 생성)
- [ ] ✅ **멤버 변수 _ 접두사 사용하나?** (private int _count)
- [ ] Assembly 레벨 적절한가?
- [ ] 의존성 방향 올바른가? (하위 → 상위만)
- [ ] MonoBehaviour vs ScriptableObject 선택 맞나?
- [ ] **싱글톤 네이밍 규칙 준수하나?** (Manager/SubManager 접미사)
- [ ] Struct null 체크 없나?
- [ ] List/Collection 초기화 됐나?
- [ ] `#if UNITY_EDITOR` 래핑 됐나? (EditorLevel)
- [ ] BaseManager 상속 시 Start() 사용하나?

---

## 📁 File Reference Format

**VSCode Terminal**: Ctrl+Click on path to open
```
.\Assets\TS\Scripts\HighLevel\Manager\GameManager.cs:45
```

**Path Tips**:
- 전체 경로가 터미널 너비 초과 시 가로 스크롤 (Shift+Mouse Wheel)
- `terminal.integrated.wordWrap: false` 설정 권장
- Line number 포함 시 해당 라인으로 바로 이동

---

## 🔧 Quick CLAUDE.md Update

**트리거 문구**: "앞으로도 적용해줘" / "앞으로도 적용되게 해줘"
→ 임시 변경사항을 CLAUDE.md에 영구 저장

---

## 📚 Reference Files

| Purpose | File Path |
|---------|-----------|
| Manager base | `HighLevel/Manager/BaseManager.cs` |
| Flow base | `HighLevel/Flow/BaseFlow.cs` |
| Resource registry | `MiddleLevel/Core/ResourcesTypeRegistry.cs` |
| Physics system | `HighLevel/System/Physics/OptimizedPhysicsSystem.cs` |
| Game entry point | `HighLevel/Manager/GameManager.cs` |

---

*Last Updated: 2025-01-15 | Unity 6000.2.7f2*
