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

        private List<Vector2Int> mainLoopPath = new List<Vector2Int>();
        private Dictionary<Color, Vector2Int> busStopStems = new Dictionary<Color, Vector2Int>();

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

            // Extract the road network loop
            BuildRoadNetwork();

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

            // Extract initial buses from queue and place them at bus stops to enter the grid
            var queuedBuses = new List<GameObject>();
            Transform busRoot = this.transform.Find("BusesRoot");
            if (busRoot != null)
            {
                foreach (Transform t in busRoot)
                {
                    if (t.name.Contains("Bus_Queued")) queuedBuses.Add(t.gameObject);
                }
            }

            var stops = FindObjectsOfType<BusStopController>();
            int spawnCount = Mathf.Min(stops.Length, queuedBuses.Count);

            float tileSize = levelGenerator != null && levelGenerator.activeLevelData != null ? levelGenerator.activeLevelData.tileSize : 4f;

            for (int i = 0; i < spawnCount; i++)
            {
                var bus = queuedBuses[i];
                var stop = stops[i];
                
                bus.SetActive(true);
                // Rename to indicate active
                bus.name = bus.name.Replace("Bus_Queued", "Bus_Active");
                
                // Spawn bus INSIDE the Stop's stem (pull back by half a tile)
                Vector3 stopPos = stop.transform.position + Vector3.up * 0.5f;
                Vector3 startPos = stopPos - stop.transform.forward * (tileSize * 0.45f);
                bus.transform.position = startPos;
                bus.transform.rotation = stop.transform.rotation;
                
                // Make the bus drive out of the stop into the road center
                var busCtrl = bus.GetComponent<BusMovement.BusController>();
                if (busCtrl != null)
                {
                    busCtrl.currentGridPos = WorldToGrid(startPos);
                    busCtrl.previousGridPos = WorldToGrid(startPos - stop.transform.forward * tileSize);
                    busCtrl.OnPathComplete += OnBusPathComplete;

                    Vector3 roadCenter = stopPos + stop.transform.forward * tileSize;
                    busCtrl.MoveAlongPath(new List<Vector3> { startPos, roadCenter });
                }
            }

        }

        private void OnBusPathComplete(BusMovement.BusController bus)
        {
            if (State != GameState.Playing) return;

            Vector2Int currentGridPos = WorldToGrid(bus.transform.position);
            bus.previousGridPos = bus.currentGridPos;
            bus.currentGridPos = currentGridPos;

            int capacity = levelGenerator.activeLevelData.agentsPerBus;
            bool isFull = bus.currentPassengerCount >= capacity;

            Vector2Int nextPos = new Vector2Int(-1, -1);

            // If full, try to enter its own stop
            if (isFull && busStopStems.ContainsKey(bus.busColor))
            {
                Vector2Int targetStem = busStopStems[bus.busColor];
                if (IsAdjacent(currentGridPos, targetStem))
                {
                    nextPos = targetStem;
                }
            }

            if (nextPos.x == -1)
            {
                // Find next loop tile
                int idx = mainLoopPath.IndexOf(currentGridPos);
                if (idx != -1)
                {
                    nextPos = mainLoopPath[(idx + 1) % mainLoopPath.Count];
                    // Ensure it doesn't reverse if moving backward for some reason
                    if (nextPos == bus.previousGridPos)
                    {
                        nextPos = mainLoopPath[(idx - 1 + mainLoopPath.Count) % mainLoopPath.Count];
                    }
                }
                else
                {
                    // Fallback
                    var neighbors = GetConnectedNeighbors(currentGridPos, levelGenerator.activeLevelData.GetCell(currentGridPos.x, currentGridPos.y));
                    foreach (var n in neighbors)
                    {
                        if (n != bus.previousGridPos)
                        {
                            nextPos = n;
                            break;
                        }
                    }
                    if (nextPos.x == -1 && neighbors.Count > 0) nextPos = neighbors[0];
                }
            }

            if (nextPos.x != -1)
            {
                Vector3 targetWorld = GridToWorld(nextPos);
                targetWorld.y = bus.transform.position.y;
                bus.MoveAlongPath(new List<Vector3> { bus.transform.position, targetWorld });

                // If it reached its stem, it parks
                if (busStopStems.ContainsKey(bus.busColor) && nextPos == busStopStems[bus.busColor])
                {
                    bus.OnPathComplete -= OnBusPathComplete;
                }
            }
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
                    busObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);


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

        #region Path Finding & Grid Helpers

        private void BuildRoadNetwork()
        {
            var data = levelGenerator.activeLevelData;
            mainLoopPath.Clear();
            busStopStems.Clear();

            // Find stems
            var stops = FindObjectsOfType<BusStopController>();
            foreach(var stop in stops)
            {
                Vector2Int pos = WorldToGrid(stop.transform.position);
                Vector2Int[] neighbors = { pos + Vector2Int.up, pos + Vector2Int.down, pos + Vector2Int.left, pos + Vector2Int.right };
                foreach (var n in neighbors)
                {
                    var cell = data.GetCell(n.x, n.y);
                    if (cell != RoadCellType.Empty && cell.ToString().Contains("DeadEnd"))
                    {
                        busStopStems[stop.stopColor] = n;
                        break;
                    }
                }
            }

            // Find start of loop
            Vector2Int start = new Vector2Int(-1, -1);
            for (int y = 0; y < data.gridHeight; y++)
            {
                for (int x = 0; x < data.gridWidth; x++)
                {
                    var cell = data.GetCell(x, y);
                    if (cell != RoadCellType.Empty && !cell.ToString().Contains("DeadEnd"))
                    {
                        start = new Vector2Int(x, y);
                        break;
                    }
                }
                if (start.x != -1) break;
            }

            if (start.x == -1) return;

            Vector2Int current = start;
            Vector2Int previous = new Vector2Int(-2, -2);
            
            while (true)
            {
                mainLoopPath.Add(current);
                var cell = data.GetCell(current.x, current.y);
                List<Vector2Int> neighbors = GetConnectedNeighbors(current, cell);
                
                Vector2Int next = new Vector2Int(-1, -1);
                foreach (var n in neighbors)
                {
                    if (n != previous)
                    {
                        next = n;
                        break;
                    }
                }
                
                if (next.x == -1 || next == start) break;
                previous = current;
                current = next;
                if (mainLoopPath.Count > 1000) break;
            }
            Debug.Log($"Built Main Road Loop with {mainLoopPath.Count} tiles.");
        }

        private List<Vector2Int> GetConnectedNeighbors(Vector2Int pos, RoadCellType cell)
        {
            var list = new List<Vector2Int>();
            if (cell == RoadCellType.Straight_NS) { list.Add(pos + new Vector2Int(0, -1)); list.Add(pos + new Vector2Int(0, 1)); }
            else if (cell == RoadCellType.Straight_EW) { list.Add(pos + new Vector2Int(1, 0)); list.Add(pos + new Vector2Int(-1, 0)); }
            else if (cell == RoadCellType.Corner_NE) { list.Add(pos + new Vector2Int(0, 1)); list.Add(pos + new Vector2Int(1, 0)); }
            else if (cell == RoadCellType.Corner_NW) { list.Add(pos + new Vector2Int(0, 1)); list.Add(pos + new Vector2Int(-1, 0)); }
            else if (cell == RoadCellType.Corner_SE) { list.Add(pos + new Vector2Int(0, -1)); list.Add(pos + new Vector2Int(1, 0)); }
            else if (cell == RoadCellType.Corner_SW) { list.Add(pos + new Vector2Int(0, -1)); list.Add(pos + new Vector2Int(-1, 0)); }
            else if (cell.ToString().Contains("HalfT_BusStop"))
            {
                if (cell.ToString().Contains("_N_") || cell.ToString().Contains("_S_")) { list.Add(pos + new Vector2Int(1, 0)); list.Add(pos + new Vector2Int(-1, 0)); }
                else { list.Add(pos + new Vector2Int(0, -1)); list.Add(pos + new Vector2Int(0, 1)); }
            }
            
            var filtered = new List<Vector2Int>();
            foreach(var n in list) {
                var nCell = levelGenerator.activeLevelData.GetCell(n.x, n.y);
                if (nCell != RoadCellType.Empty && !nCell.ToString().Contains("DeadEnd")) filtered.Add(n);
            }
            return filtered;
        }

        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float tSize = levelGenerator.activeLevelData.tileSize;
            float offsetX = (levelGenerator.activeLevelData.gridWidth * tSize) / 2f - (tSize / 2f);
            float offsetZ = (levelGenerator.activeLevelData.gridHeight * tSize) / 2f - (tSize / 2f);
            
            int x = Mathf.RoundToInt((offsetX - worldPos.x) / tSize);
            int y = Mathf.RoundToInt((offsetZ - worldPos.z) / tSize);
            return new Vector2Int(x, y);
        }

        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            float tSize = levelGenerator.activeLevelData.tileSize;
            float offsetX = (levelGenerator.activeLevelData.gridWidth * tSize) / 2f - (tSize / 2f);
            float offsetZ = (levelGenerator.activeLevelData.gridHeight * tSize) / 2f - (tSize / 2f);
            return new Vector3(offsetX - gridPos.x * tSize, 0f, offsetZ - gridPos.y * tSize);
        }

        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        #endregion

    }
}
