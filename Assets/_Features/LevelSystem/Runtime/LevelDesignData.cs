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

        HalfT_BusStop_N_Left = 11,      // Nửa trái của trạm Bus (nút giao T) hướng Bắc (chuồng đỗ mở ra hướng Nam)
        HalfT_BusStop_N_Right = 12,     // Nửa phải của trạm Bus (nút giao T) hướng Bắc
        HalfT_BusStop_E_Left = 13,      // Nửa trái của trạm Bus hướng Đông
        HalfT_BusStop_E_Right = 14,     // Nửa phải của trạm Bus hướng Đông
        HalfT_BusStop_S_Left = 15,      // Nửa trái của trạm Bus hướng Nam
        HalfT_BusStop_S_Right = 16,     // Nửa phải của trạm Bus hướng Nam
        HalfT_BusStop_W_Left = 17,      // Nửa trái của trạm Bus hướng Tây
        HalfT_BusStop_W_Right = 18,     // Nửa phải của trạm Bus hướng Tây


        Cross = 19,             // Ngã tư đầy đủ: 4 nhánh thông nhau (Bắc, Nam, Đông, Tây)

        DeadEnd_N = 20,         // Tuyến đường đâm về hướng Bắc. Phần ngõ cụt (bị bịt) nằm ở viền Bắc, cổng kết nối mở ra hướng Nam.
        DeadEnd_E = 21,         // Tuyến đường đâm về hướng Đông. Phần ngõ cụt (bị bịt) nằm ở viền Đông, cổng kết nối mở ra hướng Tây.
        DeadEnd_S = 22,         // Tuyến đường đâm về hướng Nam. Phần ngõ cụt (bị bịt) nằm ở viền Nam, cổng kết nối mở ra hướng Bắc.
        DeadEnd_W = 23,         // Tuyến đường đâm về hướng Tây. Phần ngõ cụt (bị bịt) nằm ở viền Tây, cổng kết nối mở ra hướng Đông.

        // (Loại bỏ các độc lập BusStop vì theo rule mới, ngã ba chữ T chính là trạm xe buýt)

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

    [System.Serializable]
    public class CrowdLandGroup
    {
        public List<CrowdLandConfig> chunks = new List<CrowdLandConfig>();
    }

    [CreateAssetMenu(fileName = "Level_001", menuName = "Bus Away/Level Design Data")]
    public class LevelDesignData : ScriptableObject
    {
        [Header("Grid Config")]
        public int gridWidth = 8;
        public int gridHeight = 5;
        public float tileSize => 1f;

        [Tooltip("If true, prevents S-Curves and Notches from generating T-Junctions and Crosses.")]
        public bool enforcePerfectLoop = true;


        [HideInInspector]
        public RoadCellType[] grid; // Array size: width * height
        [Header("Gameplay Config")]
        [Tooltip("Score target to clear this level")]
        public int levelGoalCoin = 160;

        [Tooltip("Number of bus stops to generate in the map")]
        public int busStopLength = 2;

        // --- Auto-managed by Game Manager, no manual editing needed ---
        [HideInInspector]
        public List<BusSpawnData> buses = new List<BusSpawnData>();

        [HideInInspector]
        public List<Color> passengerQueueOrder = new List<Color>();

        // ── Bus Dispatch Config ──────────────────────────────────────────────────
        [Header("Bus Dispatch Config")]

        /// <summary>
        /// How many buses are assigned to each bus stop on the map.
        /// All bus stops share the same number of buses.
        /// Buses loop the road ring and stop once they've collected enough passengers.
        /// </summary>
        [Tooltip("Number of buses assigned per bus stop. All stops share this value. Buses keep looping until they collect enough same-color passengers, then park at the bus stop and disappear.")]
        public int busesPerStop = 1;

        /// <summary>
        /// Minimum number of same-color passengers a bus must collect before it
        /// stops looping the road ring and drives into the bus stop.
        /// This value also determines the total agent pool per color:
        ///   resolvedLands agentCount = (number of buses of that color) × agentsPerBus
        /// Default: 32.
        /// </summary>
        [Tooltip("Number of same-color passengers a bus must board before it exits the loop and parks at the bus stop. Also drives crowd land sizing: agentCount per color = busCount × agentsPerBus.")]
        public int agentsPerBus = 32;

        [Header("Crowd Lands")]
        [HideInInspector]
        public int landCount = 3;



        [HideInInspector]
        public List<Color> landColorPalette = new List<Color>();

        [HideInInspector]
        public List<CrowdLandGroup> resolvedLands = new List<CrowdLandGroup>();

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
