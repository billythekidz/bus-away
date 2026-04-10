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

                        bool isBusStop = cell >= RoadCellType.HalfT_BusStop_N_Left && cell <= RoadCellType.HalfT_BusStop_W_Right;

                        if (isBusStop)
                        {
                            label = "B";
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
        }

        private void GenerateRandomGrid(LevelDesignData data, int complexity)
        {
            // Clear entire grid first
            for (int i = 0; i < data.grid.Length; i++)
                data.grid[i] = RoadCellType.Empty;

            // Guarantee a complete cycle by creating an outer ring road
            for (int x = 0; x < data.gridWidth; x++)
            {
                data.grid[0 * data.gridWidth + x] = RoadCellType.GenericRoad;
                data.grid[(data.gridHeight - 1) * data.gridWidth + x] = RoadCellType.GenericRoad;
            }
            for (int y = 0; y < data.gridHeight; y++)
            {
                data.grid[y * data.gridWidth + 0] = RoadCellType.GenericRoad;
                data.grid[y * data.gridWidth + (data.gridWidth - 1)] = RoadCellType.GenericRoad;
            }

            // minSize is inversely proportional to complexity (1 -> 6, 5 -> 2)
            int minSize = Mathf.Max(2, 7 - complexity);
            
            // Run BSP inside the ring
            RectInt rootArea = new RectInt(1, 1, data.gridWidth - 2, data.gridHeight - 2);
            RecursiveDivide(data, rootArea, minSize, true);

            // Scatter Bus Stops on straight road segments
            int numBusStops = 1 + complexity;
            int attempts = 100;

            while (numBusStops > 0 && attempts-- > 0)
            {
                int x = Random.Range(1, data.gridWidth - 1);
                int y = Random.Range(1, data.gridHeight - 1);
                int idx = y * data.gridWidth + x;

                if (data.grid[idx] == RoadCellType.GenericRoad)
                {
                    bool n = data.GetCell(x, y + 1) != RoadCellType.Empty;
                    bool s = data.GetCell(x, y - 1) != RoadCellType.Empty;
                    bool e = data.GetCell(x + 1, y) != RoadCellType.Empty;
                    bool w = data.GetCell(x - 1, y) != RoadCellType.Empty;

                    // Theo quy tắc mới, T-junction = BusStop.
                    // Random generation hiện tại tạo vòng (ring) và BSP, chưa có nhánh cụt T-junction một chiều.
                    // (Chúng ta có thể thêm logic tạo nhánh T sau, hiện tại để trống đoạn này hoặc nối ra 1 ô).
                    if ((n && s && !e && !w) || (e && w && !n && !s))
                    {
                        // TODO: Tạo T-junction ở đây bằng cách mọc ra 1 GenericRoad vuông góc
                        numBusStops--;
                    }
                }
            }
        }

        private void RecursiveDivide(LevelDesignData data, RectInt area, int minSize, bool isRoot)
        {
            bool canSplitH = area.height >= minSize * 2 + 1;
            bool canSplitV = area.width >= minSize * 2 + 1;

            if (!canSplitH && !canSplitV)
            {
                if (isRoot)
                {
                    // Fallback for very small grids to ensure at least one road is carved
                    minSize = 1;
                    canSplitH = area.height >= 3;
                    canSplitV = area.width >= 3;
                    if (!canSplitH && !canSplitV) return; // Still completely too small (e.g. 2x2)
                }
                else return;
            }

            bool splitHorizontal = false;
            if (canSplitH && canSplitV)
                splitHorizontal = Random.value > 0.5f;
            else if (canSplitH)
                splitHorizontal = true;
            else if (canSplitV)
                splitHorizontal = false;

            if (splitHorizontal)
            {
                int splitY = Random.Range(area.yMin + minSize, area.yMax - minSize);
                for (int x = area.xMin; x < area.xMax; x++)
                    data.grid[splitY * data.gridWidth + x] = RoadCellType.GenericRoad;
                
                RecursiveDivide(data, new RectInt(area.x, area.y, area.width, splitY - area.y), minSize, false);
                RecursiveDivide(data, new RectInt(area.x, splitY + 1, area.width, area.yMax - (splitY + 1)), minSize, false);
            }
            else
            {
                int splitX = Random.Range(area.xMin + minSize, area.xMax - minSize);
                for (int y = area.yMin; y < area.yMax; y++)
                    data.grid[y * data.gridWidth + splitX] = RoadCellType.GenericRoad;

                RecursiveDivide(data, new RectInt(area.x, area.y, splitX - area.x, area.height), minSize, false);
                RecursiveDivide(data, new RectInt(splitX + 1, area.y, area.xMax - (splitX + 1), area.height), minSize, false);
            }
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
                
                case RoadCellType.HalfT_E_Left: return "T_EL"; // ╠
                case RoadCellType.HalfT_E_Right: return "T_ER"; // ╠
                case RoadCellType.HalfT_W_Left: return "T_WL"; // ╣
                case RoadCellType.HalfT_W_Right: return "T_WR"; // ╣
                case RoadCellType.HalfT_S_Left: return "T_SL"; // ╦
                case RoadCellType.HalfT_S_Right: return "T_SR"; // ╦
                case RoadCellType.HalfT_N_Left: return "T_NL"; // ╩
                case RoadCellType.HalfT_N_Right: return "T_NR"; // ╩
                
                case RoadCellType.Cross: return "╬";
                
                case RoadCellType.DeadEnd_N: return "╨";
                case RoadCellType.DeadEnd_E: return "╞";
                case RoadCellType.DeadEnd_S: return "╥";
                case RoadCellType.DeadEnd_W: return "╡";
                
                default: return "▒";
            }
        }
    }
}
