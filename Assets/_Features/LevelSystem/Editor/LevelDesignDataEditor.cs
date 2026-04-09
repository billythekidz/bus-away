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
            // Nút bấm to, rõ ràng
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
            buttonStyle.fixedHeight = 40;

            if (GUILayout.Button("🎲 Generate Random Road Map (Buses Away Style)", buttonStyle))
            {
                GenerateRandomRoadMap();
            }
            
            GUILayout.Space(15);
            
            // Vẽ giao diện mặc định bên dưới
            DrawDefaultInspector();
        }

        private void GenerateRandomRoadMap()
        {
            LevelDesignData data = (LevelDesignData)target;
            Undo.RecordObject(data, "Generate Random Road Map");

            data.roadSegments.Clear();
            data.buses.Clear();
            
            // Định dạng từ Docs/GameDesign: Mạng lưới thường có MỘT NHÁNH ĐƯỜNG CHÍNH (Loop/U-Shape) 
            // và CÁC NGÁCH ĐỖ XE (Parking Branches) vuông góc trỏ ra ngoài.

            // ============================================
            // 1. Tạo Đường Chính (Main Loop hình chữ nhật)
            // ============================================
            float width = Random.Range(6.0f, 10.0f);
            float depth = Random.Range(4.0f, 8.0f);
            
            // Tạo lưới tròn
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
            // 2. Tạo Các Ngách Đỗ Xe Buýt (Parking Branches)
            // ============================================
            int numParkingBranches = Random.Range(4, 9);
            List<Vector3> usedOrigins = new List<Vector3>();
            
            Color[] busColors = new Color[] { Color.red, Color.blue, Color.yellow, Color.green, new Color(0.5f, 0, 0.5f) /* Tím */ };

            for (int i = 0; i < numParkingBranches; i++)
            {
                int edge = Random.Range(0, 4); // 0: Top, 1: Right, 2: Bottom, 3: Left
                Vector3 origin = Vector3.zero;
                Vector3 direction = Vector3.forward;

                // Chọn random 1 điểm trên trục để đâm nhánh ra ngoài
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

                // Chống đè điểm: Quá gần điểm tạo nhánh cũ thì bỏ qua
                bool isOverlap = false;
                foreach(var uo in usedOrigins)
                {
                    if (Vector3.Distance(uo, origin) < 2.5f) { isOverlap = true; break; }
                }
                if (isOverlap) continue;

                usedOrigins.Add(origin);

                float branchLength = Random.Range(3.5f, 5.5f);
                Vector3 endPoint = origin + direction * branchLength;

                // Tạo nhánh đường ghép vuông góc nối vào Main Loop
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
                // 3. Tự động Setup ngẫu nhiên luôn 1 Xe Buýt trên nhánh này
                // ============================================
                BusSpawnData newBus = new BusSpawnData
                {
                    busID = $"Bus_{data.buses.Count + 1}",
                    spawnPosition = origin + direction * (branchLength * 0.6f), // Đỗ lùi vào ngách
                    eulerAngles = Quaternion.LookRotation(-direction).eulerAngles, // Xe quay đầu nhìn ra đường chính
                    busColor = busColors[Random.Range(0, busColors.Length)],
                    capacity = 3
                };
                data.buses.Add(newBus);
            }

            // Gắn cờ Dirty để Scriptable Object lưu xuống đĩa cứng
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"<color=green>✓ Generate Thành Công!</color> Đã tạo Loop Map cùng {data.roadSegments.Count - 1} bãi đỗ xe.");
        }
    }
}
