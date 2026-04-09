using System.Collections.Generic;
using UnityEngine;

namespace BusAway.Level
{
    /// <summary>
    /// Defines the role of each road segment in the level layout.
    /// - MainRoad: Ring road or main connecting road, no bus stop at the end.
    /// - ParkingBranch: A short offshoot connected to the main ring where a bus parks.
    /// </summary>
    public enum RoadSegmentType
    {
        MainRoad,
        ParkingBranch,
    }

    [System.Serializable]
    public class RoadSegmentData
    {
        public string segmentName = "Road Segment";

        [Tooltip("MainRoad = plain road with corners. ParkingBranch = offshoot with a bus slot at the end.")]
        public RoadSegmentType segmentType = RoadSegmentType.MainRoad;

        [Tooltip("Sharp corners used to interpolate the curved path")]
        public List<Vector3> sharpPoints = new List<Vector3>();

        [Tooltip("Width of this road segment")]
        public float roadWidth = 2.0f;

        [Tooltip("Maximum corner rounding radius")]
        public float cornerRadius = 1.5f;

        [Tooltip("Whether this road forms a closed loop (e.g. ring road)")]
        public bool isClosedLoop = false;
    }

    [System.Serializable]
    public class BusSpawnData
    {
        public string busID;

        [Tooltip("World-space spawn position for the bus")]
        public Vector3 spawnPosition;

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
        [Header("Road Network Layout")]
        [Tooltip("All road segments in this level: one MainRoad ring + N ParkingBranch offshoots")]
        public List<RoadSegmentData> roadSegments = new List<RoadSegmentData>();

        [Header("Gameplay Config")]
        [Tooltip("Score target to clear this level")]
        public int levelGoalCoin = 160;

        [Header("Buses")]
        [Tooltip("One entry per ParkingBranch — position should match the end of the branch")]
        public List<BusSpawnData> buses = new List<BusSpawnData>();

        [Header("Passenger Queue")]
        [Tooltip("Ordered list of passenger colors to spawn during gameplay")]
        public List<Color> passengerQueueOrder = new List<Color>();
    }
}
