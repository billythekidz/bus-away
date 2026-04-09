## Overview
Currently, the map generator (`GenerateRandomGrid`) in the Level Editor uses a "continuous closed loop" algorithm which simply creates a heavily distorted ring road. Because a ring path guarantees exactly 2 connection points per cell, the map topology is fatally limited to generating only `Straight` and `Corner` variations. 

This Epic tracks the implementation of a full-scale procedural city layout algorithm capable of producing an organic transit grid populated with **all** `RoadCellType` variations, including:
- 4-way Intersections (`Cross`)
- 3-way T-Junctions (`TJunction_[Direction]`)
- Dead Ends (`DeadEnd_[Direction]`)
- Inner Corners (`InnerCorner_[Direction]`)

## Proposed Algorithm: Recursive Binary Space Partitioning (BSP)
To organically mimic city blocks and Manhattan-like street grids, we will transition to a **Recursive Grid Division (BSP)** algorithm combined with systematic imperfection (hole punching).

### 1. BSP Road Carving
- Initialize a blank grid of dimensions `(width, height)`.
- Pick a random axis (horizontal or vertical) and a random coordinate slice. Fill that entire slice with `GenericRoad` tiles.
- This creates two isolated sub-blocks. 
- Recursively apply the slice logic to each sub-block until the block area/dimensions fall below a `Complexity`-weighted threshold.
- *Mathematical Consequence:* By allowing orthogonal lines to intersect completely across the grid, we inherently formulate `Cross` blocks. Where a recursively spawned road meets an existing boundary road, a `TJunction` is formed.

### 2. Topological Imperfection (Hole Punching)
- A perfect grid is boring. We will perform a randomized culling sweep to "erase" certain road segments.
- Erasing a connected segment of a `Cross` mathematically devolves it into a `TJunction`.
- Erasing a segment of a `Straight` road devolves its endpoints into `DeadEnd` cells.

### 3. Mask Resolution
- Rather than explicitly hard-coding logic to spawn `TJunction_N` vs `TJunction_S`, we leverage the existing Auto-Tiling state logic inside `UpdateAllRoadTypes()`.
- The Auto-Tiler uses an 8-way directional mask (N, E, S, W + Diagonals). Since the BSP array natively possesses real 3-way and 4-way geometrical connections, the current bitwise OR check `(mask == 15 -> Cross)` will automatically parse every generated block.
- Scatter generation for points of interest (`BusStop` and `GenericCrosswalk`) will remain limited to pure `Straight` segments (`mask == 5` or `10`) to prevent blocking logical turning paths.

## Checklist
- [ ] Refactor `GenerateRandomGrid()` in `LevelDesignDataEditor.cs` to use grid recursion.
- [ ] Introduce structural decimation (erasing points) for dead ends.
- [ ] Confirm successful generation of 19-state mesh variations without exceptions. 
- [ ] Keep point-of-interest assignment compatible with the new generator.
