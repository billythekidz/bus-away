# Bus Away - Project Timeline & Evolution

This is a comprehensive chronological timeline that synchronizes the **Commit** history with the list of **GitHub Issues** for the "Bus Away" project. The development process is divided into phases corresponding to architectural and feature milestones.

---

## Phase 1: Project Setup, Architecture & Foundation (April 09, 2026)
*Laying the groundwork for data architecture and core grid generation capabilities.*

### Step 1: Initialization and Project Setup
- The project began with initial repository commits.
- Set up a new `README.md` to replace the GitHub default, and updated VSCode Workspace Settings.
- **Issue #1 (Closed):** Fixed URP MSAA 'Missing resolve surface' log spam in the Editor caused by `CharacterCustomizationWindow`.
- **Issue #2 (Closed):** Fixed lighting in the `Cartoon City` scene to match the original asset reference.

### Step 2: Data-Driven Architecture & AI Tooling Support
- **Issue #4 (Closed):** Built a **Data-driven Level Generation System**. Established an architecture to store level data using ScriptableObjects, allowing for future scalability.
- Implemented automatic script state saving, project rules setup, and MCP-skill to facilitate seamless collaboration with AI assistants.

### Step 3: Road Generation (3D Procedural Roads)
- **Issue #3 (Closed):** Launched the **Dynamic Road Generator**.
- Resolved numerous geometry and grid connection issues:
  - Fixed Pivot rotation, Scale misalignment, and T-junction linking.
  - Completely rebuilt the meshes for Straight, Cross, Corner, and T-Junction tiles using **ProBuilder** to achieve absolute precision and clear out geometry artifacts.
  - Used polygon arcs to round the outer edge of the Corner tile, and refined the random grid algorithm to ensure it always outputs closed rectangular loops.
  - Added a "Hollow" prefab for bus stops (`Tile_BusStop`) to prevent clipping with Buses.
  - Fixed map editor grid orientation offset to align perfectly with the game view.

### Step 4: Early Crowd AI System (High-Performance)
- **Epic #5, Tasks #6, #7, #8, #9 (Closed):** Developed a High-Performance Crowd Simulation System powered by **Job System, Burst, Collections, and Mathematics**.
- Setup `NativeArrays` and `IJobParallelFor` to handle Boids separation collisions, and utilized `RenderMeshInstanced` for high-performance mass rendering.

---

## Phase 2: Advancing Level Generation & Crowd Lands (April 10, 2026)
*Transitioning the map generation algorithm from formal squares to organic paths, while implementing passenger waiting areas (Crowd Lands).*

### Step 1: Upgrading Generator - BSP & Organic Growth
- **Epic #10 & Tasks #11-#14 (Open):** Planned to transition the Map Generator to a **BSP (Binary Space Partitioning)** slicing solution combined with **Grid Decimation** to create complex but closed-loop branch roads.
- **Issue #22 (Closed):** By the end of the day, completely replaced the map spawning logic with a more advanced algorithm: **Organic Level Generator (L-System Growth)**, producing highly natural, curvy roads.

### Step 2: Debugging and Polishing Crowd System
- **Issue #15 (Closed):** Fully resolved 7 bugs in the Crowd System identified after the review session.
- Fixed overlapping tile mesh movements and added "Half T" meshes to handle dead-end connection points at intersections.

### Step 3: Implementing Crowd Land System
- **Epic #16, Tasks #17-#19 (Closed/Open):** Developed the **Crowd Land System**.
  - Initialized Data Model & Editor UI, allowing customization of colors and the number of waiting passenger groups via a visual Color Palette.
  - Integrated `CrowdManager.SpawnLand()` to automatically trigger based on the level generation system.

### Step 4: Bus Movement Logic (Bus V2) & Corner Notches
- **Issue #20 (Closed):** Rolled out the `BusController.cs` script version V2.
- **Issue #21 (Closed):** Refactored branch road detection via algorithms (S-Curve & Combo-5 Corners).

### Step 5: Game Manager Implementation
- Implemented `GameManager.cs` to begin hooking Level, Bus, and Crowd data together into a cohesive, smooth loop experience.

---

## Phase 3: Finalizing Game Loop, Polish & SFX (April 11, 2026)
*Assembling everything into a complete Game Loop, adding touch interactions, animations, audio, and VFX to transform the project into a fully playable game.*

### Step 1: Level Design & Game Logic Loop
- Fully configured the `Level 1 SO` (Scriptable Object).
- Rendered `TextMeshPro` UI to display waiting capacity and bus counting at stops.
- Bus logic is now capable of following the main Road Loop, identifying targets, stopping, and picking up passengers.

### Step 2: Improving Crowd Agents
- Smoothed Crowd Agents by fixing visual collision bugs and added a beautifully minimalist "simple human" model built using ProBuilder.

### Step 3: User Interaction & Polishing
- Finalized the Manager workflow.
- Integrated **Haptic Feedback** (vibration) for screen interactions.
- Added Visual Effects (VFX) such as sparks/flashes when a bus moves or boards passengers.
- Updated in-game statistics displays (Game Label/Text).

### Step 4: Audio & Victory Logic
- Initialized Audio sources and ran a Python script to automatically generate 7 unique **SFX (Sound Effects)** (braking, winning, coins, hopping onto seats, etc.).
- Perfected `GameManager` functions for Play and Game Over (timeout).
- UX Treatment: Buses automatically avoid duplicate colors at loading zones.
- Game Mode Tweak: Upon reaching the win condition (100% capacity), buses continue a natural "Victory Drive" parade towards the finish line instead of freezing instantly, leaving a satisfying lasting impression.

---

**→ Summary:** "Bus Away" has evolved through a rapid development pipeline from a Core Grid System => Dynamic Asset Generation => Highly Optimized Systems (Burst/Jobs) => Reaching a Complete Gameplay Loop (UI, Audio, VFX, Haptics).
