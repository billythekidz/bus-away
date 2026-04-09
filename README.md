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

## 🗂️ Project Structure

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/           # Game logic, grid, level management
│   │   ├── Gameplay/       # Bus, stop, input controllers
│   │   ├── UI/             # HUD, menus, transitions
│   │   └── Utils/          # Helpers, extensions
│   ├── Prefabs/            # Bus, stop, grid cell prefabs
│   ├── Scenes/             # Main menu, Level 1, Level 2
│   ├── Art/                # Sprites, materials, VFX
│   └── Audio/              # SFX, music
├── Plugins/
└── StreamingAssets/
    └── Levels/             # JSON level definitions
```

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
