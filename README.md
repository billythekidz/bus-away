# 🚌 Bus Away — Unity Puzzle Prototype

A polished, playable puzzle prototype inspired by [Buses Away](https://apps.apple.com/vn/app/buses-away/id6755150293), built as a Unity Developer Test Assignment for 9AM.

---

## 🎮 Game Overview

**Bus Away** is a logic-based puzzle game where players direct buses of matching colors to their correct stops by sliding them through a grid of intersecting lanes. The challenge lies in resolving traffic jams without creating new ones — every move matters.

---

## 🧩 Core Mechanics

- **Slide-to-move** touch controls: drag buses horizontally or vertically along their lanes
- **Color matching**: each bus must reach a stop of the same color
- **Traffic resolution**: buses block each other — order and sequencing are key
- **Win condition**: all buses successfully parked at their matching stops

---

## 🗂️ Project Structure (Feature-Based Architecture)

We strictly use a **Feature-Based (Modular) Architecture** to ensure the project scales cleanly without creating "spaghetti code" or massive unwieldy folders.

```text
Assets/
  ├── _Project/               <-- Global application foundation
  │      ├── Scenes/             # Boot, MainMenu, GamePlay scenes
  │      └── Settings/           # URP Assets, input config, global system settings
  │
  └── _Features/              <-- Isolated, self-contained game modules
         ├── Core/               # Game state, save/load, core input systems
         ├── UI/                 # Canvases, popups, screen transitions
         ├── PuzzleLogic/        # Win/lose conditions, queueing rules, validation
         ├── BusMovement/        # Vehicle physics, steering, path following
         └── LevelSystem/        # Procedural road generation, map data (ScriptableObjects)
```

### 📂 Anatomy of a Feature Folder

Every feature (e.g., `_Features/BusMovement/`) should strictly contain the following standardized sub-directories:

- **`/Runtime/`**: In-game logic scripts (`.cs` files) that run on the device.
- **`/Editor/`**: Custom editor tools, inspectors, and workflow scripts. **Crucial:** Prevents APK build failures.
- **`/Data/`**: `ScriptableObjects` or local config data (e.g., specific level data).
- **`/Prefabs/`**: 3D models or GameObjects specific to this feature.
- **`/Art/`**: Materials, textures, and models that *only* belong to this feature.

### 📜 Development Rules & Usage

1. **Self-Containment**: A feature should be as self-contained as possible. You should theoretically be able to drag the `LevelSystem` folder into a completely new Unity project, and it would retain its core functionality.
2. **Global Systems go to `_Project`**: If an element is needed across *multiple* unrelated features (like a global UI font or the Main Scene), place it in `_Project/` instead of a specific feature folder.
3. **Data-Driven**: Use the `/Data/` folder within a feature for `ScriptableObjects` to allow game designers to tweak behavior without touching code.

### 🚫 Mistakes to Avoid

- ❌ **Mixing Editor and Runtime code**: NEVER place an editor script (using `using UnityEditor;`) inside the `/Runtime/` folder. It will instantly crash standard Android/iOS builds. Always put them in `/Editor/`.
- ❌ **Cross-Feature Tangling**: Avoid having `BusMovement` directly reach deeply into the internal states of `UI`. Use events, interfaces, or standard managers (`Core`) to communicate.
- ❌ **Dumping Assets in the Root Directory**: Never leave scripts, prefabs, or materials loose in `Assets/`. Always categorize them immediately into their respective feature folders.

---

## 🤖 AI Usage

As per the assignment guidelines, AI was used for **≥ 50%** of the development process:

| Task | AI Tool Used |
|---|---|
| Architecture planning & system design | Claude (Antigravity / Gemini) |
| Core grid & bus movement logic | Claude (code generation + review) |
| Level data format design (JSON schema) | Claude |
| UI layout structure (UXML / Canvas) | Claude |
| Particle & animation setup guidance | Claude |
| README writing | Claude |

All AI-generated code was reviewed, tested, and integrated manually by the developer.

---

## 🛠️ Technical Details

- **Engine**: Unity 6 (6000.x LTS)
- **Platform**: Android (APK)
- **Target API**: Android 12+ (API 31+)
- **Orientation**: Portrait
- **Input**: Touch (Unity Input System)
- **Architecture**: Component-based, ScriptableObject-driven level data

---

## 🚀 Build Instructions

1. Open the project in **Unity 6**
2. Switch platform to **Android** via `File → Build Settings`
3. Set the Keystore (or use development build)
4. Click **Build** → select output folder

The latest APK is available in the **Releases** section of this repository.

---

## 🎬 Gameplay Demo

> _Video link will be added here upon submission_

---

## 📋 Assignment Checklist

- [x] Core puzzle mechanic implemented
- [x] Touch input (drag to slide)
- [x] Win condition & level flow
- [x] 1–2 playable levels (≥ 30s gameplay each)
- [x] Visual feedback (animations, particles)
- [x] Start / Restart flow
- [x] Minimal HUD
- [x] Clean, readable code structure
- [x] Private GitHub repository
- [ ] APK build
- [ ] Gameplay video

---

## 👤 Author

Built with ❤️ and AI assistance for the **9AM Unity Developer Test Assignment**.
