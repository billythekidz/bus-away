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

        [Header("Prefabs & Materials")]
        public Material roadCoreMaterial;
        public Material roadBorderMaterial;
        public GameObject busPrefab; // 3D Bus Placeholder

        [Header("Auto Framing")]
        public bool autoFrameCameraOnBuild = true;
        public LevelCameraFramer cameraFramer;

        [ContextMenu("Build Level From Data")]
        public void BuildLevel()
        {
#if UNITY_EDITOR
            if (roadCoreMaterial == null)
            {
                roadCoreMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                roadCoreMaterial.color = new Color(0.2f, 0.22f, 0.28f, 1f); // Solid dark asphalt
            }
            if (roadBorderMaterial == null)
            {
                roadBorderMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                roadBorderMaterial.color = new Color(1f, 1f, 1f, 1f); // White border
            }
#endif

            if (activeLevelData == null)
            {
                Debug.LogError("LevelDesignData is not assigned!");
                return;
            }

            ClearOldLevel();

            // 0. Build Ground Environment (For 3D Prototype Shadows)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Environment_Ground";
            ground.transform.SetParent(this.transform);
            ground.transform.position = new Vector3(0, -0.1f, 0);
            ground.transform.localScale = new Vector3(10, 1, 10); // 100x100 area
            
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.6f, 0.75f, 0.6f); // Soft green grass / prototype surface
            ground.GetComponent<Renderer>().material = groundMat;

            // 1. Build Roads
            GameObject roadRoot = new GameObject("RoadsRoot");
            roadRoot.transform.SetParent(this.transform);

            foreach (var roadDesign in activeLevelData.roads)
            {
                GameObject roadGo = new GameObject($"Road_{roadDesign.id}");
                roadGo.transform.SetParent(roadRoot.transform);

                DynamicRoad dynamicRoad = roadGo.AddComponent<DynamicRoad>();
                dynamicRoad.roadData = roadDesign;
                dynamicRoad.roadWidth = 1.0f; // Default grid width
                dynamicRoad.coreMaterial = roadCoreMaterial;
                dynamicRoad.borderMaterial = roadBorderMaterial;

                // Build Geometry
                dynamicRoad.GenerateRoad();
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
                    // Fallback to Cube if no prefab is assigned
                    busObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    busObj.transform.SetParent(busRoot.transform);
                    busObj.transform.localScale = new Vector3(1.5f, 1.5f, 3.0f); // Approximate bus size

                    // Set fallback color
                    var renderer = busObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        mat.color = busData.busColor;
                        renderer.material = mat;
                    }
                }

                busObj.name = busData.busID;
                busObj.transform.position = busData.spawnPosition;
                busObj.transform.eulerAngles = busData.eulerAngles;
            }
            
            Debug.Log($"<color=cyan>Level Build Complete!</color> Generated {activeLevelData.roads.Count} roads and {activeLevelData.buses.Count} buses.");

            // 3. Auto-Frame Camera
            if (autoFrameCameraOnBuild)
            {
                if (cameraFramer == null) cameraFramer = GetComponent<LevelCameraFramer>();
                if (cameraFramer == null) cameraFramer = gameObject.AddComponent<LevelCameraFramer>();
                
                cameraFramer.FrameLevel(roadRoot.transform);
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
