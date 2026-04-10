using System.Collections.Generic;
using BusAway.Level;
using UnityEditor;
using UnityEngine;

namespace BusAway.LevelEditor
{
    [CustomEditor(typeof(LevelDesignData))]
    public class LevelDesignDataEditor : Editor
    {
        private int randomComplexity = 3;

        private bool IsRoadOrBus(RoadCellType type)
        {
            return type != RoadCellType.Empty;
        }

        public override void OnInspectorGUI()
        {
            LevelDesignData data = (LevelDesignData)target;

            GUILayout.Label("Map Level Configuration", EditorStyles.boldLabel);
            data.gridWidth = EditorGUILayout.IntSlider("Grid Width", data.gridWidth, 4, 30);
            data.gridHeight = EditorGUILayout.IntSlider("Grid Height", data.gridHeight, 4, 30);
            data.busStopLength = Mathf.Max(1, EditorGUILayout.IntField("Bus Stop Length", data.busStopLength));

            if (data.grid == null || data.grid.Length != data.gridWidth * data.gridHeight)
            {
                if (GUILayout.Button("Initialize / Reset Grid", GUILayout.Height(30)))
                {
                    data.grid = new RoadCellType[data.gridWidth * data.gridHeight];
                    EditorUtility.SetDirty(data);
                }
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.Label("Map Editor (Click to toggle)", EditorStyles.boldLabel);

                for (int y = data.gridHeight - 1; y >= 0; y--) // y=max at top = screen top
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    for (int x = 0; x < data.gridWidth; x++)
                    {
                        int index = y * data.gridWidth + x;
                        RoadCellType cell = data.grid[index];

                        string label = ".";
                        Color btnColor = Color.white;

                        bool isBusStopBranch = cell >= RoadCellType.DeadEnd_N && cell <= RoadCellType.DeadEnd_W;

                        if (isBusStopBranch)
                        {
                            label = GetCellLabel(cell); // Will be "B"
                            btnColor = new Color(0.2f, 0.6f, 1.0f);
                        }
                        else if (cell != RoadCellType.Empty)
                        {
                            label = GetCellLabel(cell);
                            btnColor = new Color(0.3f, 0.3f, 0.3f);
                        }

                        GUI.backgroundColor = btnColor;
                        if (GUILayout.Button(label, GUILayout.Width(25), GUILayout.Height(25)))
                        {
                            if (cell == RoadCellType.Empty)
                                data.grid[index] = RoadCellType.GenericRoad;
                            else
                                data.grid[index] = RoadCellType.Empty;

                            UpdateAllRoadTypes(data);
                            EditorUtility.SetDirty(data);
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(20);
                GUILayout.Label("Random Generator", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                randomComplexity = EditorGUILayout.IntSlider("Complexity", randomComplexity, 1, 5);
                if (GUILayout.Button("Generate Random Grid", GUILayout.Height(20)))
                {
                    GenerateRandomGrid(data, randomComplexity);
                    UpdateAllRoadTypes(data);
                    EditorUtility.SetDirty(data);
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(15);
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("── Crowd Lands ──", EditorStyles.boldLabel);

            // Min/Max land count with validation
            EditorGUI.BeginChangeCheck();
            int newMin = EditorGUILayout.IntSlider("Min Land Count", data.minLandCount, 2, 5);
            int newMax = EditorGUILayout.IntSlider("Max Land Count", data.maxLandCount, 2, 5);
            if (newMin > newMax) newMin = newMax;
            data.minLandCount = newMin;
            data.maxLandCount = newMax;

            // Min/Max agents per land (multiple of 4 enforced)
            int rawMinAgents = EditorGUILayout.IntField("Min Agents Per Land", data.minAgentsPerLand);
            int rawMaxAgents = EditorGUILayout.IntField("Max Agents Per Land", data.maxAgentsPerLand);
            data.minAgentsPerLand = Mathf.Max(4, (rawMinAgents / 4) * 4); // clamp to multiple of 4
            data.maxAgentsPerLand = Mathf.Max(data.minAgentsPerLand, (rawMaxAgents / 4) * 4);

            EditorGUILayout.HelpBox(
                $"Rows per land: {data.minAgentsPerLand/4}–{data.maxAgentsPerLand/4} " +
                $"({data.minAgentsPerLand}–{data.maxAgentsPerLand} agents)",
                MessageType.Info);

            // Color palette with visual swatches
            EditorGUILayout.LabelField("Land Color Palette", EditorStyles.boldLabel);
            if (data.landColorPalette == null) data.landColorPalette = new List<Color>();

            // Show warning if palette < maxLandCount
            if (data.landColorPalette.Count < data.maxLandCount)
            {
                EditorGUILayout.HelpBox(
                    $"Palette has {data.landColorPalette.Count} colors but maxLandCount={data.maxLandCount}. Add more colors.",
                    MessageType.Warning);
            }

            // Draw color swatches inline
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < data.landColorPalette.Count; i++)
            {
                data.landColorPalette[i] = EditorGUILayout.ColorField(GUIContent.none, data.landColorPalette[i], false, true, false, GUILayout.Width(40));
            }
            if (GUILayout.Button("+", GUILayout.Width(24))) data.landColorPalette.Add(Color.white);
            if (data.landColorPalette.Count > 0 && GUILayout.Button("-", GUILayout.Width(24)))
                data.landColorPalette.RemoveAt(data.landColorPalette.Count - 1);
            EditorGUILayout.EndHorizontal();

            if (data.resolvedLands != null && data.resolvedLands.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview: Resolved Lands (last build)", EditorStyles.miniLabel);
                int total = 0;
                foreach (var land in data.resolvedLands)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ColorField(GUIContent.none, land.color, false, false, false, GUILayout.Width(40));
                    EditorGUILayout.LabelField($"{land.agentCount} agents ({land.agentCount/4} rows)");
                    total += land.agentCount;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.LabelField($"Total: {total} agents", EditorStyles.boldLabel);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
        }

        private void UpdateAllRoadTypes(LevelDesignData data)
        {
            System.Func<int, int, bool> HasRoad = (cx, cy) => {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                RoadCellType t = data.grid[cy * data.gridWidth + cx];
                // Treat anything that isn't Empty as a road connection, 
                // EXCEPT DeadEnds (parking slots) so adjacent roads don't falsely bend toward them.
                if (t == RoadCellType.Empty) return false;
                if (t >= RoadCellType.DeadEnd_N && t <= RoadCellType.DeadEnd_W) return false;
                return true;
            };

            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    int index = y * data.gridWidth + x;
                    RoadCellType current = data.grid[index];
                    
                    // Do not Auto-Tile cells that have been finalized as Bus Stops by Post Processing
                    if (current == RoadCellType.Empty || 
                        (current >= RoadCellType.HalfT_BusStop_N_Left && current <= RoadCellType.HalfT_BusStop_W_Right) ||
                        (current >= RoadCellType.DeadEnd_N && current <= RoadCellType.DeadEnd_W)) 
                    {
                        continue;
                    }

                    bool n = HasRoad(x, y + 1);
                    bool e = HasRoad(x + 1, y);
                    bool s = HasRoad(x, y - 1);
                    bool w = HasRoad(x - 1, y);

                    int mask = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);

                    RoadCellType newType = current;

                    switch (mask)
                    {
                        case 5:  newType = RoadCellType.Straight_NS; break;
                        case 10: newType = RoadCellType.Straight_EW; break;
                        case 3:  newType = RoadCellType.Corner_NE; break;
                        case 6:  newType = RoadCellType.Corner_SE; break;
                        case 12: newType = RoadCellType.Corner_SW; break;
                        case 9:  newType = RoadCellType.Corner_NW; break;
                    }

                    if (mask == 5 || mask == 10 || mask == 3 || mask == 6 || mask == 12 || mask == 9)
                    {
                        data.grid[index] = newType;
                    }
                }
            }
        }

        private bool IsIsolatedBusStopCells(LevelDesignData data, int bx1, int by1, int bx2, int by2, int parentX1, int parentY1, int parentX2, int parentY2)
        {
            var cellsToCheck = new (int cx, int cy)[] {
                (bx1, by1 + 1), (bx1, by1 - 1), (bx1 + 1, by1), (bx1 - 1, by1),
                (bx2, by2 + 1), (bx2, by2 - 1), (bx2 + 1, by2), (bx2 - 1, by2)
            };

            foreach (var (cx, cy) in cellsToCheck)
            {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) continue;
                // Ignore the bus stop structures themselves
                if ((cx == bx1 && cy == by1) || (cx == bx2 && cy == by2) || 
                    (cx == parentX1 && cy == parentY1) || (cx == parentX2 && cy == parentY2)) continue;

                if (data.GetCell(cx, cy) != RoadCellType.Empty) return false;
            }

            return true;
        }

        private bool IsProtectedBoardingArea(int x, int y, int gridWidth)
        {
            if (y != 0) return false;
            int cx = gridWidth / 2;
            if (gridWidth % 2 == 0) return x == cx - 1 || x == cx;
            else return x >= cx - 1 && x <= cx + 1;
        }

        private void GenerateRandomGrid(LevelDesignData data, int complexity)
        {
            if (data.gridWidth < 4) data.gridWidth = 4;
            if (data.gridHeight < 4) data.gridHeight = 4;

            int maxAttempts = 100;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Clear entire grid first
                for (int i = 0; i < data.grid.Length; i++)
                    data.grid[i] = RoadCellType.Empty;

                // Start with a small base loop at the bottom (y=0) to guarantee a boarding area
                int minX, maxX;
                if (data.gridWidth % 2 == 0)
                {
                    minX = data.gridWidth / 2 - 2;
                    maxX = data.gridWidth / 2 + 1;
                }
                else
                {
                    minX = data.gridWidth / 2 - 2;
                    maxX = data.gridWidth / 2 + 2;
                }

                // Ensure bounds are safe (just in case grid is very small)
                if (minX < 0) minX = 0;
                if (maxX >= data.gridWidth) maxX = data.gridWidth - 1;

                int minY = 0;
                int maxY = 2; // small height to allow upwards expansion
                if (maxY >= data.gridHeight) maxY = data.gridHeight - 1;

                for (int x = minX; x <= maxX; x++)
                {
                    data.SetCell(x, minY, RoadCellType.GenericRoad);
                    data.SetCell(x, maxY, RoadCellType.GenericRoad);
                }
                for (int y = minY; y <= maxY; y++)
                {
                    data.SetCell(minX, y, RoadCellType.GenericRoad);
                    data.SetCell(maxX, y, RoadCellType.GenericRoad);
                }

                // Scatter corner bends iteratively
                if (complexity >= 1)
                {
                    AddCornerBendsOrganic(data, complexity);
                }

                // Place bus stops
                if (PlaceBusStopsOrganic(data))
                {
                    break;
                }
            }
        }

