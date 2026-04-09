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

            // Complexity determines the target length of the road
            int targetLength = (int)(Mathf.Min(data.gridWidth, data.gridHeight) * (1.5f + complexity * 0.8f));
            
            // Random start near the edge
            int x = Random.Range(2, data.gridWidth - 2);
            int y = 2; // Start near bottom

            data.grid[y * data.gridWidth + x] = RoadCellType.Road;
            int placed = 1;

            int dx = 0; int dy = 1; // Start moving up
            int stuckCounter = 0;

            while (placed < targetLength && stuckCounter < 50)
            {
                // Chance to turn
                float turnChance = 0.2f + (complexity * 0.1f);
                if (Random.value < turnChance)
                {
                    // Turn left or right
                    if (dx != 0) { dy = Random.value > 0.5f ? 1 : -1; dx = 0; }
                    else { dx = Random.value > 0.5f ? 1 : -1; dy = 0; }
                }

                int nx = x + dx;
                int ny = y + dy;

                // Check boundaries (keep 1 tile padding)
                if (nx < 1 || nx >= data.gridWidth - 1 || ny < 1 || ny >= data.gridHeight - 1)
                {
                    // Force a turn
                    int temp = dx; dx = dy == 0 ? (Random.value > 0.5f ? 1 : -1) : 0; dy = temp == 0 ? (Random.value > 0.5f ? 1 : -1) : 0;
                    stuckCounter++;
                    continue;
                }

                // Try to avoid 2x2 blocks of road (self-intersection prevention)
                int neighbors = 0;
                if (data.GetCell(nx+1, ny) != RoadCellType.Empty) neighbors++;
                if (data.GetCell(nx-1, ny) != RoadCellType.Empty) neighbors++;
                if (data.GetCell(nx, ny+1) != RoadCellType.Empty) neighbors++;
                if (data.GetCell(nx, ny-1) != RoadCellType.Empty) neighbors++;

                // If moving to a new cell would touch too many existing roads, skip and turn
                if (data.GetCell(nx, ny) == RoadCellType.Empty && neighbors >= 2)
                {
                    int temp = dx; dx = dy == 0 ? (Random.value > 0.5f ? 1 : -1) : 0; dy = temp == 0 ? (Random.value > 0.5f ? 1 : -1) : 0;
                    stuckCounter++;
                    continue;
                }

                // Place road
                if (data.GetCell(nx, ny) == RoadCellType.Empty)
                {
                    data.grid[ny * data.gridWidth + nx] = RoadCellType.Road;
                    placed++;
                    stuckCounter = 0;
                }
                
                x = nx;
                y = ny;
            }

            // Scatter some Bus Stops based on complexity
            int numBusStops = 1 + (complexity / 2);
            for(int i=0; i<numBusStops; i++)
            {
                int attempts = 100;
                while(attempts > 0)
                {
                    attempts--;
                    int bx = Random.Range(1, data.gridWidth - 1);
                    int by = Random.Range(1, data.gridHeight - 1);
                    
                    if (data.GetCell(bx, by) == RoadCellType.Road)
                    {
                        // Found a road, make it a bus stop
                        data.grid[by * data.gridWidth + bx] = RoadCellType.BusStop;
                        break;
                    }
                }
            }
        }
    }
}
