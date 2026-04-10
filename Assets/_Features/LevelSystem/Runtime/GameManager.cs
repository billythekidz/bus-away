using System.Collections;
using System.Collections.Generic;
using BusAway.Level;
using UnityEngine;

namespace BusAway.Gameplay
{
    public enum GameState
    {
        Initializing,
        Ready,
        Playing,
        GameOver,
        LevelCleared
    }

    [RequireComponent(typeof(LevelGenerator))]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public LevelGenerator levelGenerator;


        [Header("Prefabs")]
        public GameObject busPrefab;

        [Header("Runtime State")]
        public GameState State { get; private set; } = GameState.Initializing;
        public int currentCoins = 0;
        public int goalCoins = 0;

        [Header("Serialize Fields")]
        [SerializeField] private GameObject PlayPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)

            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (levelGenerator == null) levelGenerator = GetComponent<LevelGenerator>();

            SetupObjects();
            StartCoroutine(InitGameFlow());
        }


        private void SetupObjects()
        {
            PlayPanel.SetActive(true);
        }


        private IEnumerator InitGameFlow()
        {
            if (levelGenerator == null || levelGenerator.activeLevelData == null)
            {
                Debug.LogError("GameManager: LevelGenerator or LevelDesignData is missing!");
                yield break;
            }

            var data = levelGenerator.activeLevelData;
            goalCoins = data.levelGoalCoin;
            currentCoins = 0;

            // 1. Build Physical Level (Roads, static props)
            // LevelGenerator will now ONLY build the static environment (grid tiles)
            levelGenerator.BuildLevel();

            // Wait a frame for transforms to settle
            yield return null;

            // 2. Setup Bus Stops
            SetupBusStops(data);

            // 3. Dispatch Buses
            DispatchBuses(data);

            // 4. Setup Crowd
            SetupCrowd(data);

            var stops = FindObjectsOfType<BusStopController>();
            State = GameState.Ready;
            Debug.Log($"<color=yellow>Game Flow Initialized!</color> Goal: {goalCoins} Coins. Total Buses: {data.landColorPalette.Count * data.busesPerStop} | Bus Stops: {stops.Length}. Waiting for Play().");
        }

        public void Play()
        {
            if (State != GameState.Ready)
            {
                Debug.LogWarning($"Cannot play from state: {State}");
                return;
            }


            State = GameState.Playing;
            Debug.Log("<color=green>Game Started!</color>");

            // TODO: Enable player input or start timer here

        }

        private void SetupBusStops(LevelDesignData data)
        {
            var stops = FindObjectsOfType<BusStopController>();
            if (stops == null || stops.Length == 0 || data.landColorPalette.Count == 0) return;

            // Tạm thời tô màu xoay vòng theo bảng màu (Palette)
            // Nếu game yêu cầu trạm neutral thì có thể đổi logic sau
            for (int i = 0; i < stops.Length; i++)
            {
                Color c = data.landColorPalette[i % data.landColorPalette.Count];
                stops[i].SetColor(c);
                stops[i].SetNumber(data.busesPerStop); // Hiển thị số lượng lên nóc trạm
            }
        }

        private void DispatchBuses(LevelDesignData data)
        {
            if (busPrefab == null) return;

            // RULE: Bao nhiêu bus stop thì có bấy nhiêu bus được chạy trong road một lúc (không hơn, không kém).
            var stops = FindObjectsOfType<BusStopController>();
            int activeBusLimit = stops != null ? stops.Length : 0;


            Transform busRoot = this.transform.Find("BusesRoot");
            if (busRoot == null)
            {
                var newRoot = new GameObject("BusesRoot");
                newRoot.transform.SetParent(this.transform);
                busRoot = newRoot.transform;
            }

            // Đếm số lượng xe đã được đặt sẵn trên grid (từ LevelGenerator)
            Dictionary<Color, int> spawnedCountByColor = new Dictionary<Color, int>();
            if (data.buses != null)
            {
                foreach (var b in data.buses)
                {
                    if (!spawnedCountByColor.ContainsKey(b.busColor)) spawnedCountByColor[b.busColor] = 0;
                    spawnedCountByColor[b.busColor]++;
                }
            }

            // Sinh toàn bộ xe bus còn thiếu đưa vào waiting queue (trong tương lai sẽ spawn xe từ queue vào grid khi có chỗ trống)
            int queuedBusIndex = 0;
            foreach (Color busColor in data.landColorPalette)
            {
                int alreadySpawned = spawnedCountByColor.ContainsKey(busColor) ? spawnedCountByColor[busColor] : 0;
                int remainingToSpawn = data.busesPerStop - alreadySpawned;

                for (int i = 0; i < remainingToSpawn; i++)
                {
                    GameObject busObj = Instantiate(busPrefab, busRoot);
                    busObj.name = $"Bus_Queued_{busColor.ToString()}_{i}";


                    var busCtrl = busObj.GetComponent<BusMovement.BusController>();
                    if (busCtrl != null)
                    {
                        busCtrl.SetColor(busColor);
                    }

                    // Tạm xếp các xe đang xếp hàng đợi ở bãi đỗ xa (ví dụ X = -20)
                    // Hoặc ẩn đi (SetActive(false)), chờ logic đưa xe vào grid
                    busObj.transform.position = new Vector3(-20f + queuedBusIndex * 2f, 0.5f, -20f);

                    busObj.SetActive(false);

                    queuedBusIndex++;
                }
            }

            int initialActive = data.buses != null ? data.buses.Count : 0;
            Debug.Log($"<color=cyan>Bus Queue Manager</color>: Active Road Limit = {activeBusLimit} (1 per stop). Initial on grid = {initialActive}. Queued waiting = {queuedBusIndex}");
        }

        private void SetupCrowd(LevelDesignData data)
        {
            // Phân phối passenger dựa trên data.resolvedLands
            if (data.resolvedLands == null || data.resolvedLands.Count == 0) return;

            // Tạm thời dựa vào CrowdManager nếu có, hoặc custom logic ở đây
            // Currently calling the existing CrowdManager logic if it exists
            if (BusAway.CrowdSystem.CrowdManager.Instance != null)
            {
                for (int i = 0; i < data.resolvedLands.Count; i++)
                {
                    var land = data.resolvedLands[i];
                    BusAway.CrowdSystem.CrowdManager.Instance.SpawnLand(i, land.agentCount, land.color);
                }
            }
            else
            {
                Debug.LogWarning("CrowdManager.Instance is null - cannot spawn crowd agents.");
            }
        }

        public void AddCoin(int amount)
        {
            if (State != GameState.Playing) return;

            currentCoins += amount;
            if (currentCoins >= goalCoins)
            {
                State = GameState.LevelCleared;
                // Level Clear Logic
                Debug.Log("<color=green>LEVEL CLEARED!</color>");
            }
        }
    }
}
