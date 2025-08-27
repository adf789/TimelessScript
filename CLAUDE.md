# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 2D game project using Unity 6000.2.0b12 with a sophisticated multi-layered architecture. The project combines traditional MonoBehaviour patterns with Unity DOTS (ECS) for performance-critical systems.

## Commands

### Unity Development
- Open project in Unity Editor (Unity 6000.2.0b12 required)
- Build through Unity Editor: File → Build Settings
- Package management through Unity Package Manager or modify `Packages/manifest.json`

### Key Dependencies
- UniTask for async/await: `https://github.com/Cysharp/UniTask.git`
- Unity Entities (ECS): 1.3.14
- Universal Render Pipeline: 17.2.0
- Unity Input System: 1.14.2

## Architecture

### Four-Layer Assembly Structure
The codebase uses a strict layered architecture with assembly definitions:

1. **LowLevel** (`Assets/TS/Scripts/LowLevel/`): Core data structures, enums, models, base components
2. **MiddleLevel** (`Assets/TS/Scripts/MiddleLevel/`): Business logic, physics, jobs, views, support systems  
3. **HighLevel** (`Assets/TS/Scripts/HighLevel/`): Game managers, flow control, controllers, ECS systems
4. **EditorLevel** (`Assets/TS/Scripts/EditorLevel/`): Editor-only tools, inspectors, workflow automation

### Key Architectural Patterns

**Manager System**: All managers inherit from `BaseManager<T>` singleton pattern
- `GameManager`: Main game entry point
- `FlowManager`: State management using BaseFlow pattern
- `UIManager`, `CameraManager`: Specialized system managers

**Flow-Based State Management**: `BaseFlow` classes manage game states (Intro, Home, Loading)

**ECS Integration**: Hybrid approach combining MonoBehaviour with DOTS
- Authoring components: `ConfigAuthoring`, `RotateSpeedAuthoring`, `SpawnAuthoring`
- Systems: `RotatingSystem` with Burst compilation
- Jobs: `RotateUpdateJob`, `SpawnJob` for parallel processing

### Custom Systems

**Resource Management**: Type-based loading system
- `ResourcesTypeRegistry`: Maps types to resource paths
- `ResourcesPath`: Attribute-based resource path mapping
- Supports automatic resource loading by type

**Physics**: Custom `LightweightPhysics2D` with specialized ground collision detection

**Animation**: `SpriteSheetAnimationSupport` for 2D sprite animations

**Input**: Comprehensive input system with action maps and mouse input processing

## Important Files

- `GameManager.cs`: Main game controller and entry point
- `BaseManager.cs`: Singleton manager base class used throughout
- `BaseFlow.cs`: State management foundation
- `ResourcesTypeRegistry.cs`: Core resource loading architecture
- `LightweightPhysics2D.cs`: Custom physics implementation

## Development Workflow

### Editor Tools
The project includes extensive custom editor tools:
- Resource management windows
- Script generation utilities
- Sprite sheet processing tools
- Pixelate workflow for 3D-to-2D conversion

### Asset Processing
- Automated sprite slicing and sheet generation
- 3D model to 2D sprite conversion using Pixelate asset
- URP-based 2D rendering with custom ToonLitSprite shader

### Performance Considerations
- ECS systems use Burst compilation for performance-critical code
- Job System implementation for parallel processing
- Addressables for efficient asset loading
- Custom physics system optimized for 2D gameplay

## Code Conventions

- Assembly definitions enforce layer separation - respect dependency direction
- Managers use generic singleton pattern `BaseManager<T>`
- ECS components follow Unity DOTS conventions with authoring/runtime separation
- Resource loading uses type-based registry pattern
- Editor code is strictly separated in EditorLevel assembly

## File Reference Format

For VSCode integrated terminal with Claude Code, use these settings and formats:

### VSCode Terminal Settings
Add to `settings.json` to prevent path wrapping and enable file links:
```json
{
  "terminal.integrated.enableFileLinks": true,
  "terminal.integrated.wordWrap": false,
  "terminal.integrated.scrollback": 10000,
  "terminal.integrated.fontSize": 12,
  "terminal.integrated.lineHeight": 1.2
}
```

### File Path Format (Clickable Links)
For long paths that wrap in terminal, use multiple format options:

**Option 1: Full Path** (if terminal is wide enough):
- `.\Assets\TS\Scripts\LowLevel\Data\ComponentData\Physics\LightweightPhysicsComponent.cs:12`

**Option 2: Shortened Names** (for narrow terminals):
- `.\Assets\TS\Scripts\LowLevel\Data\CompData\Physics\LightweightPhysicsComp.cs:12`

**Option 3: DOS 8.3 Format** (Windows short names):
- `.\ASSETS~1\TS\SCRIPT~1\LOWLEV~1\DATA\COMPDA~1\PHYSIC~1\LIGHTW~1.CS:12`

**Option 4: Copy-paste format**:
```
.\Assets\TS\Scripts\LowLevel\Data\ComponentData\Physics\LightweightPhysicsComponent.cs:12
```

### File Reference Structure
```
Physics System Files:
- .\Assets\TS\Scripts\HighLevel\System\Physics\PhysicsSystem.cs:22
- .\Assets\TS\Scripts\MiddleLevel\Job\Physics\PhysicsUpdateJob.cs:23  
- .\Assets\TS\Scripts\MiddleLevel\Job\Physics\PhysicsCollisionJob.cs:36
- .\Assets\TS\Scripts\LowLevel\Data\ComponentData\Physics\LightweightPhysicsComponent.cs:12
```

### Terminal Width Management
1. **Expand terminal panel**: Drag terminal panel height to maximum
2. **Horizontal scroll**: Use `Shift + Mouse Wheel` to scroll horizontally  
3. **Word wrap off**: Prevents automatic line breaking of file paths
4. **Ctrl+Click**: Click on any part of the file path to open

### Alternative: Copy-Paste Commands
For very long paths, provide both clickable link and command:
```
File: LightweightPhysicsComponent.cs:12
Path: .\Assets\TS\Scripts\LowLevel\Data\ComponentData\Physics\LightweightPhysicsComponent.cs:12  
Cmd:  code -g ".\Assets\TS\Scripts\LowLevel\Data\ComponentData\Physics\LightweightPhysicsComponent.cs:12"
```

Environment: Windows + VSCode integrated terminal + Claude Code CLI