        private bool PlaceBusStopsOrganic(LevelDesignData data)
        {
            UpdateAllRoadTypes(data);

            var candidates = new System.Collections.Generic.List<System.Action>();

            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    if (IsProtectedBoardingArea(x, y, data.gridWidth)) continue;

                    // North Bus Stop (requires 2 Straight_EW, and 2 Empty above)
                    if (x + 1 < data.gridWidth && data.GetCell(x, y) == RoadCellType.Straight_EW && data.GetCell(x + 1, y) == RoadCellType.Straight_EW)
                    {
                        if (y + 1 < data.gridHeight && data.GetCell(x, y + 1) == RoadCellType.Empty && data.GetCell(x + 1, y + 1) == RoadCellType.Empty)
                        {
                            int cx = x, cy = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx, cy + 1) != RoadCellType.Empty || data.GetCell(cx + 1, cy + 1) != RoadCellType.Empty) return;
                                if (data.GetCell(cx, cy) != RoadCellType.Straight_EW || data.GetCell(cx + 1, cy) != RoadCellType.Straight_EW) return;
                                data.SetCell(cx, cy, RoadCellType.HalfT_BusStop_N_Left);
                                data.SetCell(cx + 1, cy, RoadCellType.HalfT_BusStop_N_Right);
                                data.SetCell(cx, cy + 1, RoadCellType.DeadEnd_N);
                                data.SetCell(cx + 1, cy + 1, RoadCellType.DeadEnd_N);
                            });
                        }
                    }

                    // South Bus Stop (requires 2 Straight_EW, and 2 Empty below)
                    if (x + 1 < data.gridWidth && data.GetCell(x, y) == RoadCellType.Straight_EW && data.GetCell(x + 1, y) == RoadCellType.Straight_EW)
                    {
                        if (y - 1 >= 0 && data.GetCell(x, y - 1) == RoadCellType.Empty && data.GetCell(x + 1, y - 1) == RoadCellType.Empty)
                        {
                            int cx = x, cy = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx, cy - 1) != RoadCellType.Empty || data.GetCell(cx + 1, cy - 1) != RoadCellType.Empty) return;
                                if (data.GetCell(cx, cy) != RoadCellType.Straight_EW || data.GetCell(cx + 1, cy) != RoadCellType.Straight_EW) return;
                                data.SetCell(cx, cy, RoadCellType.HalfT_BusStop_S_Right);
                                data.SetCell(cx + 1, cy, RoadCellType.HalfT_BusStop_S_Left);
                                data.SetCell(cx, cy - 1, RoadCellType.DeadEnd_S);
                                data.SetCell(cx + 1, cy - 1, RoadCellType.DeadEnd_S);
                            });
                        }
                    }

                    // East Bus Stop (requires 2 Straight_NS, and 2 Empty right)
                    if (y + 1 < data.gridHeight && data.GetCell(x, y) == RoadCellType.Straight_NS && data.GetCell(x, y + 1) == RoadCellType.Straight_NS)
                    {
                        if (x + 1 < data.gridWidth && data.GetCell(x + 1, y) == RoadCellType.Empty && data.GetCell(x + 1, y + 1) == RoadCellType.Empty)
                        {
                            int cx = x, cy = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx + 1, cy) != RoadCellType.Empty || data.GetCell(cx + 1, cy + 1) != RoadCellType.Empty) return;
                                if (data.GetCell(cx, cy) != RoadCellType.Straight_NS || data.GetCell(cx, cy + 1) != RoadCellType.Straight_NS) return;
                                data.SetCell(cx, cy, RoadCellType.HalfT_BusStop_E_Right);
                                data.SetCell(cx, cy + 1, RoadCellType.HalfT_BusStop_E_Left);
                                data.SetCell(cx + 1, cy, RoadCellType.DeadEnd_E);
                                data.SetCell(cx + 1, cy + 1, RoadCellType.DeadEnd_E);
                            });
                        }
                    }

                    // West Bus Stop (requires 2 Straight_NS, and 2 Empty left)
                    if (y + 1 < data.gridHeight && data.GetCell(x, y) == RoadCellType.Straight_NS && data.GetCell(x, y + 1) == RoadCellType.Straight_NS)
                    {
                        if (x - 1 >= 0 && data.GetCell(x - 1, y) == RoadCellType.Empty && data.GetCell(x - 1, y + 1) == RoadCellType.Empty)
                        {
                            int cx = x, cy = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx - 1, cy) != RoadCellType.Empty || data.GetCell(cx - 1, cy + 1) != RoadCellType.Empty) return;
                                if (data.GetCell(cx, cy) != RoadCellType.Straight_NS || data.GetCell(cx, cy + 1) != RoadCellType.Straight_NS) return;
                                data.SetCell(cx, cy, RoadCellType.HalfT_BusStop_W_Left);
                                data.SetCell(cx, cy + 1, RoadCellType.HalfT_BusStop_W_Right);
                                data.SetCell(cx - 1, cy, RoadCellType.DeadEnd_W);
                                data.SetCell(cx - 1, cy + 1, RoadCellType.DeadEnd_W);
                            });
                        }
                    }
                }
            }

            // Shuffle candidates
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
            }

            int placed = 0;
            foreach (var act in candidates)
            {
                if (placed >= data.busStopLength) break;
                
                int priorEmpty = 0;
                for (int i=0; i<data.gridWidth * data.gridHeight; i++) if (data.grid[i] == RoadCellType.Empty) priorEmpty++;

                act.Invoke();

                int afterEmpty = 0;
                for (int i=0; i<data.gridWidth * data.gridHeight; i++) if (data.grid[i] == RoadCellType.Empty) afterEmpty++;

                if (afterEmpty < priorEmpty) placed++;
            }

            return placed >= data.busStopLength;
        }

        private void AddCornerBendsOrganic(LevelDesignData data, int complexity)
        {
            int bendsNeeded = complexity * 4; // Higher multiplier to create snake-like paths
            int maxNotchWidth = complexity <= 1 ? 1 : complexity <= 3 ? 2 : 3;

            for (int iter = 0; iter < 100 && bendsNeeded > 0; iter++)
            {
                var candidates = new System.Collections.Generic.List<System.Func<bool>>();

                for (int y = 0; y < data.gridHeight - 1; y++)
                {
                    for (int x = 1; x < data.gridWidth - 1; x++)
                    {
                        if (data.GetCell(x, y) != RoadCellType.GenericRoad) continue;
                        if (IsProtectedBoardingArea(x, y, data.gridWidth)) continue;

                        bool horiz = data.GetCell(x - 1, y) == RoadCellType.GenericRoad && data.GetCell(x + 1, y) == RoadCellType.GenericRoad
                                     && data.GetCell(x, y - 1) == RoadCellType.Empty && data.GetCell(x, y + 1) == RoadCellType.Empty;
                        bool vert  = data.GetCell(x, y - 1) == RoadCellType.GenericRoad && data.GetCell(x, y + 1) == RoadCellType.GenericRoad
                                     && data.GetCell(x - 1, y) == RoadCellType.Empty && data.GetCell(x + 1, y) == RoadCellType.Empty;

                        if (horiz)
                        {
                            int cx = x, cy = y;
                            if (complexity >= 2)
                            {
                                candidates.Add(() => TryCarveScurve(data, cx, cy, true, 1, Random.value > 0.5f));
                                candidates.Add(() => TryCarveScurve(data, cx, cy, true, -1, Random.value > 0.5f));
                            }
                            candidates.Add(() => TryCarveNotch(data, cx, cy, true, 1, Random.Range(1, maxNotchWidth + 1)));
                            candidates.Add(() => TryCarveNotch(data, cx, cy, true, -1, Random.Range(1, maxNotchWidth + 1)));
                        }
                        if (vert)
                        {
                            int cx = x, cy = y;
                            if (complexity >= 2)
                            {
                                candidates.Add(() => TryCarveScurve(data, cx, cy, false, 1, Random.value > 0.5f));
                                candidates.Add(() => TryCarveScurve(data, cx, cy, false, -1, Random.value > 0.5f));
                            }
                            candidates.Add(() => TryCarveNotch(data, cx, cy, false, 1, Random.Range(1, maxNotchWidth + 1)));
                            candidates.Add(() => TryCarveNotch(data, cx, cy, false, -1, Random.Range(1, maxNotchWidth + 1)));
                        }
                    }
                }

                if (candidates.Count == 0) break;

                // Shuffle
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
                }

                bool placed = false;
                foreach (var act in candidates)
                {
                    if (act.Invoke())
                    {
                        placed = true;
                        break; // Only apply one mutation per iter to prevent collision
                    }
                }
                if (placed) bendsNeeded--;
                else break;
            }
        }

        private bool IsPerfectLoop(LevelDesignData data)
        {
            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    if (data.GetCell(x, y) == RoadCellType.Empty) continue;
                    
                    int neighbors = 0;
                    if (x > 0 && data.GetCell(x - 1, y) != RoadCellType.Empty) neighbors++;
                    if (x < data.gridWidth - 1 && data.GetCell(x + 1, y) != RoadCellType.Empty) neighbors++;
                    if (y > 0 && data.GetCell(x, y - 1) != RoadCellType.Empty) neighbors++;
                    if (y < data.gridHeight - 1 && data.GetCell(x, y + 1) != RoadCellType.Empty) neighbors++;
                    
                    if (neighbors != 2) return false;
                }
            }
            return true;
        }

        private bool TryCarveNotch(LevelDesignData data, int ex, int ey, bool horizontal, int outDir, int width)
        {
            var edgeCells = new System.Collections.Generic.List<(int, int)>();
            var notchCells = new System.Collections.Generic.List<(int, int)>();

            int halfL = width / 2;
            int halfR = (width - 1) / 2;

            if (horizontal)
            {
                for (int dx = -halfL; dx <= halfR; dx++) edgeCells.Add((ex + dx, ey));
                int ny = ey + outDir;
                for (int dx = -halfL - 1; dx <= halfR + 1; dx++) notchCells.Add((ex + dx, ny));
            }
            else
            {
                for (int dy = -halfL; dy <= halfR; dy++) edgeCells.Add((ex, ey + dy));
                int nx = ex + outDir;
                for (int dy = -halfL - 1; dy <= halfR + 1; dy++) notchCells.Add((nx, ey + dy));
            }

            foreach (var (cx, cy) in edgeCells)
            {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                if (IsProtectedBoardingArea(cx, cy, data.gridWidth)) return false;
                if (data.GetCell(cx, cy) != RoadCellType.GenericRoad) return false;
                if (horizontal && (data.GetCell(cx, cy - 1) != RoadCellType.Empty || data.GetCell(cx, cy + 1) != RoadCellType.Empty)) return false;
                if (!horizontal && (data.GetCell(cx - 1, cy) != RoadCellType.Empty || data.GetCell(cx + 1, cy) != RoadCellType.Empty)) return false;
            }
            foreach (var (cx, cy) in notchCells)
            {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                if (data.GetCell(cx, cy) != RoadCellType.Empty) return false;
            }

            // Apply tentatively
            foreach (var (cx, cy) in edgeCells) data.SetCell(cx, cy, RoadCellType.Empty);
            foreach (var (cx, cy) in notchCells) data.SetCell(cx, cy, RoadCellType.GenericRoad);

            if (data.enforcePerfectLoop && !IsPerfectLoop(data))
            {
                // Revert
                foreach (var (cx, cy) in notchCells) data.SetCell(cx, cy, RoadCellType.Empty);
                foreach (var (cx, cy) in edgeCells) data.SetCell(cx, cy, RoadCellType.GenericRoad);
                return false;
            }

            return true;
        }

        private bool TryCarveScurve(LevelDesignData data, int ex, int ey, bool horizontal, int outDir, bool mirror)
        {
            int sDir = mirror ? -1 : 1;

            var removeCells = new System.Collections.Generic.List<(int, int)>();
            var newCells = new System.Collections.Generic.List<(int, int)>();

            removeCells.Add((ex, ey));

            if (horizontal)
            {
                int r1 = ey + outDir;
                int r2 = ey + outDir * 2;

                newCells.Add((ex - sDir, r1));
                newCells.Add((ex, r1));
                newCells.Add((ex, r2));
                newCells.Add((ex + sDir, r2));
                newCells.Add((ex + sDir, r1));
                removeCells.Add((ex + sDir, ey));
            }
            else
            {
                int c1 = ex + outDir;
                int c2 = ex + outDir * 2;

                newCells.Add((c1, ey - sDir));
                newCells.Add((c1, ey));
                newCells.Add((c2, ey));
                newCells.Add((c2, ey + sDir));
                newCells.Add((c1, ey + sDir));
                removeCells.Add((ex, ey + sDir));
            }

            foreach (var (cx, cy) in removeCells)
            {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                if (IsProtectedBoardingArea(cx, cy, data.gridWidth)) return false;
                if (data.GetCell(cx, cy) != RoadCellType.GenericRoad) return false;
                if (horizontal && (data.GetCell(cx, cy - 1) != RoadCellType.Empty || data.GetCell(cx, cy + 1) != RoadCellType.Empty)) return false;
                if (!horizontal && (data.GetCell(cx - 1, cy) != RoadCellType.Empty || data.GetCell(cx + 1, cy) != RoadCellType.Empty)) return false;
            }
            foreach (var (cx, cy) in newCells)
            {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                if (data.GetCell(cx, cy) != RoadCellType.Empty) return false;
            }

            // Apply tentatively
            foreach (var (cx, cy) in removeCells) data.SetCell(cx, cy, RoadCellType.Empty);
            foreach (var (cx, cy) in newCells) data.SetCell(cx, cy, RoadCellType.GenericRoad);

            if (data.enforcePerfectLoop && !IsPerfectLoop(data))
            {
                // Revert
                foreach (var (cx, cy) in newCells) data.SetCell(cx, cy, RoadCellType.Empty);
                foreach (var (cx, cy) in removeCells) data.SetCell(cx, cy, RoadCellType.GenericRoad);
                return false;
            }

            return true;
        }

        private string GetCellLabel(RoadCellType cell)
        {
            switch (cell)
            {
                case RoadCellType.Straight_NS: return "║";
                case RoadCellType.Straight_EW: return "═";
                
                case RoadCellType.Corner_SE: return "╔";
                case RoadCellType.Corner_SW: return "╗";
                case RoadCellType.Corner_NE: return "╚";
                case RoadCellType.Corner_NW: return "╝";
                
                case RoadCellType.HalfT_BusStop_N_Left:
                case RoadCellType.HalfT_BusStop_N_Right:
                    return "╩";
                case RoadCellType.HalfT_BusStop_E_Left:
                case RoadCellType.HalfT_BusStop_E_Right:
                    return "╠";
                case RoadCellType.HalfT_BusStop_S_Left:
                case RoadCellType.HalfT_BusStop_S_Right:
                    return "╦";
                case RoadCellType.HalfT_BusStop_W_Left:
                case RoadCellType.HalfT_BusStop_W_Right:
                    return "╣";
                
                case RoadCellType.Cross: return "╬";
                
                case RoadCellType.DeadEnd_N:
                case RoadCellType.DeadEnd_E:
                case RoadCellType.DeadEnd_S:
                case RoadCellType.DeadEnd_W:
                    return "B";
                
                default: return "▒";
            }
        }
    }
}
