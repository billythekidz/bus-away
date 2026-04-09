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

                for (int y = 0; y < data.gridHeight; y++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    for (int x = 0; x < data.gridWidth; x++)
                    {
                        int index = y * data.gridWidth + x;
                        RoadCellType cell = data.grid[index];

                        string label = ".";
                        Color btnColor = Color.white;

                        if (cell == RoadCellType.BusStop)
                        {
                            label = "B";
                            btnColor = new Color(0.2f, 0.6f, 1.0f);
                        }
                        else if (cell == RoadCellType.GenericCrosswalk || cell == RoadCellType.Crosswalk_NS || cell == RoadCellType.Crosswalk_EW)
                        {
                            label = "C";
                            btnColor = new Color(0.8f, 0.8f, 0.0f);
                        }
                        else if (cell != RoadCellType.Empty)
                        {
                            label = "▒";
                            btnColor = new Color(0.3f, 0.3f, 0.3f);
                        }

                        GUI.backgroundColor = btnColor;
                        if (GUILayout.Button(label, GUILayout.Width(25), GUILayout.Height(25)))
                        {
                            if (cell == RoadCellType.Empty)
                                data.grid[index] = RoadCellType.GenericRoad;
                            else if (cell != RoadCellType.BusStop && cell != RoadCellType.GenericCrosswalk && cell != RoadCellType.Crosswalk_NS && cell != RoadCellType.Crosswalk_EW)
                                data.grid[index] = RoadCellType.BusStop;
                            else if (cell == RoadCellType.BusStop)
                                data.grid[index] = RoadCellType.GenericCrosswalk;
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
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(15);
            DrawDefaultInspector();

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
                return data.grid[cy * data.gridWidth + cx] != RoadCellType.Empty;
            };

            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    int index = y * data.gridWidth + x;
                    RoadCellType current = data.grid[index];

                    if (current == RoadCellType.Empty || current == RoadCellType.BusStop) continue;

                    bool isCrosswalk = (current == RoadCellType.GenericCrosswalk || current == RoadCellType.Crosswalk_NS || current == RoadCellType.Crosswalk_EW);

                    bool n = HasRoad(x, y + 1);
                    bool e = HasRoad(x + 1, y);
                    bool s = HasRoad(x, y - 1);
                    bool w = HasRoad(x - 1, y);

                    // Diagonals for inner corners
                    bool ne = HasRoad(x + 1, y + 1);
                    bool se = HasRoad(x + 1, y - 1);
                    bool sw = HasRoad(x - 1, y - 1);
                    bool nw = HasRoad(x - 1, y + 1);

                    int mask = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);

                    RoadCellType newType = isCrosswalk ? RoadCellType.GenericCrosswalk : RoadCellType.GenericRoad;

                    switch (mask)
                    {
                        case 0: break; // Unconnected
                        case 1: newType = RoadCellType.DeadEnd_N; break;
                        case 2: newType = RoadCellType.DeadEnd_E; break;
                        case 4: newType = RoadCellType.DeadEnd_S; break;
                        case 8: newType = RoadCellType.DeadEnd_W; break;
                        
                        case 5: newType = isCrosswalk ? RoadCellType.Crosswalk_NS : RoadCellType.Straight_NS; break;
                        case 10: newType = isCrosswalk ? RoadCellType.Crosswalk_EW : RoadCellType.Straight_EW; break;
                        
                        case 3: newType = ne ? RoadCellType.InnerCorner_NE : RoadCellType.Corner_NE; break;
                        case 6: newType = se ? RoadCellType.InnerCorner_SE : RoadCellType.Corner_SE; break;
                        case 12: newType = sw ? RoadCellType.InnerCorner_SW : RoadCellType.Corner_SW; break;
                        case 9: newType = nw ? RoadCellType.InnerCorner_NW : RoadCellType.Corner_NW; break;
                        
                        case 11: newType = RoadCellType.TJunction_N; break;
                        case 7: newType = RoadCellType.TJunction_E; break;
                        case 14: newType = RoadCellType.TJunction_S; break;
                        case 13: newType = RoadCellType.TJunction_W; break;
                        
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

            // minSize is inversely proportional to complexity (1 -> 6, 5 -> 2)
            int minSize = Mathf.Max(2, 7 - complexity);
            
            RectInt rootArea = new RectInt(0, 0, data.gridWidth, data.gridHeight);
            RecursiveDivide(data, rootArea, minSize);

            // Task 2: Hole Punching (Decimation)
            float eraseChance = (6 - complexity) * 0.08f; 
            for (int i = 0; i < data.grid.Length; i++)
            {
                if (data.grid[i] == RoadCellType.GenericRoad && Random.value < eraseChance)
                    data.grid[i] = RoadCellType.Empty;
            }

            // Task 3: Scatter POIs
            int attempts = 100;
            int numBusStops = 1 + complexity;
            int numCrosswalks = 1 + complexity;
            
            while ((numBusStops > 0 || numCrosswalks > 0) && attempts-- > 0)
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
                    
                    if ((n && s && !e && !w) || (e && w && !n && !s))
                    {
                        if (numBusStops > 0) {
                            data.grid[idx] = RoadCellType.BusStop;
                            numBusStops--;
                        } else if (numCrosswalks > 0) {
                            data.grid[idx] = RoadCellType.GenericCrosswalk;
                            numCrosswalks--;
                        }
                    }
                }
            }
        }

        private void RecursiveDivide(LevelDesignData data, RectInt area, int minSize)
        {
            if (area.width < minSize * 2 + 1 && area.height < minSize * 2 + 1) return;

            bool splitHorizontal = area.height > area.width;
            if (area.width >= minSize * 2 + 1 && area.height >= minSize * 2 + 1)
                splitHorizontal = Random.value > 0.5f;

            if (splitHorizontal)
            {
                if (area.height < minSize * 2 + 1) return;
                int splitY = Random.Range(area.yMin + minSize, area.yMax - minSize);
                for (int x = area.xMin; x < area.xMax; x++)
                    data.grid[splitY * data.gridWidth + x] = RoadCellType.GenericRoad;
                
                RecursiveDivide(data, new RectInt(area.x, area.y, area.width, splitY - area.y), minSize);
                RecursiveDivide(data, new RectInt(area.x, splitY + 1, area.width, area.yMax - (splitY + 1)), minSize);
            }
            else
            {
                if (area.width < minSize * 2 + 1) return;
                int splitX = Random.Range(area.xMin + minSize, area.xMax - minSize);
                for (int y = area.yMin; y < area.yMax; y++)
                    data.grid[y * data.gridWidth + splitX] = RoadCellType.GenericRoad;

                RecursiveDivide(data, new RectInt(area.x, area.y, splitX - area.x, area.height), minSize);
                RecursiveDivide(data, new RectInt(splitX + 1, area.y, area.xMax - (splitX + 1), area.height), minSize);
            }
        }
    }
}
