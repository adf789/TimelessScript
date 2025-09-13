# GEMINI.md

This file provides guidance to Gemini when working with code in this repository.

## Project Overview

This is a Unity 2D game project using Unity 6000.2.0b12. The project has a sophisticated multi-layered architecture that combines traditional MonoBehaviour patterns with Unity DOTS (ECS) for performance-critical systems.

The codebase is structured into four layers with assembly definitions:
1.  **LowLevel** (`Assets/TS/Scripts/LowLevel/`): Core data structures, enums, models, and base components.
2.  **MiddleLevel** (`Assets/TS/Scripts/MiddleLevel/`): Business logic, physics, jobs, views, and support systems.
3.  **HighLevel** (`Assets/TS/Scripts/HighLevel/`): Game managers, flow control, controllers, and ECS systems.
4.  **EditorLevel** (`Assets/TS/Scripts/EditorLevel/`): Editor-only tools, inspectors, and workflow automation.

### Key Technologies
- **Engine**: Unity 6000.2.0b12
- **Async**: UniTask
- **ECS**: Unity Entities 1.3.14
- **Rendering**: Universal Render Pipeline (URP) 17.2.0
- **Input**: Unity Input System 1.14.2

## Building and Running

- **Unity Editor**: Open the project in Unity Editor (version 6000.2.0b12 is required).
- **Build**: Build the project through the Unity Editor: `File` → `Build Settings`.
- **Package Management**: Packages are managed through the Unity Package Manager or by modifying the `Packages/manifest.json` file.

## Development Conventions

- **Layered Architecture**: The project enforces a strict layered architecture through assembly definitions. Dependencies flow from HighLevel to LowLevel.
- **Manager System**: Managers are implemented as singletons inheriting from the `BaseManager<T>` class.
- **State Management**: Game states are managed using a flow-based system with `BaseFlow` classes.
- **ECS**: A hybrid approach is used, combining MonoBehaviours for authoring and DOTS for runtime systems.
- **Resource Management**: A custom type-based resource loading system is implemented.
- **Physics**: The project uses a custom `LightweightPhysics2D` system.
- **Editor Code**: All editor-related code is kept in the `EditorLevel` assembly.

## Key Files

- **`GameManager.cs`**: The main entry point for the game.
- **`BaseManager.cs`**: The base class for all singleton managers.
- **`BaseFlow.cs`**: The base class for the state management system.
- **`ResourcesTypeRegistry.cs`**: The core of the resource loading system.
- **`LightweightPhysics2D.cs`**: The custom physics implementation.
- **`Assets/TS/InputSystem_Actions.inputactions`**: The input actions for the Unity Input System.

---

## 코드 예시 가이드라인 (For Gemini)

향후 모든 코드 예시를 제시할 때, 다음 규칙을 준수하여 가독성을 높이고 이해하기 쉽게 설명한다.

1.  **마크다운 코드 블록 사용**: 모든 코드 예시는 C# 구문 강조가 적용된 마크다운 코드 블록(```csharp)으로 감싼다.
2.  **전체 파일 컨텍스트 제공**: 단순한 코드 조각(snippet)보다는, 네임스페이스, 클래스 정의 등을 포함한 전체 파일의 맥락을 보여주는 완전한 코드 예시를 우선으로 한다.
3.  **명확한 주석 및 설명**: 코드의 핵심적인 부분이나 수정이 필요한 부분에 대해서는 코드 내에 주석을 추가하거나, 코드 블록 외부에 명확한 설명을 덧붙인다.
4.  **파일 경로 명시**: 여러 파일에 걸친 예시를 보여줄 경우, 각 코드 블록 상단에 어떤 파일에 해당하는 코드인지 파일 경로와 이름을 명확하게 밝힌다. (예: `// Assets/TS/Scripts/HighLevel/System/MySystem.cs`)
