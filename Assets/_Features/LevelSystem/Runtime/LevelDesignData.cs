using System.Collections.Generic;
using UnityEngine;

namespace BusAway.Level
{
    public enum RoadCellType
    {
        Empty = 0,              // Ô trống, không có đường hay công trình nào

        Straight_NS = 1,        // Đường thẳng dọc (Bắc-Nam), xe di chuyển theo trục Z
        Straight_EW = 2,        // Đường thẳng ngang (Đông-Tây), xe di chuyển theo trục X

        Corner_NE = 3,          // Góc cua Bắc-Đông: Nối đường từ phía Bắc (trên) sang phía Đông (phải)
        Corner_NW = 4,          // Góc cua Bắc-Tây: Nối đường từ phía Bắc (trên) sang phía Tây (trái)
        Corner_SE = 5,          // Góc cua Nam-Đông: Nối đường từ phía Nam (dưới) sang phía Đông (phải)
        Corner_SW = 6,          // Góc cua Nam-Tây: Nối đường từ phía Nam (dưới) sang phía Tây (trái)

        HalfT_N_Left = 11,      // Nửa trái của nút giao T hướng Bắc (cổng bus phía Bắc)
        HalfT_N_Right = 12,     // Nửa phải của nút giao T hướng Bắc
        HalfT_E_Left = 13,      // Nửa trái của nút giao T hướng Đông
        HalfT_E_Right = 14,     // Nửa phải của nút giao T hướng Đông
        HalfT_S_Left = 15,      // Nửa trái của nút giao T hướng Nam
        HalfT_S_Right = 16,     // Nửa phải của nút giao T hướng Nam
        HalfT_W_Left = 17,      // Nửa trái của nút giao T hướng Tây
        HalfT_W_Right = 18,     // Nửa phải của nút giao T hướng Tây
        
        Cross = 19,             // Ngã tư đầy đủ: 4 nhánh thông nhau (Bắc, Nam, Đông, Tây)

        DeadEnd_N = 20,         // Tuyến đường đâm về hướng Bắc. Phần ngõ cụt (bị bịt) nằm ở viền Bắc, cổng kết nối mở ra hướng Nam.
        DeadEnd_E = 21,         // Tuyến đường đâm về hướng Đông. Phần ngõ cụt (bị bịt) nằm ở viền Đông, cổng kết nối mở ra hướng Tây.
        DeadEnd_S = 22,         // Tuyến đường đâm về hướng Nam. Phần ngõ cụt (bị bịt) nằm ở viền Nam, cổng kết nối mở ra hướng Bắc.
        DeadEnd_W = 23,         // Tuyến đường đâm về hướng Tây. Phần ngõ cụt (bị bịt) nằm ở viền Tây, cổng kết nối mở ra hướng Đông.

        // Trạm xe buýt / Gara đỗ xe: mỗi trạm chiếm 2 ô liên tiếp (Phần 1 + Phần 2).
        // Index đặt theo cặp liền kề theo từng hướng xoay để dễ mapping và lập trình.
        BusStop_1_N = 31,       // Phần đầu trạm, hướng Bắc (mở cửa về phía Bắc). Đặt ô này trước, ô BusStop_2_N kế tiếp theo sau
        BusStop_2_N = 32,       // Phần đuôi trạm, hướng Bắc. Luôn đặt ngay sau BusStop_1_N theo hướng Bắc

        BusStop_1_E = 33,       // Phần đầu trạm, hướng Đông (mở cửa về phía Đông)
        BusStop_2_E = 34,       // Phần đuôi trạm, hướng Đông. Luôn đặt ngay sau BusStop_1_E theo hướng Đông

        BusStop_1_S = 35,       // Phần đầu trạm, hướng Nam (mở cửa về phía Nam)
        BusStop_2_S = 36,       // Phần đuôi trạm, hướng Nam. Luôn đặt ngay sau BusStop_1_S theo hướng Nam

        BusStop_1_W = 37,       // Phần đầu trạm, hướng Tây (mở cửa về phía Tây)
        BusStop_2_W = 38,       // Phần đuôi trạm, hướng Tây. Luôn đặt ngay sau BusStop_1_W theo hướng Tây

        // Các type tổng quát, dùng khi chưa xác định loại cụ thể hoặc để test nhanh
        GenericRoad = 99,       // Đường generic, chưa phân loại hướng. Dùng tạm trong quá trình thiết kế
    }

    [System.Serializable]
    public class BusSpawnData
    {
        public string busID;

        [Tooltip("Grid X coordinate for spawn")]
        public int gridX;
        [Tooltip("Grid Y coordinate for spawn")]
        public int gridY;

        [Tooltip("Euler angles (rotation) for the bus at spawn")]
        public Vector3 eulerAngles;

        [Tooltip("Number of passengers this bus can carry")]
        public int capacity = 3;

        [Tooltip("Color that identifies this bus and its matching passengers")]
        public Color busColor = Color.white;
    }

    [System.Serializable]
    public class CrowdLandConfig
    {
        public int agentCount;   // Must be multiple of 4
        public Color color;
    }

    [CreateAssetMenu(fileName = "Level_001", menuName = "Bus Away/Level Design Data")]
    public class LevelDesignData : ScriptableObject
    {
        [Header("Grid Config")]
        public int gridWidth = 8;
        public int gridHeight = 5;
        public float tileSize => 1f;


        [HideInInspector]
        public RoadCellType[] grid; // Array size: width * height
        [Header("Gameplay Config")]
        [Tooltip("Score target to clear this level")]
        public int levelGoalCoin = 160;

        [Header("Buses")]
        public List<BusSpawnData> buses = new List<BusSpawnData>();

        [Header("Passenger Queue")]
        [Tooltip("Ordered list of passenger colors to spawn during gameplay")]
        public List<Color> passengerQueueOrder = new List<Color>();

        [Header("Crowd Lands")]
        [HideInInspector]
        public int minLandCount = 2;

        [HideInInspector]
        public int maxLandCount = 4;

        [HideInInspector]
        public int minAgentsPerLand = 8;

        [HideInInspector]
        public int maxAgentsPerLand = 24;

        [HideInInspector]
        public List<Color> landColorPalette = new List<Color>();

        [HideInInspector]
        public List<CrowdLandConfig> resolvedLands = new List<CrowdLandConfig>();

        public RoadCellType GetCell(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return RoadCellType.Empty;
            if (grid == null || grid.Length != gridWidth * gridHeight) return RoadCellType.Empty;
            return grid[y * gridWidth + x];
        }

        public void SetCell(int x, int y, RoadCellType type)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            if (grid == null || grid.Length != gridWidth * gridHeight)
            {
                grid = new RoadCellType[gridWidth * gridHeight];
            }
            grid[y * gridWidth + x] = type;
        }
    }
}
