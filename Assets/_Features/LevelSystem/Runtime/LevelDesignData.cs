using System.Collections.Generic;
using UnityEngine;

namespace BusAway.Level
{
    public enum RoadCellType
    {
        Empty = 0,              // Ô trống, không có đường hay công trình nào

        Straight_NS = 1,        // Đường thẳng dọc (Bắc-Nam), xe di chuyển theo trục Z
        Straight_EW = 2,        // Đường thẳng ngang (Đông-Tây), xe di chuyển theo trục X

        Corner_NE = 3,          // Góc cua Bắc-Đông: đường rẽ từ hướng Nam lên rồi sang Đông (phải)
        Corner_NW = 4,          // Góc cua Bắc-Tây: đường rẽ từ hướng Nam lên rồi sang Tây (trái)
        Corner_SE = 5,          // Góc cua Nam-Đông: đường rẽ từ hướng Bắc xuống rồi sang Đông (phải)
        Corner_SW = 6,          // Góc cua Nam-Tây: đường rẽ từ hướng Bắc xuống rồi sang Tây (trái)

        InnerCorner_NE = 7,     // Góc trong Bắc-Đông: phần bo góc viền nội tuyến hướng NE (dùng cho đường vòng)
        InnerCorner_NW = 8,     // Góc trong Bắc-Tây: phần bo góc viền nội tuyến hướng NW
        InnerCorner_SE = 9,     // Góc trong Nam-Đông: phần bo góc viền nội tuyến hướng SE
        InnerCorner_SW = 10,    // Góc trong Nam-Tây: phần bo góc viền nội tuyến hướng SW

        TJunction_N = 11,       // Ngã ba hướng Bắc: 3 nhánh (Đông, Tây, Bắc), chặn hướng Nam
        TJunction_E = 12,       // Ngã ba hướng Đông: 3 nhánh (Bắc, Nam, Đông), chặn hướng Tây
        TJunction_S = 13,       // Ngã ba hướng Nam: 3 nhánh (Đông, Tây, Nam), chặn hướng Bắc
        TJunction_W = 14,       // Ngã ba hướng Tây: 3 nhánh (Bắc, Nam, Tây), chặn hướng Đông

        Cross = 15,             // Ngã tư đầy đủ: 4 nhánh thông nhau (Bắc, Nam, Đông, Tây)

        DeadEnd_N = 16,         // Đường cụt hướng Bắc: chỉ có 1 lối vào từ phía Nam, đầu kia bịt kín
        DeadEnd_E = 17,         // Đường cụt hướng Đông: chỉ có 1 lối vào từ phía Tây
        DeadEnd_S = 18,         // Đường cụt hướng Nam: chỉ có 1 lối vào từ phía Bắc
        DeadEnd_W = 19,         // Đường cụt hướng Tây: chỉ có 1 lối vào từ phía Đông

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
