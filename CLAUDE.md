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