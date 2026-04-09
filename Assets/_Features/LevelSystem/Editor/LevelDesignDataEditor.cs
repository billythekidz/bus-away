using UnityEngine;
using UnityEditor;
using BusAway.Level;

namespace BusAway.LevelEditor
{
    [CustomEditor(typeof(LevelDesignData))]
    public class LevelDesignDataEditor : Editor
    {
        private int randomComplexity = 3;

        public override void OnInspectorGUI()
        {
            LevelDesignData data = (LevelDesignData)target;

            GUILayout.Label("Map Level Configuration", EditorStyles.boldLabel);
            data.gridWidth = EditorGUILayout.IntSlider("Grid Width", data.gridWidth, 4, 30);
            data.gridHeight = EditorGUILayout.IntSlider("Grid Height", data.gridHeight, 4, 30);
            data.tileSize = EditorGUILayout.FloatField("Tile Size", data.tileSize);

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
                
                // Draw 2D Grid
                // Y=0 is bottom, so we render from Y=height-1 down to 0
                for (int y = data.gridHeight - 1; y >= 0; y--)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    for (int x = 0; x < data.gridWidth; x++)
                    {
                        int index = y * data.gridWidth + x;
                        RoadCellType cell = data.grid[index];

                        string label = ".";
                        Color btnColor = Color.white;
                        
                        if (cell == RoadCellType.Road) 
                        {
                            label = "▒";
                            btnColor = new Color(0.3f, 0.3f, 0.3f);
                        }
                        else if (cell == RoadCellType.BusStop)
                        {
                            label = "B";
                            btnColor = new Color(0.2f, 0.6f, 1.0f);
                        }

                        GUI.backgroundColor = btnColor;
                        if (GUILayout.Button(label, GUILayout.Width(25), GUILayout.Height(25)))
                        {
                            // Cycle: Empty -> Road -> Bus Stop -> Empty
                            data.grid[index] = (RoadCellType)(((int)cell + 1) % 3);
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

        private void GenerateRandomGrid(LevelDesignData data, int complexity)
        {
            // Clear entire grid first
            for (int i = 0; i < data.grid.Length; i++)
                data.grid[i] = RoadCellType.Empty;

            int minSize = 4;
            int maxW = Mathf.Max(minSize, data.gridWidth - 2);
            int maxH = Mathf.Max(minSize, data.gridHeight - 2);

            int ringW = Random.Range(minSize, maxW + 1);
            int ringH = Random.Range(minSize, maxH + 1);

            int startX = Random.Range(1, data.gridWidth - ringW + 1);
            int startY = Random.Range(1, data.gridHeight - ringH + 1);

            // Draw horizontal edges
            for (int x = startX; x < startX + ringW; x++)
            {
                data.grid[startY * data.gridWidth + x] = RoadCellType.Road;
                data.grid[(startY + ringH - 1) * data.gridWidth + x] = RoadCellType.Road;
            }

            // Draw vertical edges
            for (int y = startY; y < startY + ringH; y++)
            {
                data.grid[y * data.gridWidth + startX] = RoadCellType.Road;
                data.grid[y * data.gridWidth + (startX + ringW - 1)] = RoadCellType.Road;
            }

            // Scatter some Bus Stops based on complexity
            int numBusStops = 1 + complexity;
            for(int i=0; i<numBusStops; i++)
            {
                int attempts = 100;
                while(attempts > 0)
                {
                    attempts--;
                    int bx = Random.Range(startX, startX + ringW);
                    int by = Random.Range(startY, startY + ringH);
                    
                    if (data.GetCell(bx, by) == RoadCellType.Road)
                    {
                        // Ensure corners are not bus stops
                        bool isCorner = (bx == startX && by == startY) ||
                                        (bx == startX && by == startY + ringH - 1) ||
                                        (bx == startX + ringW - 1 && by == startY) ||
                                        (bx == startX + ringW - 1 && by == startY + ringH - 1);
                        
                        if (!isCorner)
                        {
                            data.grid[by * data.gridWidth + bx] = RoadCellType.BusStop;
                            break;
                        }
                    }
                }
            }
        }
    }
}
