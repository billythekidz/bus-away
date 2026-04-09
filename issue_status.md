## Current State of Procedural 3D Road Generation

### ✅ Achievements
1. **Full 3D Procedural Roads:** Transitioned from a 2D `LineRenderer` approach to a 3D geometry system. `DynamicRoad.cs` now properly instances 3D primitives (Cubes for roads, Cylinders for corners/joints).
2. **URP Lighting & Visuals:** Integrated standard Unity Universal Render Pipeline (URP/Lit) materials. Added a dynamic ground plane (`Environment_Ground`) at `Y = -0.1f` to catch real-time shadows.
3. **Isometric Casual Puzzle Camera:** Restructured `LevelCameraFramer.cs`. Shifted from Unity's 2D Orthographic mode to a **Perspective 3D Camera** mimicking the "casual puzzle" style found in Game Design references (e.g., lookAt centered, distance clamped, ~70° elevation, ~50° FOV).
4. **Data Refinement (`LevelDesignData`):** Roads are categorized via `RoadSegmentType` (MainRoad vs. ParkingBranch), eliminating unintended spawn points (e.g. gates at the bottom edge) via an updated Editor Generator.

### 🚧 Current Limitations & Issues
1. **Intersection Z-Fighting (Complex Grids):**
    - The current `DynamicRoad.cs` simply overlays blocks. If complex grids (like in `0.png`) create true cross-intersections (4-way paths overlapping exactly at the same Y-height), the engine will face Z-fighting on top faces.
    - *Fix Needed:* Either implement a specialized "Intersection Cube/Plane", offset intersecting paths slightly, or use a Boolean Mesh logic.
2. **Editor Generator Limitations (`LevelDesignDataEditor`):**
    - The `Generate Random Road Map` button currently only builds a simple "Ring Road + Offshoots". It does not generate complex inner labyrinths spanning a grid.
    - *Fix Needed:* A Grid-based cellular automata or maze generation algorithm needs to be written for sophisticated levels.
3. **Gameplay & Entities Pending:**
    - Buses are currently placeholder cubes (`PrimitiveType.Cube`), spawned based on the `.asset` data but lacking mobility scripts.
    - Passengers, queue sorting, and actual traffic flow logic are unimplemented.
    - *Fix Needed:* Implementation of `BusController.cs` and `PuzzleLogic.cs`.

### ⏭️ Next Steps
- Port the primitive Box/Cylinders over to custom Low-Poly FBX meshes if art direction pushes for high fidelity.
- Build the core traffic puzzle loop: matching `BusSpawnData.color` to `.passengerQueueOrder`.
