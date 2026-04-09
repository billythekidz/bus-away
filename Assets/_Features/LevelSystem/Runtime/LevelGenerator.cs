using UnityEngine;
using BusAway.Level;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BusAway.Gameplay
{
    [ExecuteAlways]
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Data Source")]
        public LevelDesignData activeLevelData;

        [Header("Grid Tiles & Materials (If empty, uses Primitives)")]
        public GameObject tileStraightPrefab;
        public GameObject tileCornerPrefab;
        public GameObject tileTJunctionPrefab;
        public GameObject tileCrossPrefab;
        
        [Header("Buses")]
        public GameObject busPrefab;

        [Header("Auto Framing")]
        public bool autoFrameCameraOnBuild = true;
        public LevelCameraFramer cameraFramer;

        [ContextMenu("Build Level From Data")]
        public void BuildLevel()
        {
            if (activeLevelData == null)
            {
                Debug.LogError("LevelDesignData is not assigned!");
                return;
            }

            ClearOldLevel();

            // 0. Build Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Environment_Ground";
            ground.transform.SetParent(this.transform);
            ground.transform.position = new Vector3(0, -0.1f, 0);
            ground.transform.localScale = new Vector3(10, 1, 10);
            
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.6f, 0.75f, 0.6f);
            ground.GetComponent<Renderer>().material = groundMat;

            // 1. Build Roads (Autotiling)
            GameObject roadRoot = new GameObject("RoadsRoot");
            roadRoot.transform.SetParent(this.transform);

            float tSize = activeLevelData.tileSize;
            float offsetX = -(activeLevelData.gridWidth * tSize) / 2f + (tSize / 2f);
            float offsetZ = -(activeLevelData.gridHeight * tSize) / 2f + (tSize / 2f);

            Material roadCoreMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.2f, 0.22f, 0.28f, 1f) };
            Material roadBorderMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };

            for (int y = 0; y < activeLevelData.gridHeight; y++)
            {
                for (int x = 0; x < activeLevelData.gridWidth; x++)
                {
                    RoadCellType cell = activeLevelData.GetCell(x, y);
                    if (cell == RoadCellType.Empty) continue;

                    // Evaluate neighbors
                    bool n = HasRoad(x, y + 1);
                    bool sn = HasRoad(x, y - 1);
                    bool e = HasRoad(x + 1, y);
                    bool w = HasRoad(x - 1, y);

                    int mask = (n ? 1 : 0) | (e ? 2 : 0) | (sn ? 4 : 0) | (w ? 8 : 0);

                    GameObject prefabTemplate = tileStraightPrefab;
                    float rotY = 0f;
                    string shapeTypeStr = "Straight";

                    switch (mask)
                    {
                        // Straights / Dead Ends
                        case 0:
                        case 1: // N
                        case 4: // S
                        case 5: // N+S
                            prefabTemplate = tileStraightPrefab; rotY = 0f; shapeTypeStr = "Straight"; break;
                        case 2: // E
                        case 8: // W
                        case 10: // E+W
                            prefabTemplate = tileStraightPrefab; rotY = 90f; shapeTypeStr = "Straight"; break;

                        // Corners 
                        case 6: // S+E
                            prefabTemplate = tileCornerPrefab; rotY = 0f; shapeTypeStr = "Corner"; break;
                        case 3: // N+E
                            prefabTemplate = tileCornerPrefab; rotY = -90f; shapeTypeStr = "Corner"; break;
                        case 9: // N+W
                            prefabTemplate = tileCornerPrefab; rotY = 180f; shapeTypeStr = "Corner"; break;
                        case 12: // S+W
                            prefabTemplate = tileCornerPrefab; rotY = 90f; shapeTypeStr = "Corner"; break;

                        // T-Junctions
                        case 7: // N+E+S
                            prefabTemplate = tileTJunctionPrefab; rotY = 0f; shapeTypeStr = "TJunction"; break;
                        case 14: // E+S+W
                            prefabTemplate = tileTJunctionPrefab; rotY = 90f; shapeTypeStr = "TJunction"; break;
                        case 13: // S+W+N
                            prefabTemplate = tileTJunctionPrefab; rotY = 180f; shapeTypeStr = "TJunction"; break;
                        case 11: // W+N+E
                            prefabTemplate = tileTJunctionPrefab; rotY = -90f; shapeTypeStr = "TJunction"; break;

                        // Cross
                        case 15: // All
                            prefabTemplate = tileCrossPrefab; rotY = 0f; shapeTypeStr = "Cross"; break;
                    }

                    Vector3 pos = new Vector3(offsetX + x * tSize, 0, offsetZ + y * tSize);
                    GameObject tileObj;

                    if (prefabTemplate != null)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying) tileObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabTemplate, roadRoot.transform);
                        else tileObj = Instantiate(prefabTemplate, roadRoot.transform);
#else
                        tileObj = Instantiate(prefabTemplate, roadRoot.transform);
