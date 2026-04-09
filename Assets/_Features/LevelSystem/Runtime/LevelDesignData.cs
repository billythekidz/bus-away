using System.Collections.Generic;
using UnityEngine;

namespace BusAway.Level
{
    [System.Serializable]
    public class RoadSegmentData
    {
        public string segmentName = "Road Segment";
        
        [Tooltip("Sharp corners used to interpolate the curved path")]
        public List<Vector3> sharpPoints = new List<Vector3>();
        
        [Tooltip("Width of this specific road segment")]
        public float roadWidth = 2.0f;
        
        [Tooltip("Maximum cornering radius")]
        public float cornerRadius = 1.5f;

        [Tooltip("Whether this is a closed loop tying start to end (for future enhancements)")]
        public bool isClosedLoop = false;
    }

    [System.Serializable]
    public class BusSpawnData
    {
        public string busID;
        public Vector3 spawnPosition;
        public Vector3 eulerAngles;
        public int capacity = 3;
        public Color busColor = Color.white;
    }

    [CreateAssetMenu(fileName = "Level_001", menuName = "Bus Away/Level Design Data")]
    public class LevelDesignData : ScriptableObject
    {
        [Header("Road Network Layout")]
        [Tooltip("List of road segments constructing the map grid (parking lots, junctions...)")]
        public List<RoadSegmentData> roadSegments = new List<RoadSegmentData>();

        [Header("Gameplay Config")]
        [Tooltip("Target coins or goal to clear the level")]
        public int levelGoalCoin = 160;

        [Tooltip("Configuration of parked buses")]
        public List<BusSpawnData> buses = new List<BusSpawnData>();
        
        [Tooltip("Spawn order sequence for the Passenger Queue array")]
        public List<Color> passengerQueueOrder = new List<Color>();
    }
}
