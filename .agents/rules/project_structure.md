---
name: Feature-Based Architecture Rules
description: Enforces the strict Feature-Based Architecture layout for the Bus Away project, ensuring proper separation of concerns and build safety.
---

# Feature-Based Architecture Guidelines

All codebase changes and new implementations MUST strictly follow the Feature-Based Architecture outlined in the `README.md`.

## Core Folder Structure
- **`Assets/_Project/`**: Contains global application foundations.
  - `Scenes/`: Boot, MainMenu, GamePlay scenes.
  - `Settings/`: URP Assets, input configs, global system settings.
- **`Assets/_Features/`**: Contains ALL game mechanic modules.
  - Examples: `Core`, `UI`, `PuzzleLogic`, `BusMovement`, `LevelSystem`

## Anatomy of a Feature Folder
Every feature (e.g., `_Features/BusMovement/`) MUST contain the following standardized sub-directories. Do NOT place loose files directly in the feature root.
- **`/Runtime/`**: In-game logic scripts (`.cs` files) that run on the device.
- **`/Editor/`**: Custom editor tools, inspectors, and workflow scripts.
- **`/Data/`**: `ScriptableObjects` or local config data (e.g., specific level data).
- **`/Prefabs/`**: 3D models or GameObjects specific to this feature.
- **`/Art/`**: Materials, textures, and models that *only* belong to this feature.

## Critical Rules
1. **Self-Containment**: A feature should be as self-contained as possible without direct hard-referencing internal states of other features.
2. **Editor Code Isolation**: NEVER place an editor script (using `using UnityEditor;`) inside the `/Runtime/` folder. It will instantly crash standard Android/iOS builds. Always put them in `/Editor/`.
3. **Cross-Feature Communication**: Avoid deep nesting or tangling (e.g., `BusMovement` should not reach into `UI` internals). Use events, interfaces, or standard managers (`Core`) to communicate.
4. **No Asset Dumping**: Never leave scripts, prefabs, or materials loose in `Assets/`. Categorize them immediately into their respective feature folders.
5. **Data-Driven**: Use the `/Data/` folder within a feature for `ScriptableObjects` to allow behavior tweaking without touching code.
