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

            // ── Gameplay Config ──────────────────────────────────────────────────
            EditorGUILayout.LabelField("── Gameplay Config ──", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            data.levelGoalCoin = EditorGUILayout.IntField(
                new GUIContent("Level Goal (Coins)", "Total coins the player must collect to clear this level."),
                data.levelGoalCoin);
            data.busStopLength = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Bus Stop Count", "Number of bus stops to procedurally place on the map."),
                data.busStopLength));

            // ── Bus Dispatch Config ───────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("── Bus Dispatch Config ──", EditorStyles.boldLabel);

            data.busesPerStop = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Buses Per Stop",
                    "How many buses are dispatched per bus stop. All stops share the same number.\n" +
                    "Each bus loops the road ring continuously until it collects enough same-color passengers,\n" +
                    "then drives into the bus stop and disappears."),
                data.busesPerStop));

            data.agentsPerBus = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Agents Per Bus",
                    "Minimum number of same-color passengers a bus must board before it exits the road loop\n" +
                    "and parks at the bus stop. Default: 32.\n\n" +
                    "This also drives crowd land sizing automatically:\n" +
                    "  agentCount per color = (buses of that color) × Agents Per Bus"),
                data.agentsPerBus));

            // --- Auto-recalculate resolvedLands based on landColorPalette + busesPerStop + agentsPerBus ---
            // Each unique color in the palette gets: busesPerStop buses × agentsPerBus agents
            if (data.landColorPalette != null && data.landColorPalette.Count > 0)
            {
                if (data.resolvedLands == null) data.resolvedLands = new List<CrowdLandConfig>();
                data.resolvedLands.Clear();
                foreach (var color in data.landColorPalette)
                {
                    data.resolvedLands.Add(new CrowdLandConfig
                    {
                        color = color,
                        // busesPerStop buses of this color × agentsPerBus passengers each
                        agentCount = data.busesPerStop * data.agentsPerBus
                    });
                }
            }

            int totalBuses = (data.landColorPalette != null ? data.landColorPalette.Count : 0) * data.busesPerStop;
            int totalAgents = totalBuses * data.agentsPerBus;
            EditorGUILayout.HelpBox(
                $"Total buses: {totalBuses}  ({data.busesPerStop} per stop × {data.landColorPalette?.Count ?? 0} colors)\n" +
                $"Total agents: {totalAgents}  ({data.busesPerStop} buses × {data.agentsPerBus} agents × {data.landColorPalette?.Count ?? 0} colors)",
                MessageType.Info);

            // ── Crowd Lands ───────────────────────────────────────────────────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("── Crowd Lands ──", EditorStyles.boldLabel);

            int newMin = EditorGUILayout.IntSlider(
                new GUIContent("Min Land Count", "Minimum number of crowd spawn areas to generate on the map."),
                data.minLandCount, 2, 5);
            int newMax = EditorGUILayout.IntSlider(
                new GUIContent("Max Land Count", "Maximum number of crowd spawn areas to generate on the map."),
                data.maxLandCount, 2, 5);
            if (newMin > newMax) newMin = newMax;
            data.minLandCount = newMin;
            data.maxLandCount = newMax;

            // Color palette with visual swatches
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Land Color Palette", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Each color here = one bus color group. Crowd agent count per color is auto-calculated from Buses Per Stop × Agents Per Bus.",
                MessageType.None);
            if (data.landColorPalette == null) data.landColorPalette = new List<Color>();

            if (data.landColorPalette.Count < data.busStopLength)
            {
                EditorGUILayout.HelpBox(
                    $"Palette has {data.landColorPalette.Count} colors but Bus Stop Count = {data.busStopLength}. " +
                    $"Add at least {data.busStopLength} colors so each bus stop has a distinct bus color.",
                    MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < data.landColorPalette.Count; i++)
            {
                data.landColorPalette[i] = EditorGUILayout.ColorField(GUIContent.none, data.landColorPalette[i], false, true, false, GUILayout.Width(40));
            }
            if (GUILayout.Button("+", GUILayout.Width(24))) data.landColorPalette.Add(Color.white);
            if (data.landColorPalette.Count > 0 && GUILayout.Button("-", GUILayout.Width(24)))
                data.landColorPalette.RemoveAt(data.landColorPalette.Count - 1);
            EditorGUILayout.EndHorizontal();

            // Preview resolved lands (auto-calculated)
            if (data.resolvedLands != null && data.resolvedLands.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Resolved Lands (auto-calculated)", EditorStyles.miniLabel);
                int total = 0;
                foreach (var land in data.resolvedLands)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ColorField(GUIContent.none, land.color, false, false, false, GUILayout.Width(40));
                    EditorGUILayout.LabelField($"{land.agentCount} agents  ({data.busesPerStop} bus × {data.agentsPerBus} each)");
                    total += land.agentCount;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.LabelField($"Grand Total: {total} agents across {data.resolvedLands.Count} color groups", EditorStyles.boldLabel);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
        }

        private int CountConsecutiveMask(LevelDesignData data, int startX, int startY, int dx, int dy, int expectedMask)
        {
            int count = 0;
            int x = startX + dx;
            int y = startY + dy;
            
            System.Func<int, int, bool> HasRoad = (cx, cy) => {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                return data.grid[cy * data.gridWidth + cx] != RoadCellType.Empty; /* treating busstop same as empty for masking is intended below */
            };

            while (x >= 0 && x < data.gridWidth && y >= 0 && y < data.gridHeight)
            {
                bool n = HasRoad(x, y + 1);
                bool e = HasRoad(x + 1, y);
                bool s = HasRoad(x, y - 1);
                bool w = HasRoad(x - 1, y);
                int m = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);
                if (m == expectedMask) count++;
                else break;
                
                x += dx;
                y += dy;
            }
            return count;
        }

        private void UpdateAllRoadTypes(LevelDesignData data)
        {
            System.Func<int, int, bool> HasRoad = (cx, cy) => {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                return data.grid[cy * data.gridWidth + cx] != RoadCellType.Empty;
            };

            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    int index = y * data.gridWidth + x;
                    RoadCellType current = data.grid[index];
                    // Chú ý: Vì BusStop hiện tại chính là HalfT, và HalfT được gen tự động từ T-Junction.
                    // Chúng ta không skip tự động tính toán cho BusStop nữa.
                    if (current == RoadCellType.Empty) continue;

                    bool n = HasRoad(x, y + 1);
                    bool e = HasRoad(x + 1, y);
                    bool s = HasRoad(x, y - 1);
                    bool w = HasRoad(x - 1, y);

                    int mask = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);

                    RoadCellType newType = RoadCellType.GenericRoad;

                    switch (mask)
                    {
                        case 0: break;
                        case 1: newType = RoadCellType.DeadEnd_N; break;
                        case 2: newType = RoadCellType.DeadEnd_E; break;
                        case 4: newType = RoadCellType.DeadEnd_S; break;
                        case 8: newType = RoadCellType.DeadEnd_W; break;

                        case 5:  newType = RoadCellType.Straight_NS; break;
                        case 10: newType = RoadCellType.Straight_EW; break;
                        
                        case 3: newType = RoadCellType.Corner_NE; break;
                        case 6: newType = RoadCellType.Corner_SE; break;
                        case 12: newType = RoadCellType.Corner_SW; break;
                        case 9: newType = RoadCellType.Corner_NW; break;
                        
                        case 11: newType = (CountConsecutiveMask(data, x, y, -1, 0, 11) % 2 == 0) ? RoadCellType.HalfT_BusStop_N_Left : RoadCellType.HalfT_BusStop_N_Right; break;
                        case 7: newType = (CountConsecutiveMask(data, x, y, 0, -1, 7) % 2 == 0) ? RoadCellType.HalfT_BusStop_E_Right : RoadCellType.HalfT_BusStop_E_Left; break;
                        case 14: newType = (CountConsecutiveMask(data, x, y, -1, 0, 14) % 2 == 0) ? RoadCellType.HalfT_BusStop_S_Right : RoadCellType.HalfT_BusStop_S_Left; break;
                        case 13: newType = (CountConsecutiveMask(data, x, y, 0, -1, 13) % 2 == 0) ? RoadCellType.HalfT_BusStop_W_Left : RoadCellType.HalfT_BusStop_W_Right; break;
                        
                        case 15: newType = RoadCellType.Cross; break;
                    }
                    data.grid[index] = newType;
                }
            }

            // Pass 2: Overwrite the explicitly adjacent bus stop branches that generated fake corners.
            // By enforcing their connectivity direction, they will properly render as DeadEnds.
            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    RoadCellType cell = data.grid[y * data.gridWidth + x];
                    if (cell >= RoadCellType.HalfT_BusStop_N_Left && cell <= RoadCellType.HalfT_BusStop_W_Right)
                    {
                        if (cell == RoadCellType.HalfT_BusStop_N_Left || cell == RoadCellType.HalfT_BusStop_N_Right)
                        {
                            if (y + 1 < data.gridHeight && data.grid[(y + 1) * data.gridWidth + x] != RoadCellType.Empty) 
                                data.grid[(y + 1) * data.gridWidth + x] = RoadCellType.DeadEnd_N;
                        }
                        else if (cell == RoadCellType.HalfT_BusStop_E_Left || cell == RoadCellType.HalfT_BusStop_E_Right)
                        {
                            if (x + 1 < data.gridWidth && data.grid[y * data.gridWidth + (x + 1)] != RoadCellType.Empty) 
                                data.grid[y * data.gridWidth + (x + 1)] = RoadCellType.DeadEnd_E;
                        }
                        else if (cell == RoadCellType.HalfT_BusStop_S_Left || cell == RoadCellType.HalfT_BusStop_S_Right)
                        {
                            if (y - 1 >= 0 && data.grid[(y - 1) * data.gridWidth + x] != RoadCellType.Empty) 
                                data.grid[(y - 1) * data.gridWidth + x] = RoadCellType.DeadEnd_S;
                        }
                        else if (cell == RoadCellType.HalfT_BusStop_W_Left || cell == RoadCellType.HalfT_BusStop_W_Right)
                        {
                            if (x - 1 >= 0 && data.grid[y * data.gridWidth + (x - 1)] != RoadCellType.Empty) 
                                data.grid[y * data.gridWidth + (x - 1)] = RoadCellType.DeadEnd_W;
                        }
                    }
                }
            }
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
            PlaceBusStopsOrganic(data);
        }

        private void PlaceBusStopsOrganic(LevelDesignData data)
        {
            int numBusStops = data.busStopLength;
            var candidates = new System.Collections.Generic.List<System.Func<bool>>();

            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    if (IsProtectedBoardingArea(x, y, data.gridWidth)) continue;

                    // Horizontal stops (2 cells wide)
                    if (x + 1 < data.gridWidth && data.GetCell(x, y) == RoadCellType.GenericRoad && data.GetCell(x + 1, y) == RoadCellType.GenericRoad)
                    {
                        if (IsProtectedBoardingArea(x + 1, y, data.gridWidth)) continue;
                        // Check Upwards
                        if (y + 1 < data.gridHeight)
                        {
                            int cx_ = x, cy_ = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx_, cy_ + 1) == RoadCellType.Empty && data.GetCell(cx_ + 1, cy_ + 1) == RoadCellType.Empty)
                                {
                                    if (cx_ - 1 >= 0 && data.GetCell(cx_ - 1, cy_ + 1) != RoadCellType.Empty) return false;
                                    if (cx_ + 2 < data.gridWidth && data.GetCell(cx_ + 2, cy_ + 1) != RoadCellType.Empty) return false;
                                    if (cy_ + 2 < data.gridHeight && data.GetCell(cx_, cy_ + 2) != RoadCellType.Empty) return false;
                                    if (cy_ + 2 < data.gridHeight && data.GetCell(cx_ + 1, cy_ + 2) != RoadCellType.Empty) return false;

                                    data.SetCell(cx_, cy_ + 1, RoadCellType.GenericRoad);
                                    data.SetCell(cx_ + 1, cy_ + 1, RoadCellType.GenericRoad);
                                    return true;
                                }
                                return false;
                            });
                        }
                        // Check Downwards
                        if (y - 1 >= 0)
                        {
                            int cx_ = x, cy_ = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx_, cy_ - 1) == RoadCellType.Empty && data.GetCell(cx_ + 1, cy_ - 1) == RoadCellType.Empty)
                                {
                                    if (cx_ - 1 >= 0 && data.GetCell(cx_ - 1, cy_ - 1) != RoadCellType.Empty) return false;
                                    if (cx_ + 2 < data.gridWidth && data.GetCell(cx_ + 2, cy_ - 1) != RoadCellType.Empty) return false;
                                    if (cy_ - 2 >= 0 && data.GetCell(cx_, cy_ - 2) != RoadCellType.Empty) return false;
                                    if (cy_ - 2 >= 0 && data.GetCell(cx_ + 1, cy_ - 2) != RoadCellType.Empty) return false;

                                    data.SetCell(cx_, cy_ - 1, RoadCellType.GenericRoad);
                                    data.SetCell(cx_ + 1, cy_ - 1, RoadCellType.GenericRoad);
                                    return true;
                                }
                                return false;
                            });
                        }
                    }

                    // Vertical stops (2 cells tall)
                    if (y + 1 < data.gridHeight && data.GetCell(x, y) == RoadCellType.GenericRoad && data.GetCell(x, y + 1) == RoadCellType.GenericRoad)
                    {
                        if (IsProtectedBoardingArea(x, y + 1, data.gridWidth)) continue;
                        // Check Rightwards
                        if (x + 1 < data.gridWidth)
                        {
                            int cx_ = x, cy_ = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx_ + 1, cy_) == RoadCellType.Empty && data.GetCell(cx_ + 1, cy_ + 1) == RoadCellType.Empty)
                                {
                                    if (cy_ - 1 >= 0 && data.GetCell(cx_ + 1, cy_ - 1) != RoadCellType.Empty) return false;
                                    if (cy_ + 2 < data.gridHeight && data.GetCell(cx_ + 1, cy_ + 2) != RoadCellType.Empty) return false;
                                    if (cx_ + 2 < data.gridWidth && data.GetCell(cx_ + 2, cy_) != RoadCellType.Empty) return false;
                                    if (cx_ + 2 < data.gridWidth && data.GetCell(cx_ + 2, cy_ + 1) != RoadCellType.Empty) return false;

                                    data.SetCell(cx_ + 1, cy_, RoadCellType.GenericRoad);
                                    data.SetCell(cx_ + 1, cy_ + 1, RoadCellType.GenericRoad);
                                    return true;
                                }
                                return false;
                            });
                        }
                        // Check Leftwards
                        if (x - 1 >= 0)
                        {
                            int cx_ = x, cy_ = y;
                            candidates.Add(() => {
                                if (data.GetCell(cx_ - 1, cy_) == RoadCellType.Empty && data.GetCell(cx_ - 1, cy_ + 1) == RoadCellType.Empty)
                                {
                                    if (cy_ - 1 >= 0 && data.GetCell(cx_ - 1, cy_ - 1) != RoadCellType.Empty) return false;
                                    if (cy_ + 2 < data.gridHeight && data.GetCell(cx_ - 1, cy_ + 2) != RoadCellType.Empty) return false;
                                    if (cx_ - 2 >= 0 && data.GetCell(cx_ - 2, cy_) != RoadCellType.Empty) return false;
                                    if (cx_ - 2 >= 0 && data.GetCell(cx_ - 2, cy_ + 1) != RoadCellType.Empty) return false;

                                    data.SetCell(cx_ - 1, cy_, RoadCellType.GenericRoad);
                                    data.SetCell(cx_ - 1, cy_ + 1, RoadCellType.GenericRoad);
                                    return true;
                                }
                                return false;
                            });
                        }
                    }
                }
            }

            // Shuffle and place
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = tmp;
            }

            foreach (var act in candidates)
            {
                if (numBusStops <= 0) break;
                if (act.Invoke()) numBusStops--;
            }
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
                // Ensure it's a completely straight piece before removing
                if (horizontal && (data.GetCell(cx, cy - 1) != RoadCellType.Empty || data.GetCell(cx, cy + 1) != RoadCellType.Empty)) return false;
                if (!horizontal && (data.GetCell(cx - 1, cy) != RoadCellType.Empty || data.GetCell(cx + 1, cy) != RoadCellType.Empty)) return false;
            }
            foreach (var (cx, cy) in notchCells)
            {
                if (cx < 0 || cx >= data.gridWidth || cy < 0 || cy >= data.gridHeight) return false;
                if (data.GetCell(cx, cy) != RoadCellType.Empty) return false;
            }

            foreach (var (cx, cy) in edgeCells) data.SetCell(cx, cy, RoadCellType.Empty);
            foreach (var (cx, cy) in notchCells) data.SetCell(cx, cy, RoadCellType.GenericRoad);
            return true;
        }

        private bool TryCarveScurve(LevelDesignData data, int ex, int ey, bool horizontal, int outDir, bool mirror)
        {
            int sDir = mirror ? -1 : 1;

            var removeCells = new System.Collections.Generic.List<(int, int)>();
            var newCells = new System.Collections.Generic.List<(int, int)>();
            var protectedCells = new System.Collections.Generic.List<(int, int)>();

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

                protectedCells.Add((ex - sDir * 2, r1));
                protectedCells.Add((ex - sDir, r2));
                protectedCells.Add((ex + sDir * 2, r1));
                protectedCells.Add((ex + sDir * 2, r2));
                protectedCells.Add((ex, r2 + outDir));
                protectedCells.Add((ex + sDir, r2 + outDir));
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

                protectedCells.Add((c1, ey - sDir * 2));
                protectedCells.Add((c2, ey - sDir));
                protectedCells.Add((c1, ey + sDir * 2));
                protectedCells.Add((c2, ey + sDir * 2));
                protectedCells.Add((c2 + outDir, ey));
                protectedCells.Add((c2 + outDir, ey + sDir));
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
            foreach (var (cx, cy) in protectedCells)
            {
                if (cx >= 0 && cx < data.gridWidth && cy >= 0 && cy < data.gridHeight)
                {
                    if (data.GetCell(cx, cy) != RoadCellType.Empty) return false;
                }
            }

            foreach (var (cx, cy) in removeCells) data.SetCell(cx, cy, RoadCellType.Empty);
            foreach (var (cx, cy) in newCells) data.SetCell(cx, cy, RoadCellType.GenericRoad);
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
