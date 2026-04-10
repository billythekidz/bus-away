using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BusAway.Level;

namespace BusAway.Gameplay
{
    [RequireComponent(typeof(LevelGenerator))]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public LevelGenerator levelGenerator;
        
        [Header("Prefabs")]
        public GameObject busPrefab;

        [Header("Runtime State")]
        public int currentCoins = 0;
        public int goalCoins = 0;

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
            
            StartCoroutine(InitGameFlow());
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
            Debug.Log($"<color=yellow>Game Flow Initialized!</color> Goal: {goalCoins} Coins. Total Buses: {data.landColorPalette.Count * data.busesPerStop} | Bus Stops: {stops.Length}");
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
            }
        }

        private void DispatchBuses(LevelDesignData data)
        {
            if (busPrefab == null) return;
            
            GameObject busRoot = new GameObject("BusesRoot");
            busRoot.transform.SetParent(this.transform);

            // We need to spawn 'busesPerStop' buses for EACH color in the landColorPalette.
            // They need a path to move on. For now, we spawn them at the center or random road tile,
            // later we will map the circular road track and pass the List<Vector3> path to BusController.
            int busIndex = 0;
            foreach (Color busColor in data.landColorPalette)
            {
                for (int i = 0; i < data.busesPerStop; i++)
                {
                    GameObject busObj = Instantiate(busPrefab, busRoot.transform);
                    busObj.name = $"Bus_{busIndex}_{ColorUtility.ToHtmlStringRGB(busColor)}";
                    busObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    
                    // Set color
                    var busCtrl = busObj.GetComponent<BusMovement.BusController>();
                    if (busCtrl != null)
                    {
                        busCtrl.SetColor(busColor);
                    }
                    else
                    {
                        var renderer = busObj.GetComponentInChildren<Renderer>();
                        if (renderer != null)
                        {
                            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            mat.color = busColor;
                            renderer.material = mat;
                        }
                    }

                    // Tạm thời đặt rải rác. Về sau sẽ chạy thuật toán tìm đường (Pathfinding)
                    // để lấy List<Vector3> road map và gọi busObj.GetComponent<BusController>().MoveAlongPath(...)
                    busObj.transform.position = new Vector3(busIndex * 2f, 0.5f, 0); 
                    busIndex++;
                }
            }
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
            currentCoins += amount;
            if (currentCoins >= goalCoins)
            {
                // Level Clear Logic
                Debug.Log("<color=green>LEVEL CLEARED!</color>");
            }
        }
    }
}
