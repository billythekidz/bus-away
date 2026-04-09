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
        public GameObject busPrefab; // Placeholder cho xe buýt 3D

        [ContextMenu("Build Level From Data")]
        public void BuildLevel()
        {
            if (activeLevelData == null)
            {
                Debug.LogError("Chưa gán LevelDesignData!");
                return;
            }

            ClearOldLevel();

            // 1. Build Roads
            GameObject roadRoot = new GameObject("RoadsRoot");
            roadRoot.transform.SetParent(this.transform);

            foreach (var segment in activeLevelData.roadSegments)
            {
                GameObject roadGo = new GameObject(segment.segmentName);
                roadGo.transform.SetParent(roadRoot.transform);
                
                DynamicRoad dynRoad = roadGo.AddComponent<DynamicRoad>();
                dynRoad.roadMaterial = roadCoreMaterial;
                dynRoad.borderMaterial = roadBorderMaterial;

                // Sync data
                dynRoad.SetupFromData(segment);
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
                    // Fallback bằng Cube nếu chưa có prefab
                    busObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    busObj.transform.SetParent(busRoot.transform);
                    busObj.transform.localScale = new Vector3(1.5f, 1.5f, 3.0f); // Kích thước tựa xe buýt

                    // Set màu fallback
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
            
            Debug.Log($"<color=cyan>Level Build Hoàn Tất!</color> Đã tạo {activeLevelData.roadSegments.Count} đường và {activeLevelData.buses.Count} xe.");
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