#endif
                        tileObj.transform.position = pos;
                        tileObj.transform.eulerAngles = new Vector3(0, rotY, 0);
                        tileObj.name = $"Tile_{x}_{y}";
                    }
                    else
                    {
                        // ----- FALLBACK GENERATION USING UNITY PRIMITIVES -----
                        tileObj = new GameObject($"Tile_{x}_{y}_Fallback_{shapeTypeStr}");
                        tileObj.transform.SetParent(roadRoot.transform);
                        tileObj.transform.position = pos;
                        tileObj.transform.eulerAngles = new Vector3(0, rotY, 0);

                        float asphaltWidth = tSize * 0.75f;
                        float offsetH = 0.05f;

                        // Border Base
                        GameObject bBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        bBase.name = "BorderBase";
                        bBase.transform.SetParent(tileObj.transform, false);
                        bBase.transform.localScale = new Vector3(tSize, 0.1f, tSize);
                        Object.DestroyImmediate(bBase.GetComponent<Collider>());
                        bBase.GetComponent<Renderer>().material = roadBorderMaterial;

                        Renderer[] asphalts = null;

                        if (shapeTypeStr == "Straight")
                        {
                            // N + S Asphalt
                            GameObject a1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a1.transform.SetParent(tileObj.transform, false);
                            a1.transform.localPosition = new Vector3(0, offsetH, 0);
                            a1.transform.localScale = new Vector3(asphaltWidth, 0.11f, tSize + 0.01f);
                            asphalts = new Renderer[] { a1.GetComponent<Renderer>() };
                        }
                        else if (shapeTypeStr == "Corner")
                        {
                            GameObject a1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a1.transform.SetParent(tileObj.transform, false);
                            a1.transform.localPosition = new Vector3(0, offsetH, tSize/4f);
                            a1.transform.localScale = new Vector3(asphaltWidth, 0.11f, tSize/2f + 0.01f);

                            GameObject a2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a2.transform.SetParent(tileObj.transform, false);
                            a2.transform.localPosition = new Vector3(tSize/4f, offsetH, 0);
                            a2.transform.localScale = new Vector3(tSize/2f + 0.01f, 0.11f, asphaltWidth);
                            asphalts = new Renderer[] { a1.GetComponent<Renderer>(), a2.GetComponent<Renderer>() };
                        }
                        else if (shapeTypeStr == "TJunction")
                        {
                            GameObject a1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a1.transform.SetParent(tileObj.transform, false);
                            a1.transform.localPosition = new Vector3(0, offsetH, 0);
                            a1.transform.localScale = new Vector3(asphaltWidth, 0.11f, tSize + 0.01f);

                            GameObject a2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a2.transform.SetParent(tileObj.transform, false);
                            a2.transform.localPosition = new Vector3(tSize/4f, offsetH, 0);
                            a2.transform.localScale = new Vector3(tSize/2f + 0.01f, 0.11f, asphaltWidth);
                            
                            asphalts = new Renderer[] { a1.GetComponent<Renderer>(), a2.GetComponent<Renderer>() };
                        }
                        else if (shapeTypeStr == "Cross")
                        {
                            GameObject a1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a1.transform.SetParent(tileObj.transform, false);
                            a1.transform.localPosition = new Vector3(0, offsetH, 0);
                            a1.transform.localScale = new Vector3(asphaltWidth, 0.11f, tSize + 0.01f);

                            GameObject a2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a2.transform.SetParent(tileObj.transform, false);
                            a2.transform.localPosition = new Vector3(0, offsetH, 0);
                            a2.transform.localScale = new Vector3(tSize + 0.01f, 0.11f, asphaltWidth);

                            asphalts = new Renderer[] { a1.GetComponent<Renderer>(), a2.GetComponent<Renderer>() };
                        }

                        if (asphalts != null)
                        {
                            foreach (var r in asphalts)
                            {
                                r.gameObject.name = "Asphalt";
                                Object.DestroyImmediate(r.GetComponent<Collider>());
                                r.material = roadCoreMaterial;
                            }
                        }
                    }
                }
            }

            // 2. Build Buses
            GameObject busRoot = new GameObject("BusesRoot");
            busRoot.transform.SetParent(this.transform);

            foreach (var busData in activeLevelData.buses)
            {
                GameObject busObj;
                
                if (busPrefab != null) {
#if UNITY_EDITOR
                    if (!Application.isPlaying) busObj = (GameObject)PrefabUtility.InstantiatePrefab(busPrefab, busRoot.transform);
                    else busObj = Instantiate(busPrefab, busRoot.transform);
#else
                    busObj = Instantiate(busPrefab, busRoot.transform);
#endif
                }
                else 
                {
                    busObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    busObj.transform.SetParent(busRoot.transform);
                    busObj.transform.localScale = new Vector3(1.5f, 1.5f, 3.0f); 

                    var renderer = busObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        mat.color = busData.busColor;
                        renderer.material = mat;
                    }
                }

                busObj.name = busData.busID;
                Vector3 busPos = new Vector3(offsetX + busData.gridX * tSize, 0, offsetZ + busData.gridY * tSize);
                busObj.transform.position = busPos + Vector3.up * 0.5f;
                busObj.transform.eulerAngles = busData.eulerAngles;
            }
            
            Debug.Log($"<color=cyan>Level Build Complete!</color> Autotiled grid.");

            // 3. Auto-Frame Camera
            if (autoFrameCameraOnBuild)
            {
                if (cameraFramer == null) cameraFramer = GetComponent<LevelCameraFramer>();
                if (cameraFramer == null) cameraFramer = gameObject.AddComponent<LevelCameraFramer>();
                
                cameraFramer.FrameLevel(roadRoot.transform);
            }
        }

        private bool HasRoad(int x, int y)
        {
            var cell = activeLevelData.GetCell(x, y);
            return cell == RoadCellType.Road || cell == RoadCellType.BusStop;
        }

        [ContextMenu("Clear Map")]
        public void ClearOldLevel()
        {
            Transform[] children = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++) children[i] = transform.GetChild(i);
            
            foreach (var t in children)
            {
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
            }
        }
    }
}
