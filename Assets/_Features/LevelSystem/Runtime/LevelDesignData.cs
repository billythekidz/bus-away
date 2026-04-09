using System.Collections.Generic;
using UnityEngine;

namespace BusAway.Level
{
    [System.Serializable]
    public class RoadSegmentData
    {
        public string segmentName = "Road Segment";
        
        [Tooltip("Các góc vuông (sharp corners) để nội suy ra đường cong")]
        public List<Vector3> sharpPoints = new List<Vector3>();
        
        [Tooltip("Độ rộng của đoạn đường này")]
        public float roadWidth = 2.0f;
        
        [Tooltip("Bán kính bo góc cực đại")]
        public float cornerRadius = 1.5f;

        [Tooltip("Là vòng lặp kín khép góc cuối hay không (tuỳ chọn nâng cấp sau)")]
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
        [Tooltip("Danh sách các đoạn đường tạo nên toàn bộ map lưới (ngã ba, bãi đỗ...)")]
        public List<RoadSegmentData> roadSegments = new List<RoadSegmentData>();

        [Header("Gameplay Config")]
        [Tooltip("Số tiền hoặc mục tiêu cần đạt để qua màn")]
        public int levelGoalCoin = 160;

        [Tooltip("Cấu hình danh sách xe buýt đang đậu trong bãi")]
        public List<BusSpawnData> buses = new List<BusSpawnData>();
        
        [Tooltip("Mảng quy định danh sách hàng đợi hành khách (Passenger Queue) spawn theo thứ tự")]
        public List<Color> passengerQueueOrder = new List<Color>();
    }
}
