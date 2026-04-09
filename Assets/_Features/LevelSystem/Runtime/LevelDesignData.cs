using System.Collections.Generic;
using UnityEngine;

namespace BusAway.Level
{
    public enum RoadCellType
    {
        Empty = 0,
        
        Straight_NS = 1,
        Straight_EW = 2,
        
        Corner_NE = 3,
        Corner_NW = 4,
        Corner_SE = 5,
        Corner_SW = 6,

        InnerCorner_NE = 7,
        InnerCorner_NW = 8,
        InnerCorner_SE = 9,
        InnerCorner_SW = 10,
        
        TJunction_N = 11,
        TJunction_E = 12,
        TJunction_S = 13,
        TJunction_W = 14,
        
        Cross = 15,
        
        DeadEnd_N = 16,
        DeadEnd_E = 17,
        DeadEnd_S = 18,
        DeadEnd_W = 19,

        BusStop = 20,

        Crosswalk_NS = 21,
        Crosswalk_EW = 22,

        // Generic types
        GenericRoad = 99,
        GenericCrosswalk = 100
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
