using UnityEngine;
using UnityEditor;
using BusAway.Level;
using System.Collections.Generic;

namespace BusAway.LevelEditor
{
    [CustomEditor(typeof(LevelDesignData))]
    public class LevelDesignDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Big, clear button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
            buttonStyle.fixedHeight = 40;

            if (GUILayout.Button("🎲 Generate Random Road Map (Buses Away Style)", buttonStyle))
            {
                GenerateRandomRoadMap();
            }
            
            GUILayout.Space(15);
            
            // Draw the default inspector below the button
            DrawDefaultInspector();
        }

        private void GenerateRandomRoadMap()
        {
            LevelDesignData data = (LevelDesignData)target;
            Undo.RecordObject(data, "Generate Random Road Map");

            data.roadSegments.Clear();
            data.buses.Clear();
            
            // Architecture based on GameDesign Docs: 
            // The map grid usually has ONE MAIN RING (Loop/U-Shape)
            // and PARKING BRANCHES sticking outwards at right angles.

            // ============================================
            // 1. Create Main Road (Rectangular Main Loop)
            // ============================================
            float width = Random.Range(6.0f, 10.0f);
            float depth = Random.Range(4.0f, 8.0f);
            
            // Create ring nodes
            Vector3 topLeft = new Vector3(-width, 0, depth);
            Vector3 topRight = new Vector3(width, 0, depth);
            Vector3 botRight = new Vector3(width, 0, -depth);
            Vector3 botLeft = new Vector3(-width, 0, -depth);

            RoadSegmentData mainRoad = new RoadSegmentData
            {
                segmentName = "Main Ring Road",
                cornerRadius = 2.0f,
                roadWidth = 2.5f,
                isClosedLoop = true,
                sharpPoints = new List<Vector3> { botLeft, topLeft, topRight, botRight, botLeft }
            };
            data.roadSegments.Add(mainRoad);

            // ============================================
            // 2. Create Parking Branches
            // ============================================
            int numParkingBranches = Random.Range(4, 9);
            List<Vector3> usedOrigins = new List<Vector3>();
            
            Color[] busColors = new Color[] { Color.red, Color.blue, Color.yellow, Color.green, new Color(0.5f, 0, 0.5f) /* Purple */ };

            for (int i = 0; i < numParkingBranches; i++)
            {
                int edge = Random.Range(0, 4); // 0: Top, 1: Right, 2: Bottom, 3: Left
                Vector3 origin = Vector3.zero;
                Vector3 direction = Vector3.forward;

                // Choose a random point on the selected axis to extend the branch outwards
                if (edge == 0) // Top
                {
                    origin = new Vector3(Mathf.Round(Random.Range(-width + 2, width - 2) / 2f) * 2f, 0, depth);
                    direction = Vector3.forward;
                }
                else if (edge == 1) // Right
                {
                    origin = new Vector3(width, 0, Mathf.Round(Random.Range(-depth + 2, depth - 2) / 2f) * 2f);
                    direction = Vector3.right;
                }
                else if (edge == 2) // Bottom
                {
                    origin = new Vector3(Mathf.Round(Random.Range(-width + 2, width - 2) / 2f) * 2f, 0, -depth);
                    direction = Vector3.back;
                }
                else // Left
                {
                    origin = new Vector3(-width, 0, Mathf.Round(Random.Range(-depth + 2, depth - 2) / 2f) * 2f);
                    direction = Vector3.left;
                }

                // Prevent overlaps: Skip if too close to an existing branch origin
                bool isOverlap = false;
                foreach(var uo in usedOrigins)
                {
                    if (Vector3.Distance(uo, origin) < 2.5f) { isOverlap = true; break; }
                }
                if (isOverlap) continue;

                usedOrigins.Add(origin);

                float branchLength = Random.Range(3.5f, 5.5f);
                Vector3 endPoint = origin + direction * branchLength;

                // Create branch road linking perpendicularly to the Main Loop
                RoadSegmentData branch = new RoadSegmentData
                {
                    segmentName = $"Parking Branch {data.roadSegments.Count}",
                    cornerRadius = 1.0f,
                    roadWidth = 2.0f,
                    isClosedLoop = false,
                    sharpPoints = new List<Vector3> { origin, endPoint }
                };
                data.roadSegments.Add(branch);

                // ============================================
                // 3. Automatically spawn a Bus on this branch
                // ============================================
                BusSpawnData newBus = new BusSpawnData
                {
                    busID = $"Bus_{data.buses.Count + 1}",
                    spawnPosition = origin + direction * (branchLength * 0.6f), // Parked backwards in the branch
                    eulerAngles = Quaternion.LookRotation(-direction).eulerAngles, // Bus facing towards the main road
                    busColor = busColors[Random.Range(0, busColors.Length)],
                    capacity = 3
                };
                data.buses.Add(newBus);
            }

            // Mark as dirty to ensure Scriptable Object saves to disk
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"<color=green>✓ Generate Successful!</color> Created Loop Map with {data.roadSegments.Count - 1} parking branches.");
        }
    }
}
