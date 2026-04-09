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

                // Draw 2D Grid
                // Y=0 is near camera (bottom of game screen), so we render from Y=0 downward
                // to match the game's top-down view (Y=gridHeight-1 at top of screen = top of editor)
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

            // Draw edges as a continuous path
            List<Vector2Int> path = new List<Vector2Int>();
            for (int x = startX; x < startX + ringW - 1; x++) path.Add(new Vector2Int(x, startY));
            for (int y = startY; y < startY + ringH - 1; y++) path.Add(new Vector2Int(startX + ringW - 1, y));
            for (int x = startX + ringW - 1; x > startX; x--) path.Add(new Vector2Int(x, startY + ringH - 1));
            for (int y = startY + ringH - 1; y > startY; y--) path.Add(new Vector2Int(startX, y));

            int deformIters = complexity * 4;
            while (deformIters-- > 0 && path.Count > 0)
            {
                // Find all valid straight segments of length 3 cells
                List<int> validIndices = new List<int>();
                for (int i = 0; i < path.Count; i++)
                {
                    Vector2Int p0 = path[(i + path.Count - 1) % path.Count];
                    Vector2Int p2 = path[(i + 1) % path.Count];
                    if (p0.x == p2.x || p0.y == p2.y) validIndices.Add(i);
                }
                if (validIndices.Count == 0) break;

                int pick = validIndices[Random.Range(0, validIndices.Count)];
                Vector2Int P0 = path[(pick + path.Count - 1) % path.Count];
                Vector2Int P1 = path[pick];
                Vector2Int P2 = path[(pick + 1) % path.Count];

                Vector2Int V = P2 - P0;
                Vector2Int[] normals = new Vector2Int[] { new Vector2Int(V.y / 2, -V.x / 2), new Vector2Int(-V.y / 2, V.x / 2) };
                Vector2Int N = normals[Random.Range(0, 2)];

                Vector2Int C = P0 + N;
                Vector2Int M = P1 + N;
                Vector2Int D = P2 + N;

                // Bounds check with margin
                if (C.x < 1 || C.x >= data.gridWidth - 1 || C.y < 1 || C.y >= data.gridHeight - 1) continue;
                if (M.x < 1 || M.x >= data.gridWidth - 1 || M.y < 1 || M.y >= data.gridHeight - 1) continue;
                if (D.x < 1 || D.x >= data.gridWidth - 1 || D.y < 1 || D.y >= data.gridHeight - 1) continue;

                // Neighbor check
                System.Func<Vector2Int, int> countNeighbors = (pos) =>
                {
                    int c = 0;
                    foreach (var p in path)
                    {
                        if (p == pos) return 100; // Self collision
                        int dx = Mathf.Abs(p.x - pos.x);
                        int dy = Mathf.Abs(p.y - pos.y);
                        if (dx + dy == 1) c++;
                    }
                    return c;
                };

                Vector2Int savedP1 = path[pick];
                path.RemoveAt(pick);

                if (countNeighbors(C) == 1 && countNeighbors(M) == 0 && countNeighbors(D) == 1)
                {
                    path.Insert(pick, D);
                    path.Insert(pick, M);
                    path.Insert(pick, C);
                }
                else
                {
                    path.Insert(pick, savedP1);
                }
            }

            // Write path to grid
            foreach (var p in path)
            {
                data.grid[p.y * data.gridWidth + p.x] = RoadCellType.Road;
            }

            // Scatter Bus Stops on straight segments
            int numBusStops = 1 + complexity;
            int stopAttempts = 100;
            while (numBusStops > 0 && stopAttempts-- > 0)
            {
                int idx = Random.Range(0, path.Count);
                Vector2Int b = path[idx];

                if (data.GetCell(b.x, b.y) == RoadCellType.Road)
                {
                    Vector2Int prev = path[(idx + path.Count - 1) % path.Count];
                    Vector2Int next = path[(idx + 1) % path.Count];
                    if (prev.x == next.x || prev.y == next.y) // Only place on straight roads
                    {
                        data.grid[b.y * data.gridWidth + b.x] = RoadCellType.BusStop;
                        numBusStops--;
                    }
                }
            }
        }
    }
}
