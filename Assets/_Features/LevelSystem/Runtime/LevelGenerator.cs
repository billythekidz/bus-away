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
        [Header("Straight Roads")]
        public GameObject straightNS;
        public GameObject straightEW;

        [Header("Corners")]
        public GameObject cornerSE;
        public GameObject cornerNE;
        public GameObject cornerNW;
        public GameObject cornerSW;

        [Header("Bus Stops (Half T-Junctions)")]
        public GameObject halfT_N_Left;
        public GameObject halfT_N_Right;
        public GameObject halfT_E_Left;
        public GameObject halfT_E_Right;
        public GameObject halfT_S_Left;
        public GameObject halfT_S_Right;
        public GameObject halfT_W_Left;
        public GameObject halfT_W_Right;

        [Header("Bus Stop Props (Canopy/Sign)")]
        [Tooltip("Place this prefab on North T-junctions. Parking slot is at the North edge; the entrance/door faces South toward the street.")]
        public GameObject busStopPropN;
        [Tooltip("Place this prefab on East T-junctions. Parking slot is at the East edge; the entrance/door faces West toward the street.")]
        public GameObject busStopPropE;
        [Tooltip("Place this prefab on South T-junctions. Parking slot is at the South edge; the entrance/door faces North toward the street.")]
        public GameObject busStopPropS;
        [Tooltip("Place this prefab on West T-junctions. Parking slot is at the West edge; the entrance/door faces East toward the street.")]
        public GameObject busStopPropW;

        [Header("Cross, Generic")]
        public GameObject crossRoad;
        public GameObject genericRoad;

        [Header("Dead Ends")]
        public GameObject deadEndN;
        public GameObject deadEndE;
        public GameObject deadEndS;
        public GameObject deadEndW;



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
            // Camera is rotated 180° on Y-axis, so we flip both X and Z
            // so that grid[x,y] visually maps: x+ = screen-right, y+ = screen-up
            float offsetX = (activeLevelData.gridWidth * tSize) / 2f - (tSize / 2f);
            float offsetZ = (activeLevelData.gridHeight * tSize) / 2f - (tSize / 2f);
            const float camFlip = 180f; // Add to all rotY values to align with flipped camera

            Material roadCoreMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.2f, 0.22f, 0.28f, 1f) };
            Material roadBorderMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };

            for (int y = 0; y < activeLevelData.gridHeight; y++)
            {
                for (int x = 0; x < activeLevelData.gridWidth; x++)
                {
                    RoadCellType cell = activeLevelData.GetCell(x, y);
                    if (cell == RoadCellType.Empty) continue;

                    GameObject prefabTemplate = null;
                    GameObject busStopPropTemplate = null;
                    float rotYFallback = 0f;
                    string shapeTypeStr = "Straight";

                    switch (cell)
                    {
                        case RoadCellType.Straight_NS: prefabTemplate = straightNS; rotYFallback = camFlip + 0f; shapeTypeStr = "Straight"; break;
                        case RoadCellType.Straight_EW: prefabTemplate = straightEW; rotYFallback = camFlip + 90f; shapeTypeStr = "Straight"; break;

                        case RoadCellType.Corner_SE: prefabTemplate = cornerSE; rotYFallback = camFlip + 0f; shapeTypeStr = "Corner"; break;
                        case RoadCellType.Corner_NE: prefabTemplate = cornerNE; rotYFallback = camFlip - 90f; shapeTypeStr = "Corner"; break;
                        case RoadCellType.Corner_NW: prefabTemplate = cornerNW; rotYFallback = camFlip + 180f; shapeTypeStr = "Corner"; break;
                        case RoadCellType.Corner_SW: prefabTemplate = cornerSW; rotYFallback = camFlip + 90f; shapeTypeStr = "Corner"; break;
                        case RoadCellType.HalfT_BusStop_N_Left: prefabTemplate = halfT_N_Left; busStopPropTemplate = busStopPropN; rotYFallback = camFlip + 0f; shapeTypeStr = "HalfT_BusStop"; break;
                        case RoadCellType.HalfT_BusStop_N_Right: prefabTemplate = halfT_N_Right; busStopPropTemplate = busStopPropN; rotYFallback = camFlip + 0f; shapeTypeStr = "HalfT_BusStop"; break;
                        case RoadCellType.HalfT_BusStop_E_Left: prefabTemplate = halfT_E_Left; busStopPropTemplate = busStopPropE; rotYFallback = camFlip + 90f; shapeTypeStr = "HalfT_BusStop"; break;
                        case RoadCellType.HalfT_BusStop_E_Right: prefabTemplate = halfT_E_Right; busStopPropTemplate = busStopPropE; rotYFallback = camFlip + 90f; shapeTypeStr = "HalfT_BusStop"; break;
                        case RoadCellType.HalfT_BusStop_S_Left: prefabTemplate = halfT_S_Left; busStopPropTemplate = busStopPropS; rotYFallback = camFlip + 180f; shapeTypeStr = "HalfT_BusStop"; break;
                        case RoadCellType.HalfT_BusStop_S_Right: prefabTemplate = halfT_S_Right; busStopPropTemplate = busStopPropS; rotYFallback = camFlip + 180f; shapeTypeStr = "HalfT_BusStop"; break;
                        case RoadCellType.HalfT_BusStop_W_Left: prefabTemplate = halfT_W_Left; busStopPropTemplate = busStopPropW; rotYFallback = camFlip - 90f; shapeTypeStr = "HalfT_BusStop"; break;
                        case RoadCellType.HalfT_BusStop_W_Right: prefabTemplate = halfT_W_Right; busStopPropTemplate = busStopPropW; rotYFallback = camFlip - 90f; shapeTypeStr = "HalfT_BusStop"; break;

                        case RoadCellType.Cross: prefabTemplate = crossRoad; rotYFallback = camFlip; shapeTypeStr = "Cross"; break;

                        case RoadCellType.DeadEnd_N: prefabTemplate = deadEndN; rotYFallback = camFlip + 0f; shapeTypeStr = "DeadEnd"; break;
                        case RoadCellType.DeadEnd_E: prefabTemplate = deadEndE; rotYFallback = camFlip + 90f; shapeTypeStr = "DeadEnd"; break;
                        case RoadCellType.DeadEnd_S: prefabTemplate = deadEndS; rotYFallback = camFlip + 180f; shapeTypeStr = "DeadEnd"; break;
                        case RoadCellType.DeadEnd_W: prefabTemplate = deadEndW; rotYFallback = camFlip - 90f; shapeTypeStr = "DeadEnd"; break;

                        case RoadCellType.GenericRoad: prefabTemplate = genericRoad; rotYFallback = camFlip; shapeTypeStr = "Straight"; break;
                    }

                    // Flip X and Z to match camera's 180° Y rotation
                    Vector3 pos = new Vector3(offsetX - x * tSize, 0, offsetZ - y * tSize);
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
                        // NO ROTATION OVERRIDE: Prefab retains its own saved rotation
                        tileObj.name = $"Tile_{x}_{y}_{shapeTypeStr}";

                        // Nếu tile này cần spawn thêm canopy (Bus Stop prop) và có gán prefab
                        if (busStopPropTemplate != null)
                        {
                            // Chỉ spawn 1 lần cho cặp Bus Stop bằng cách check "_Left"
                            if (cell.ToString().Contains("_Left"))
                            {
                                GameObject propObj;
#if UNITY_EDITOR
                                if (!Application.isPlaying) propObj = (GameObject)PrefabUtility.InstantiatePrefab(busStopPropTemplate, roadRoot.transform);
                                else propObj = Instantiate(busStopPropTemplate, roadRoot.transform);
#else
                            propObj = Instantiate(busStopPropTemplate, roadRoot.transform);
#endif
                                Vector3 rightPos = pos;
                                Vector3 branchOffsetWorld = Vector3.zero;

                                if (cell == RoadCellType.HalfT_BusStop_N_Left)
                                {
                                    rightPos = new Vector3(offsetX - (x + 1) * tSize, 0, offsetZ - y * tSize); // Right is x+1
                                    branchOffsetWorld = new Vector3(0, 0, -tSize); // Branch y+1 (World -Z)
                                }
                                else if (cell == RoadCellType.HalfT_BusStop_E_Left)
                                {
                                    rightPos = new Vector3(offsetX - x * tSize, 0, offsetZ - (y - 1) * tSize); // Right is y-1
                                    branchOffsetWorld = new Vector3(-tSize, 0, 0); // Branch x+1 (World -X)
                                }
                                else if (cell == RoadCellType.HalfT_BusStop_W_Left)
                                {
                                    rightPos = new Vector3(offsetX - x * tSize, 0, offsetZ - (y + 1) * tSize); // Right is y+1
                                    branchOffsetWorld = new Vector3(tSize, 0, 0); // Branch x-1 (World +X)
                                }
                                else if (cell == RoadCellType.HalfT_BusStop_S_Left)
                                {
                                    rightPos = new Vector3(offsetX - (x - 1) * tSize, 0, offsetZ - y * tSize); // Right is x-1
                                    branchOffsetWorld = new Vector3(0, 0, tSize); // Branch y-1 (World +Z)
                                }

                                Vector3 centerPos = (pos + rightPos) / 2f;
                                propObj.transform.position = centerPos + branchOffsetWorld;

                                propObj.transform.rotation = busStopPropTemplate.transform.rotation;

                                // Scale as requested
                                Vector3 origScale = busStopPropTemplate.transform.localScale;
                                propObj.transform.localScale = new Vector3(origScale.x * 2f, origScale.y * 1f, origScale.z * 1.25f);

                                // Đặt tên
                                propObj.name = $"BusStop_Prop_{x}_{y}";
                            }
                        }
                    }
                    else
                    {
                        // ----- FALLBACK GENERATION USING UNITY PRIMITIVES -----
                        tileObj = new GameObject($"Tile_{x}_{y}_Fallback_{shapeTypeStr}");
                        tileObj.transform.SetParent(roadRoot.transform);
                        tileObj.transform.position = pos;
                        tileObj.transform.eulerAngles = new Vector3(0, rotYFallback, 0);

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
                            a1.transform.localPosition = new Vector3(0, offsetH, tSize / 4f);
                            a1.transform.localScale = new Vector3(asphaltWidth, 0.11f, tSize / 2f + 0.01f);

                            GameObject a2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a2.transform.SetParent(tileObj.transform, false);
                            a2.transform.localPosition = new Vector3(tSize / 4f, offsetH, 0);
                            a2.transform.localScale = new Vector3(tSize / 2f + 0.01f, 0.11f, asphaltWidth);
                            asphalts = new Renderer[] { a1.GetComponent<Renderer>(), a2.GetComponent<Renderer>() };
                        }
                        else if (shapeTypeStr == "HalfT")
                        {
                            GameObject a1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a1.transform.SetParent(tileObj.transform, false);
                            a1.transform.localPosition = new Vector3(0, offsetH, 0);
                            a1.transform.localScale = new Vector3(asphaltWidth, 0.11f, tSize + 0.01f);

                            GameObject a2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            a2.transform.SetParent(tileObj.transform, false);
                            a2.transform.localPosition = new Vector3(tSize / 4f, offsetH, 0);
                            a2.transform.localScale = new Vector3(tSize / 2f + 0.01f, 0.11f, asphaltWidth);

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

                if (busPrefab != null)
                {
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

            // 3. Build Crowd Lands
            BuildCrowdLands();

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
            return cell != RoadCellType.Empty;
        }

        private void BuildCrowdLands()
        {
            if (activeLevelData == null) return;

            int landCount = Random.Range(activeLevelData.minLandCount, activeLevelData.maxLandCount + 1);

            var palette = new System.Collections.Generic.List<Color>(activeLevelData.landColorPalette);
            ShuffleList(palette);

            activeLevelData.resolvedLands.Clear();

            int totalAgents = 0;
            for (int i = 0; i < landCount; i++)
            {
                int minRows = activeLevelData.minAgentsPerLand / 4;
                int maxRows = activeLevelData.maxAgentsPerLand / 4;
                int rows = Random.Range(minRows, maxRows + 1);
                int count = rows * 4;

                var landCfg = new CrowdLandConfig
                {
                    agentCount = count,
                    color = (i < palette.Count) ? palette[i] : Color.white
                };
                activeLevelData.resolvedLands.Add(landCfg);
                totalAgents += count;

                if (BusAway.CrowdSystem.CrowdManager.Instance != null)
                {
                    BusAway.CrowdSystem.CrowdManager.Instance.SpawnLand(i, count, landCfg.color);
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(activeLevelData);
#endif

            Debug.Log($"<color=lime>CrowdLands Built:</color> {landCount} lands, total agents = {totalAgents}");
        }

        private void ShuffleList<T>(System.Collections.Generic.List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
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
