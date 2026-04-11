using System.Collections;
using System.Collections.Generic;
using BusAway.Level;
using TMPro;
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
        private Dictionary<BusStopController, Queue<Color>> stopQueues = new Dictionary<BusStopController, Queue<Color>>();
        private Dictionary<BusStopController, Vector2Int> stopToStem = new Dictionary<BusStopController, Vector2Int>();
        private Dictionary<BusMovement.BusController, BusStopController> busToStop = new Dictionary<BusMovement.BusController, BusStopController>();
        private List<BusMovement.BusController> activeBusesWaitingForPlay = new List<BusMovement.BusController>();

        // -- Boarding / Loading Zone System --
        public class BusWaitingGroup
        {
            public Color color;
            public int agentCount;
            public int landIndex;
        }
        private Queue<BusWaitingGroup> busWaitingQueue = new Queue<BusWaitingGroup>();
        private HashSet<int> tappedLands = new HashSet<int>();
        private Dictionary<int, int> landChunkIndices = new Dictionary<int, int>();


        private List<Vector2Int> loadingZoneTiles = new List<Vector2Int>();
        private Dictionary<BusMovement.BusController, Vector2Int> busToLoadingZone = new Dictionary<BusMovement.BusController, Vector2Int>();
        private List<BusMovement.BusController> waitingToMoveBuses = new List<BusMovement.BusController>();
        private TextMeshPro waitZoneCountText;


        [Header("Serialize Fields")]
        [SerializeField] private GameObject PlayPanel;
        [SerializeField] private Transform busWaitZone;

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

        private void Update()
        {
            if (State != GameState.Playing) return;


            HandleInput();
            ProcessLoadingZones();
            ProcessWaitingBuses();
            UpdateWaitZoneText();
        }

        private void UpdateWaitZoneText()
        {
            if (waitZoneCountText == null)
            {
                var textObj = new GameObject("WaitZoneText");
                textObj.transform.position = new Vector3(0, 0.2f, 2.2f);
                textObj.transform.eulerAngles = new Vector3(90, 180, 0); // Flat on ground, facing flipped camera
                waitZoneCountText = textObj.AddComponent<TextMeshPro>();
                waitZoneCountText.fontSize = 5;
                waitZoneCountText.fontStyle = FontStyles.Bold;
                waitZoneCountText.alignment = TextAlignmentOptions.Center;
            }

            int currentWaitCount = 0;
            foreach (var group in busWaitingQueue) currentWaitCount += group.agentCount;
            int maxLimit = levelGenerator.activeLevelData.busesPerStop * 60;


            waitZoneCountText.text = $"{currentWaitCount} / {maxLimit}";
            waitZoneCountText.color = (currentWaitCount >= maxLimit) ? Color.red : Color.black;
        }

        private void ProcessWaitingBuses()
        {
            for (int i = waitingToMoveBuses.Count - 1; i >= 0; i--)
            {
                var bus = waitingToMoveBuses[i];
                if (bus == null)
                {
                    waitingToMoveBuses.RemoveAt(i);
                    continue;
                }


                if (TryMoveBus(bus))
                {
                    waitingToMoveBuses.RemoveAt(i);
                }
            }
        }

        private bool IsTileOccupied(Vector2Int tile, BusMovement.BusController queryingBus)
        {
            foreach (var kvp in busToStop)
            {
                var bus = kvp.Key;
                if (bus == queryingBus || bus == null) continue;


                if (!bus.isMoving && bus.currentGridPos == tile) return true;
                if (bus.isMoving && bus.targetGridPos == tile) return true;
            }
            return false;
        }

        private void HandleInput()
        {
            bool isTap = false;
            Vector2 inputPos = Vector2.zero;

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                isTap = true;
                inputPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                isTap = true;
                inputPos = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (isTap)
            {
                Ray ray = Camera.main.ScreenPointToRay(inputPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    float worldX = hit.point.x;


                    int bestLand = -1;
                    float minD = float.MaxValue;
                    var crowds = levelGenerator.activeLevelData.resolvedLands;


                    if (BusAway.CrowdSystem.CrowdManager.Instance != null && crowds != null)
                    {
                        for (int i = 0; i < crowds.Count; i++)
                        {
                            if (tappedLands.Contains(i)) continue;


                            float formationWidth = (crowds.Count - 1) * BusAway.CrowdSystem.CrowdManager.Instance.landSpacingX;
                            float offsetX = -formationWidth / 2f;
                            Vector3 landBase = BusAway.CrowdSystem.CrowdManager.Instance.transform.position + BusAway.CrowdSystem.CrowdManager.Instance.landBaseOffset;
                            float expectedX = landBase.x + offsetX + i * BusAway.CrowdSystem.CrowdManager.Instance.landSpacingX;


                            float d = Mathf.Abs(expectedX - worldX);
                            if (d < minD && d < BusAway.CrowdSystem.CrowdManager.Instance.landSpacingX * 0.5f)
                            {
                                minD = d;
                                bestLand = i;
                            }
                        }
                    }


                    if (bestLand != -1)
                    {
                        var group = levelGenerator.activeLevelData.resolvedLands[bestLand];
                        if (!landChunkIndices.ContainsKey(bestLand)) landChunkIndices[bestLand] = 0;
                        int chunkIdx = landChunkIndices[bestLand];

                        if (chunkIdx < group.chunks.Count)
                        {
                            var activeChunk = group.chunks[chunkIdx];

                            // Determine Z bounds of the active chunk
                            int startRow = 0;
                            for (int i = 0; i < chunkIdx; i++)
                            {
                                startRow += group.chunks[i].agentCount / 4;
                            }
                            int chunkRows = activeChunk.agentCount / 4;


                            Vector3 landBase = BusAway.CrowdSystem.CrowdManager.Instance.transform.position + BusAway.CrowdSystem.CrowdManager.Instance.landBaseOffset;
                            float frontZ = 0.1f;
                            float agentZ = BusAway.CrowdSystem.CrowdManager.Instance.agentSpacingZ;

                            // +/- 1 row tolerance for easier clicking

                            float minZ = landBase.z + frontZ + (startRow - 1.0f) * agentZ;
                            float maxZ = landBase.z + frontZ + (startRow + chunkRows + 1.0f) * agentZ;

                            if (hit.point.z < minZ || hit.point.z > maxZ)

                            {
                                return; // Clicked on the land, but not on the top-most active chunk. Ignored!
                            }

                            int currentWaitCount = 0;
                            foreach (var g in busWaitingQueue) currentWaitCount += g.agentCount;
                            int maxLimit = levelGenerator.activeLevelData.busesPerStop * 60;

                            if (currentWaitCount + activeChunk.agentCount > maxLimit)
                            {
                                Debug.Log("Wait Zone is full! Cannot dispatch more passengers.");
                                return;
                            }

                            landChunkIndices[bestLand] = chunkIdx + 1; // move to next chunk

                            busWaitingQueue.Enqueue(new BusWaitingGroup { color = activeChunk.color, agentCount = activeChunk.agentCount, landIndex = bestLand });
                            Debug.Log($"Queueing Land {bestLand} with {activeChunk.agentCount} agents of color {activeChunk.color}");

                            // Haptic: light tap to confirm valid land selection
                            HapticFeedback.Light();

                            // Calculate top center edge of busWaitZone (smallest Z)
                            Vector3 targetWaitPos = new Vector3(0, 0, -2f);
                            if (busWaitZone != null)
                            {
                                // Top boundary corresponds to minimum Z due to 180 deg Y camera rotation
                                float topEdgeZ = busWaitZone.position.z - (busWaitZone.localScale.z / 2f);
                                targetWaitPos = new Vector3(busWaitZone.position.x, busWaitZone.position.y, topEdgeZ);
                            }

                            // Dispatch agents visually to Walk Zone / Wait Zone 
                            int shiftRows = activeChunk.agentCount / 4;
                            BusAway.CrowdSystem.CrowdManager.Instance.DispatchGroupToWaitZone(bestLand, activeChunk.color, activeChunk.agentCount, targetWaitPos, shiftRows);

                            if (landChunkIndices[bestLand] >= group.chunks.Count)
                            {
                                tappedLands.Add(bestLand);
                            }
                        }
                    }
                }
            }
        }

        // Throttle ProcessLoadingZones log spam
        private float _lastLoadingZoneLogTime = -999f;

        private void ProcessLoadingZones()
        {
            bool verbose = (Time.time - _lastLoadingZoneLogTime) > 2f;

            if (busWaitingQueue.Count == 0) return;

            // Check available loading zone slots
            if (loadingZoneTiles.Count == 0)
            {
                if (verbose) { Debug.LogWarning("[Loading] loadingZoneTiles is EMPTY! No loading zone was set up."); _lastLoadingZoneLogTime = Time.time; }
                return;
            }

            var availableSlots = new List<Vector2Int>(loadingZoneTiles);
            foreach (var kvp in busToLoadingZone)
            {
                availableSlots.Remove(kvp.Value);
            }

            if (availableSlots.Count == 0)
            {
                if (verbose) { Debug.Log($"[Loading] All {loadingZoneTiles.Count} slot(s) occupied. Buses in zone: {busToLoadingZone.Count}"); _lastLoadingZoneLogTime = Time.time; }
                return;
            }

            var group = busWaitingQueue.Peek();
            if (verbose) Debug.Log($"[Loading] Seeking bus of color {group.color} ({group.agentCount} agents). Buses registered: {busToStop.Count}");

            // Find a roaming bus of this color
            BusMovement.BusController chosenBus = null;
            foreach (var kvp in busToStop)
            {
                var bus = kvp.Key;
                if (bus == null) continue;
                bool alreadyInZone = busToLoadingZone.ContainsKey(bus);
                bool colorMatch = bus.busColor == group.color;
                if (verbose) Debug.Log($"  Bus '{bus.name}' color={bus.busColor} vs group={group.color} | match={colorMatch} | inZone={alreadyInZone}");
                if (!alreadyInZone && colorMatch)
                {
                    chosenBus = bus;
                    break;
                }
            }

            if (chosenBus != null)
            {
                Vector2Int slot = availableSlots[0];
                busToLoadingZone[chosenBus] = slot;
                _lastLoadingZoneLogTime = Time.time;
                Debug.Log($"[Loading] <color=cyan>Bus '{chosenBus.name}' assigned to Loading Zone slot {slot}. Will stop there next time it passes.</color>");
            }
            else
            {
                if (verbose) { Debug.LogWarning($"[Loading] No matching bus found for color {group.color}!"); _lastLoadingZoneLogTime = Time.time; }
            }
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
            landChunkIndices.Clear();
            tappedLands.Clear();

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

            // All buses waiting for play should start moving!
            foreach (var busCtrl in activeBusesWaitingForPlay)
            {
                if (busCtrl != null)
                {
                    busCtrl.OnPathComplete -= OnBusPathComplete;
                    busCtrl.OnPathComplete += OnBusPathComplete;

                    // Kickstart the movement towards the next calculated pos

                    int pathIdx = mainLoopPath.IndexOf(busCtrl.currentGridPos);
                    if (pathIdx != -1)
                    {
                        Vector2Int nextPos = mainLoopPath[(pathIdx + 1) % mainLoopPath.Count];
                        Vector3 targetWorld = GridToWorld(nextPos);
                        targetWorld.y = busCtrl.transform.position.y;


                        float randDelay = Random.Range(0f, 1.25f);
                        StartCoroutine(DelayedKickstartBus(busCtrl, nextPos, targetWorld, randDelay));
                    }
                }
            }
            activeBusesWaitingForPlay.Clear();

        }

        private IEnumerator DelayedKickstartBus(BusMovement.BusController busCtrl, Vector2Int nextPos, Vector3 targetWorld, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (State != GameState.Playing || busCtrl == null) yield break;


            busCtrl.targetGridPos = nextPos;
            busCtrl.MoveAlongPath(new List<Vector3> { busCtrl.transform.position, targetWorld }, false);
        }

        private void OnBusPathComplete(BusMovement.BusController bus)
        {
            if (State != GameState.Playing) return;

            Vector2Int currentGridPos = WorldToGrid(bus.transform.position);
            bus.previousGridPos = bus.currentGridPos;
            bus.currentGridPos = currentGridPos;

            if (!TryMoveBus(bus))
            {
                if (!waitingToMoveBuses.Contains(bus))
                {
                    waitingToMoveBuses.Add(bus);
                }
            }
        }

        private bool TryMoveBus(BusMovement.BusController bus)
        {
            int capacity = levelGenerator.activeLevelData.agentsPerBus;
            bool isFull = bus.currentPassengerCount >= capacity;

            Vector2Int nextPos = new Vector2Int(-1, -1);

            bool isHeadingToStop = isFull;
            bool hasLoadingAssignment = busToLoadingZone.TryGetValue(bus, out var assignedSlot);
            if (hasLoadingAssignment)
            {
                isHeadingToStop = false;
            }

            if (isHeadingToStop && busToStop.TryGetValue(bus, out var stop))
            {
                if (stopToStem.TryGetValue(stop, out var targetStem))
                {
                    if (IsAdjacent(bus.currentGridPos, targetStem))
                    {
                        nextPos = targetStem;
                    }
                }
            }

            if (nextPos.x == -1)
            {
                int idx = mainLoopPath.IndexOf(bus.currentGridPos);
                if (idx != -1)
                {
                    nextPos = mainLoopPath[(idx + 1) % mainLoopPath.Count];
                    if (nextPos == bus.previousGridPos)
                    {
                        nextPos = mainLoopPath[(idx - 1 + mainLoopPath.Count) % mainLoopPath.Count];
                    }
                }
                else
                {
                    Debug.LogWarning($"[TryMoveBus] Bus '{bus.name}' at gridPos {bus.currentGridPos} is NOT on mainLoopPath! Path count={mainLoopPath.Count}");
                    var neighbors = GetConnectedNeighbors(bus.currentGridPos, levelGenerator.activeLevelData.GetCell(bus.currentGridPos.x, bus.currentGridPos.y));
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
                if (IsTileOccupied(nextPos, bus)) return false;

                bus.targetGridPos = nextPos;

                bool isParkingAtStem = isHeadingToStop && busToStop.TryGetValue(bus, out var stop2) && stopToStem.TryGetValue(stop2, out var stem2) && nextPos == stem2;
                bool isLoadingZone = hasLoadingAssignment && nextPos == assignedSlot;

                if (hasLoadingAssignment)
                {
                    Debug.Log($"[TryMoveBus] Bus '{bus.name}' assigned to zone {assignedSlot}, currently at {bus.currentGridPos}, nextPos={nextPos}, isLoadingZone={isLoadingZone}");
                }

                Vector3 targetWorld = GridToWorld(nextPos);
                targetWorld.y = bus.transform.position.y;
                bus.MoveAlongPath(new List<Vector3> { bus.transform.position, targetWorld }, isParkingAtStem || isLoadingZone);

                if (isParkingAtStem)
                {
                    // Don't destroy immediately — wait for MoveAlongPath tween to finish,
                    // then drive the bus into the physical stop and disable it there.
                    bus.OnPathComplete -= OnBusPathComplete;
                    bus.OnPathComplete += OnBusArrivedAtStem;
                }
                else if (isLoadingZone)
                {
                    Debug.Log($"[TryMoveBus] <color=yellow>Bus '{bus.name}' is heading to Loading Zone {assignedSlot}! Switching to OnBusArrivedAtLoadingZone.</color>");
                    bus.OnPathComplete -= OnBusPathComplete;
                    bus.OnPathComplete += OnBusArrivedAtLoadingZone;
                }

                return true;
            }
            return false;
        }

        // ── Called when bus tween finishes arriving at the stem tile (fork to stop) ──
        private void OnBusArrivedAtStem(BusMovement.BusController bus)
        {
            bus.OnPathComplete -= OnBusArrivedAtStem;

            if (busToStop.TryGetValue(bus, out var stop) && stop != null)
            {
                // Drive bus from stem into the physical bus stop position
                Vector3 stopWorldPos = stop.transform.position;
                stopWorldPos.y = bus.transform.position.y;

                Vector3 dir = (stopWorldPos - bus.transform.position);
                if (dir.sqrMagnitude > 0.001f)
                    bus.transform.rotation = Quaternion.LookRotation(dir.normalized);

                bus.OnPathComplete += OnBusArrivedAtStop;
                bus.MoveAlongPath(new List<Vector3> { bus.transform.position, stopWorldPos }, true);
            }
            else
            {
                // No stop info — just disable in place
                bus.gameObject.SetActive(false);
            }
        }

        // ── Called when bus finishes moving into the bus stop ──────────────────────
        private void OnBusArrivedAtStop(BusMovement.BusController bus)
        {
            bus.OnPathComplete -= OnBusArrivedAtStop;

            if (busToStop.TryGetValue(bus, out var originStop))
            {
                busToStop.Remove(bus);
                bus.gameObject.SetActive(false);   // park & hide at stop
                SpawnNextBusFromStop(originStop);   // dispatch next bus from that stop
            }
            else
            {
                bus.gameObject.SetActive(false);
            }
        }

        private void OnBusArrivedAtLoadingZone(BusMovement.BusController bus)
        {
            bus.OnPathComplete -= OnBusArrivedAtLoadingZone;

            Vector2Int currentGridPos = WorldToGrid(bus.transform.position);
            bus.previousGridPos = bus.currentGridPos;
            bus.currentGridPos = currentGridPos;

            Debug.Log($"[Boarding] <color=lime>Bus '{bus.name}' ARRIVED at Loading Zone {currentGridPos}. Queue size={busWaitingQueue.Count}, passengers={bus.currentPassengerCount}</color>");
            StartCoroutine(LoadPassengersCoroutine(bus));
        }

        private IEnumerator LoadPassengersCoroutine(BusMovement.BusController bus)
        {
            int capacity = levelGenerator.activeLevelData.agentsPerBus;
            Debug.Log($"[Boarding] Starting LoadPassengersCoroutine for '{bus.name}' (capacity={capacity}, current={bus.currentPassengerCount}). Queue size={busWaitingQueue.Count}");

            while (bus.currentPassengerCount < capacity && busWaitingQueue.Count > 0)
            {
                var group = busWaitingQueue.Peek();
                Debug.Log($"[Boarding] Queue head: color={group.color}, agentCount={group.agentCount}. Bus color={bus.busColor}, match={group.color == bus.busColor}");
                if (group.color != bus.busColor)
                {
                    Debug.Log($"[Boarding] Color mismatch - stopping load.");
                    break; // Queue head is not our color, stop loading
                }

                int spaceAvailable = levelGenerator.activeLevelData.agentsPerBus - bus.currentPassengerCount;
                int loadAmount = Mathf.Min(spaceAvailable, group.agentCount);

                var agentPositions = BusAway.CrowdSystem.CrowdManager.Instance.ExtractWaitZoneAgents(bus.busColor, loadAmount);


                for (int i = 0; i < agentPositions.Count; i++)
                {
                    var startPos = agentPositions[i];


                    GameObject dummy = Instantiate(BusAway.CrowdSystem.CrowdManager.Instance.agentPrefab);
                    dummy.transform.position = startPos;


                    var renderers = dummy.GetComponentsInChildren<Renderer>(true);
                    MaterialPropertyBlock mb = new MaterialPropertyBlock();
                    mb.SetColor("_BaseColor", bus.busColor);
                    foreach (var r in renderers) r.SetPropertyBlock(mb);

                    int currentSlot = bus.currentPassengerCount + i;
                    int col = currentSlot % 2;
                    int row = currentSlot / 2;


                    int maxCapacity = levelGenerator.activeLevelData.agentsPerBus;
                    int maxRows = Mathf.CeilToInt(maxCapacity / 2f);

                    // ── Snug cell-grid layout derived from wall positions ────────────────
                    // Divide the full interior into (maxRows × 2) equal cells.
                    // Each passenger sits at the center of their cell → no wasted space.
                    Transform vc = bus.busFloor.parent; // VisualContainer

                    float fz = bus.busWallF != null ? vc.InverseTransformPoint(bus.busWallF.position).z : 2.1f;
                    float bz = bus.busWallB != null ? vc.InverseTransformPoint(bus.busWallB.position).z : -2.1f;
                    float lx = bus.busWallL != null ? vc.InverseTransformPoint(bus.busWallL.position).x : -1.1f;
                    float rx = bus.busWallR != null ? vc.InverseTransformPoint(bus.busWallR.position).x : 1.1f;

                    // Grid cell sizes
                    float totalZ = Mathf.Abs(fz - bz);     // e.g. 4.2 local
                    float totalX = Mathf.Abs(lx - rx);     // e.g. 2.2 local
                    float cellZ = totalZ / maxRows;        // height of each row cell
                    float cellX = totalX / 2f;             // width of each col cell

                    float centerZ = (fz + bz) * 0.5f;       // ≈ 0
                    float centerX = (lx + rx) * 0.5f;       // ≈ -0.008 (slight asymmetry ok)

                    // Seat center = cell center (row 0 = front, row N-1 = rear)
                    float seatZ = centerZ + totalZ * 0.5f - cellZ * (row + 0.5f);
                    float seatX = (col == 0)
                        ? centerX - cellX * 0.5f   // left column
                        : centerX + cellX * 0.5f;  // right column

                    Vector3 localSeat = new Vector3(seatX, 0.3f, seatZ);


                    dummy.transform.SetParent(bus.busFloor.parent, true);

                    PrimeTween.Tween.LocalPosition(dummy.transform, localSeat, 0.25f, PrimeTween.Ease.OutQuad);
                    PrimeTween.Tween.LocalRotation(dummy.transform, Quaternion.identity, 0.25f, PrimeTween.Ease.OutQuad);

                    // Spark bling VFX — persistent above passenger head until bus is disabled
                    if (bus.sparkBlingVFX != null)
                    {
                        var spark = Instantiate(bus.sparkBlingVFX, dummy.transform);
                        spark.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                        var main = spark.main;
                        main.loop = true;
                        main.stopAction = ParticleSystemStopAction.None;
                        spark.Play();
                    }

                    // Haptic: medium pulse each time a passenger boards the bus
                    HapticFeedback.Medium();


                    yield return new WaitForSeconds(0.05f);
                }

                if (agentPositions.Count > 0)
                {
                    yield return new WaitForSeconds(0.25f);
                }

                group.agentCount -= loadAmount;
                bus.currentPassengerCount += loadAmount;
                bus.UpdatePassengerLabel(bus.currentPassengerCount, capacity);

                if (group.agentCount <= 0)
                {
                    busWaitingQueue.Dequeue();
                }
            }

            Debug.Log($"[Boarding] Done loading. Bus '{bus.name}' now has {bus.currentPassengerCount}/{capacity} passengers.");
            busToLoadingZone.Remove(bus);
            bus.OnPathComplete -= OnBusPathComplete;
            bus.OnPathComplete += OnBusPathComplete;

            if (!TryMoveBus(bus))
            {
                if (!waitingToMoveBuses.Contains(bus))
                {
                    waitingToMoveBuses.Add(bus);
                }
            }
        }

        private void SetupBusStops(LevelDesignData data)
        {
            var stops = FindObjectsOfType<BusStopController>();
            if (stops == null || stops.Length == 0 || data.resolvedLands == null) return;


            stopQueues.Clear();

            // Create a queue for each land representing the sequence of buses it requires
            List<Queue<Color>> landBusQueues = new List<Queue<Color>>();
            Dictionary<Color, int> globalColorAccum = new Dictionary<Color, int>();

            foreach (var land in data.resolvedLands)
            {
                Queue<Color> queue = new Queue<Color>();
                foreach (var chunk in land.chunks)
                {
                    if (!globalColorAccum.ContainsKey(chunk.color)) globalColorAccum[chunk.color] = 0;
                    globalColorAccum[chunk.color] += chunk.agentCount;

                    while (globalColorAccum[chunk.color] >= data.agentsPerBus)
                    {
                        queue.Enqueue(chunk.color);
                        globalColorAccum[chunk.color] -= data.agentsPerBus;
                    }
                }
                landBusQueues.Add(queue);
            }

            List<Color> orderedBuses = new List<Color>();
            bool addedAny = true;
            while (addedAny)
            {
                addedAny = false;
                List<int> availableLands = new List<int>();
                for (int i = 0; i < landBusQueues.Count; i++) if (landBusQueues[i].Count > 0) availableLands.Add(i);


                if (availableLands.Count > 0)
                {
                    addedAny = true;
                    // Shuffle the availableLands to make the interleaving random but still top-chunk oriented
                    for (int n = 0; n < availableLands.Count; n++)
                    {
                        int r = Random.Range(n, availableLands.Count);
                        int tmp = availableLands[n]; availableLands[n] = availableLands[r]; availableLands[r] = tmp;
                    }

                    foreach (var idx in availableLands)
                    {
                        orderedBuses.Add(landBusQueues[idx].Dequeue());
                    }
                }
            }

            // Distribute the ordered buses round-robin across available bus stops
            for (int s = 0; s < stops.Length; s++)
            {
                stopQueues[stops[s]] = new Queue<Color>();
            }

            for (int i = 0; i < orderedBuses.Count; i++)
            {
                stopQueues[stops[i % stops.Length]].Enqueue(orderedBuses[i]);
            }
        }

        private void DispatchBuses(LevelDesignData data)
        {
            if (busPrefab == null) return;
            busToStop.Clear();
            activeBusesWaitingForPlay.Clear();

            Transform busRoot = this.transform.Find("BusesRoot");
            if (busRoot != null)
            {
                foreach (Transform t in busRoot)
                {
                    if (Application.isPlaying) Destroy(t.gameObject);
                    else DestroyImmediate(t.gameObject);
                }
            }
            else
            {
                var newRoot = new GameObject("BusesRoot");
                newRoot.transform.SetParent(this.transform);
                busRoot = newRoot.transform;
            }

            // Chỉ spawn ra mỗi queue 1 xe lên grid ban đầu (first bus of each stop)!
            var stops = FindObjectsOfType<BusStopController>();
            foreach (var stop in stops)
            {
                SpawnNextBusFromStop(stop);
            }
        }

        private void SpawnNextBusFromStop(BusStopController stop)
        {
            if (stop == null || !stopQueues.ContainsKey(stop)) return;
            var queue = stopQueues[stop];


            if (queue.Count == 0)
            {
                // Trạm đã hoàn thành tất cả xe
                stop.SetNumber(0);
                return;
            }

            Color nextColor = queue.Dequeue();
            stop.SetColor(nextColor);

            // Số trên bảng hiển thị ĐÚNG số lượng xe đang còn xếp hàng bên trong bến chờ xuất phát
            stop.SetNumber(queue.Count);

            // Spawn the bus ở giữa ngã 3 (hoặc ngẫu nhiên trên loop?)
            // Để đẹp mắt, có thể randomize nó trên đường main loop, HOẶC ném ngay ở stem của bến
            int randIdx = Random.Range(0, mainLoopPath.Count);
            Vector2Int spawnGridPos = mainLoopPath[randIdx];

            Transform busRoot = this.transform.Find("BusesRoot");
            GameObject busObj = Instantiate(busPrefab, busRoot);
            busObj.name = $"Bus_Active_{nextColor.ToString()}_{queue.Count}";
            busObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);


            Vector3 worldPos = GridToWorld(spawnGridPos);
            worldPos.y = 0.5f;

            busObj.transform.position = worldPos;

            // Khởi tạo hướng xoay cho Bus dọc theo đường
            int pathIdx = mainLoopPath.IndexOf(spawnGridPos);
            Vector2Int nextPos = mainLoopPath[(pathIdx + 1) % mainLoopPath.Count];
            Vector3 nextWorld = GridToWorld(nextPos);
            nextWorld.y = worldPos.y;
            busObj.transform.rotation = Quaternion.LookRotation((nextWorld - worldPos).normalized);

            var busCtrl = busObj.GetComponent<BusMovement.BusController>();
            if (busCtrl != null)
            {
                busCtrl.SetColor(nextColor);
                busCtrl.currentGridPos = spawnGridPos;
                busCtrl.previousGridPos = spawnGridPos;
                busToStop[busCtrl] = stop;


                if (State == GameState.Playing)
                {
                    busCtrl.OnPathComplete += OnBusPathComplete;
                    busCtrl.MoveAlongPath(new List<Vector3> { worldPos, nextWorld }, false);
                }
                else
                {
                    activeBusesWaitingForPlay.Add(busCtrl);
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
                    var group = data.resolvedLands[i];
                    int startRow = 0;
                    foreach (var chunk in group.chunks)
                    {
                        BusAway.CrowdSystem.CrowdManager.Instance.SpawnLand(i, chunk.agentCount, chunk.color, data.resolvedLands.Count, startRow);
                        startRow += chunk.agentCount / 4;
                    }
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
            stopToStem.Clear();
            loadingZoneTiles.Clear();

            int bottomY = data.gridHeight - 1;
            int centerX = data.gridWidth / 2;


            // Find stems
            var stops = FindObjectsOfType<BusStopController>();
            foreach (var stop in stops)
            {
                Vector2Int pos = WorldToGrid(stop.transform.position);
                Vector2Int[] neighbors = { pos + Vector2Int.up, pos + Vector2Int.down, pos + Vector2Int.left, pos + Vector2Int.right };
                foreach (var n in neighbors)
                {
                    var cell = data.GetCell(n.x, n.y);
                    if (cell != RoadCellType.Empty && cell.ToString().Contains("DeadEnd"))
                    {
                        stopToStem[stop] = n;
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

            if (mainLoopPath.Count > 0)
            {
                // Camera rotated 180° around Y: grid Y=0 → world Z=+max → visual BOTTOM
                // We want the tile at minY (= visual bottom, where passengers wait)
                int minY = int.MaxValue;
                foreach (var pt in mainLoopPath) if (pt.y < minY) minY = pt.y;

                int bestX = -1;
                int minDistance = 9999;
                foreach (var pt in mainLoopPath)
                {
                    if (pt.y == minY)
                    {
                        int dist = Mathf.Abs(pt.x - centerX);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestX = pt.x;
                        }
                    }
                }


                if (bestX != -1)
                {
                    loadingZoneTiles.Add(new Vector2Int(bestX, minY));
                    Debug.Log($"<color=cyan>[BuildRoadNetwork] Loading Zone set at grid ({bestX}, {minY}), world={GridToWorld(new Vector2Int(bestX, minY))}</color>");
                }
                else
                {
                    Debug.LogError($"[BuildRoadNetwork] Could not find a loading zone tile! minY={minY}, centerX={centerX}. Loop tiles at minY: {string.Join(",", System.Array.FindAll(mainLoopPath.ToArray(), p => p.y == minY))}");
                }
            }

            Debug.Log($"[BuildRoadNetwork] Main Road Loop built with {mainLoopPath.Count} tiles. LoadingZoneTiles={loadingZoneTiles.Count}");
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
            foreach (var n in list)
            {
                var nCell = levelGenerator.activeLevelData.GetCell(n.x, n.y);
                if (nCell != RoadCellType.Empty && !nCell.ToString().Contains("DeadEnd")) filtered.Add(n);
            }
            return filtered;
        }

        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (levelGenerator != null) worldPos -= levelGenerator.transform.position;


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


            Vector3 localPos = new Vector3(offsetX - gridPos.x * tSize, 0f, offsetZ - gridPos.y * tSize);
            if (levelGenerator != null) return localPos + levelGenerator.transform.position;
            return localPos;
        }

        private bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        #endregion

    }
}
