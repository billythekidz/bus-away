using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BusAway.CrowdSystem
{
    public class CrowdManager : MonoBehaviour
    {
        public enum SpawnMode { Prefab, InstancedMesh }

        public static CrowdManager Instance { get; private set; }

        [Header("Settings")]
        public SpawnMode spawnMode = SpawnMode.Prefab;
        public int maxAgents = 1000;
        public float moveSpeed = 3f;
        public float separationRadius = 1.0f;
        public float separationWeight = 1.5f;
        public float targetWeight = 1.0f;
        public float arrivalDistance = 0.1f;

        [Header("Land Spawning")]
        public float landSpacingX = 3.0f;
        public float landRoadSpacingZ = 1.0f;
        public float agentSpacingX = 0.6f;
        public float agentSpacingZ = 0.6f;
        public Vector3 landBaseOffset = new Vector3(0, 0, -2.0f);
        public int rowsPerLand = 6;
        [Tooltip("Số lượng gạch đường thẳng cố định của bãi đỗ, không bị co giãn khi đổi Agent Spacing")]
        public int landFixedRoadPieces = 5;
        public GameObject straightRoadPrefab;

        [Header("Rendering")]
        public GameObject agentPrefab;

        private class SubMeshInfo
        {
            public Mesh mesh;
            public Material material;
            public Matrix4x4 localMatrix;
        }
        private List<SubMeshInfo> subMeshes = new List<SubMeshInfo>();
        
        // Prefab mode list
        private GameObject[] agentPrefabs;

        // Native Arrays for Jobs
        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private NativeArray<float3> targets;
        private NativeArray<Matrix4x4> matrices;
        private NativeArray<int> states; // 0 = Rigid InLand, 1 = Boiding to BusWaitZone
        private NativeArray<int> lands; // Which land index the agent belongs to

        // Bug #2/#3 Fix: tempPositions stored as a field so we can manually dispose it
        // after the job completes, instead of relying on the removed [DeallocateOnJobCompletion].
        private NativeArray<float3> tempPositions;

        // Parallel array for colors to pass to MaterialPropertyBlock
        private Vector4[] colors;
        private Matrix4x4[] batchMatrices; // Temp array for graphics copy (max 1023)
        private Matrix4x4[] tempSubmeshMatrices; // Temp array for local offset calculation


        // Bug #5 Fix: Use List<Vector4> so SetVectorArray only sends exactly batchSize
        // elements, rather than always sending all 1023 even for the last partial batch.
        private List<Vector4> colorBatchList;

        private int activeCount = 0;
        private JobHandle crowdJobHandle;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            // Bug #7 Fix: Early return before InitializeArrays to prevent allocating
            // NativeArrays on a duplicate that is about to be destroyed.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeArrays();
        }

        private void InitializeArrays()
        {
            positions = new NativeArray<float3>(maxAgents, Allocator.Persistent);
            velocities = new NativeArray<float3>(maxAgents, Allocator.Persistent);
            targets = new NativeArray<float3>(maxAgents, Allocator.Persistent);
            matrices = new NativeArray<Matrix4x4>(maxAgents, Allocator.Persistent);
            states = new NativeArray<int>(maxAgents, Allocator.Persistent);
            lands = new NativeArray<int>(maxAgents, Allocator.Persistent);

            colors = new Vector4[maxAgents];
            batchMatrices = new Matrix4x4[1023];
            tempSubmeshMatrices = new Matrix4x4[1023];
            agentPrefabs = new GameObject[maxAgents];

            colorBatchList = new List<Vector4>(1023);
            propertyBlock = new MaterialPropertyBlock();

            subMeshes.Clear();
            if (agentPrefab != null)
            {
                Renderer[] renderers = agentPrefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null && r.sharedMaterial != null)
                    {
                        SubMeshInfo info = new SubMeshInfo();
                        info.mesh = mf.sharedMesh;
                        info.material = new Material(r.sharedMaterial);
                        info.material.enableInstancing = true;

                        Matrix4x4 rootTR_Inverse = Matrix4x4.Inverse(Matrix4x4.TRS(agentPrefab.transform.position, agentPrefab.transform.rotation, Vector3.one));
                        Matrix4x4 childLocal = r.transform.localToWorldMatrix;
                        info.localMatrix = rootTR_Inverse * childLocal;

                        subMeshes.Add(info);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Ensure all in-flight jobs are done before disposing any memory.
            crowdJobHandle.Complete();

            // Bug #2/#3 Fix: Manually dispose tempPositions here since we removed
            // [DeallocateOnJobCompletion]. Guard with IsCreated in case it was never allocated.
            if (tempPositions.IsCreated) tempPositions.Dispose();

            if (positions.IsCreated) positions.Dispose();
            if (velocities.IsCreated) velocities.Dispose();
            if (targets.IsCreated) targets.Dispose();
            if (matrices.IsCreated) matrices.Dispose();
            if (states.IsCreated) states.Dispose();
            if (lands.IsCreated) lands.Dispose();
        }

        private void EnsureInitialized()
        {
            if (propertyBlock == null || !positions.IsCreated)
            {
                // We've likely suffered a domain reload in Play Mode. Native arrays are lost.
                InitializeArrays();
                activeCount = 0;
            }
        }

        /// <summary>
        /// Spawns a character and returns its tracking index.
        /// Warning: Indices can change if characters are removed.
        /// </summary>
        public int SpawnCharacter(Vector3 position, Vector3 target, Color color, int landIndex)
        {
            EnsureInitialized();

            // Ensure previous jobs are done before modifying arrays
            crowdJobHandle.Complete();

            if (activeCount >= maxAgents)
            {
                Debug.LogWarning("CrowdManager: Too many agents!");
                return -1;
            }

            int index = activeCount;
            positions[index] = position;
            velocities[index] = float3.zero;
            targets[index] = target;
            states[index] = 0; // Rigidly set in Land initially
            lands[index] = landIndex;
            colors[index] = color;

            if (spawnMode == SpawnMode.Prefab && agentPrefab != null)
            {
                GameObject go = Instantiate(agentPrefab, position, Quaternion.identity, transform);
                agentPrefabs[index] = go;

                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    MaterialPropertyBlock mb = new MaterialPropertyBlock();
                    mb.SetColor(BaseColorID, color);
                    foreach (var r in renderers) {
                        r.SetPropertyBlock(mb);
                    }
                }
            }

            activeCount++;
            return index;
        }

        /// <summary>
        /// Spawns a batch of agents for one "land" column.
        /// </summary>
        public void SpawnLand(int landIndex, int agentCount, Color color, int totalLands = 1, int startRow = 0)
        {
            Debug.Assert(agentCount % 4 == 0, $"SpawnLand: agentCount must be multiple of 4, got {agentCount}");

            float formationWidth = (totalLands - 1) * landSpacingX;
            float offsetX = -formationWidth / 2f;

            Vector3 landCenter = this.transform.position + landBaseOffset + new Vector3(offsetX + landIndex * landSpacingX, 0, 0);

            // Đặt target đích xác ở đầu dải phân cách / giao lộ (để ko bị khoảng hở phía trên)
            // Giao lộ mọc ra ở local Z + 0.4f. Tùy chỉnh 1 xíu để họ đứng ngay mép đường.
            // Phải dùng tọa độ local so với landCenter bới vì targetPos = landCenter + new Vector3(..., frontZ)
            float frontZ = 0.1f; 

            for (int i = 0; i < agentCount; i++)
            {
                int row = startRow + (i / 4);
                int col = i % 4;
                
                // Agents clustered around landCenter.x
                float localX = (col - 1.5f) * agentSpacingX;
                
                // Target is at the very front for this specific column
                Vector3 targetPos = landCenter + new Vector3(localX, 0, frontZ);

                // Spawn positions are arrayed forward along positive Z
                // This makes the pivot at the top (smaller Z) and overflow at the bottom (larger Z)
                Vector3 pos = targetPos + new Vector3(0, 0, row * agentSpacingZ);

                // Target should be their initial position so they stay still at spawn
                // Moving forward is handled later by DispatchGroupToWaitZone
                SpawnCharacter(pos, pos, color, landIndex);
            }

            // Spawn roads under the land
            GameObject roadPrefab = straightRoadPrefab;
            float tSize = landRoadSpacingZ;

            if (roadPrefab == null)
            {
                // Reflection fallback to avoid Assembly dependency CS0234
                UnityEngine.Object[] generators = Resources.FindObjectsOfTypeAll(System.Type.GetType("BusAway.Gameplay.LevelGenerator, LevelSystem") ?? typeof(MonoBehaviour));
                foreach (var g in generators)
                {
                    if (g.GetType().Name == "LevelGenerator")
                    {
                        var field = g.GetType().GetField("straightNS");
                        if (field != null) roadPrefab = field.GetValue(g) as GameObject;
                        break;
                    }
                }
            }

            if (roadPrefab != null)
            {
                GameObject roadGroup = new GameObject($"Roads_Land_{landIndex}");
                roadGroup.transform.SetParent(this.transform);

                // Use Fixed Road Pieces defined in inspector instead of scaling dynamically
                int roadPieces = landFixedRoadPieces;

                for (int p = 0; p < roadPieces; p++)
                {
                    GameObject rw = Instantiate(roadPrefab, roadGroup.transform);
                    // Place it progressively downwards along positive Z
                    rw.transform.position = new Vector3(landCenter.x, 0, landCenter.z + 0.4f + p * tSize + tSize / 2f);
                }
            }

            Debug.Log($"Land[{landIndex}] spawned: {agentCount} agents, color={color}");
        }

        /// <summary>
        /// Gets the current active agent count. Used for testing.
        /// </summary>
        public int GetActiveCountForTesting() => activeCount;

        /// <summary>
        /// Moves a number of agents of a specific color to the BusWaitZone target using Boids (state 1).
        /// Shifts the remaining agents of this color in their land forward simultaneously (rigidly, state 0).
        /// </summary>
        public void DispatchGroupToWaitZone(int targetLandIndex, Color groupColor, int count, Vector3 waitZonePos, int shiftRows)
        {
            EnsureInitialized();
            crowdJobHandle.Complete();

            System.Collections.Generic.List<int> groupColorIndices = new System.Collections.Generic.List<int>();
            for (int i = 0; i < activeCount; i++)
            {
                if (states[i] == 0 && lands[i] == targetLandIndex && Vector4.Distance(colors[i], groupColor) < 0.05f)
                {
                    groupColorIndices.Add(i);
                }
            }

            // front is now the smallest Z (top of the screen)
            groupColorIndices.Sort((a, b) => positions[a].z.CompareTo(positions[b].z));

            HashSet<int> dispatchedSet = new HashSet<int>();
            for (int i = 0; i < groupColorIndices.Count; i++)
            {
                if (i < count)
                {
                    int agentIdx = groupColorIndices[i];
                    states[agentIdx] = 1; 
                    targets[agentIdx] = waitZonePos;
                    dispatchedSet.Add(agentIdx);
                }
            }

            // Shift ALL OTHER remaining agents in this land forward (up the screen -> subtract Z)
            for (int i = 0; i < activeCount; i++)
            {
                if (states[i] == 0 && lands[i] == targetLandIndex && !dispatchedSet.Contains(i))
                {
                    targets[i] = targets[i] - new float3(0, 0, shiftRows * agentSpacingZ);
                }
            }
        }

        /// <summary>
        /// Removes a character by index, swapping with the last agent to keep arrays packed.
        /// </summary>
        public void RemoveCharacter(int index)
        {
            EnsureInitialized();
            crowdJobHandle.Complete();

            if (index < 0 || index >= activeCount) return;

            int lastIndex = activeCount - 1;

            if (spawnMode == SpawnMode.Prefab && agentPrefabs[index] != null)
            {
                Destroy(agentPrefabs[index]);
                agentPrefabs[index] = null;
            }

            // Swap with last active instance if it's not the same
            if (index != lastIndex)
            {
                positions[index] = positions[lastIndex];
                velocities[index] = velocities[lastIndex];
                targets[index] = targets[lastIndex];
                states[index] = states[lastIndex];
                lands[index] = lands[lastIndex];
                colors[index] = colors[lastIndex];
                
                if (spawnMode == SpawnMode.Prefab)
                {
                    agentPrefabs[index] = agentPrefabs[lastIndex];
                    agentPrefabs[lastIndex] = null;
                }
            }

            activeCount--;
        }

        /// <summary>
        /// Changes the target of an active agent.
        /// </summary>
        public void SetTarget(int index, Vector3 newTarget)
        {
            EnsureInitialized();
            crowdJobHandle.Complete();
            if (index >= 0 && index < activeCount)
            {
                targets[index] = newTarget;
            }
        }

        /// <summary>
        /// Finds the agent closest to the given world point (e.g. from a raycast).
        /// </summary>
        public int GetClosestAgent(Vector3 worldPos, float maxDistance = 2.0f)
        {
            EnsureInitialized();
            crowdJobHandle.Complete(); // Make sure positions are valid

            int closestIndex = -1;
            float minDistSqr = maxDistance * maxDistance;

            for (int i = 0; i < activeCount; i++)
            {
                float distSqr = math.distancesq(positions[i], worldPos);
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closestIndex = i;
                }
            }
            return closestIndex;
        }

        /// <summary>
        /// Uses Breadth First Search (BFS) to find all connected agents of the same color.
        /// </summary>
        public List<int> GetConnectedRegion(int startIndex)
        {
            List<int> result = new List<int>();
            if (startIndex < 0 || startIndex >= activeCount) return result;

            EnsureInitialized();
            crowdJobHandle.Complete();

            Vector4 targetColor = colors[startIndex];
            
            // Adjacency threshold (diagonal included: sqrt(x^2 + z^2) ~ 1.414 * spacing)
            float thresholdDistance = Mathf.Max(agentSpacingX, agentSpacingZ) * 1.5f;
            float thresholdSqr = thresholdDistance * thresholdDistance;

            bool[] visited = new bool[activeCount];
            Queue<int> queue = new Queue<int>();

            queue.Enqueue(startIndex);
            visited[startIndex] = true;

            while (queue.Count > 0)
            {
                int curr = queue.Dequeue();
                result.Add(curr);

                float3 currPos = positions[curr];

                // Check all other agents
                for (int i = 0; i < activeCount; i++)
                {
                    if (visited[i]) continue;
                    
                    // Check if SAME Color
                    if (colors[i] == targetColor)
                    {
                        // Check if CONNECTED (by distance)
                        if (math.distancesq(currPos, positions[i]) <= thresholdSqr)
                        {
                            visited[i] = true;
                            queue.Enqueue(i);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts a number of waiting agents (state == 1) of the given color, returning their world positions.
        /// The agents are removed from the CrowdManager.
        /// </summary>
        public List<Vector3> ExtractWaitZoneAgents(Color groupColor, int count)
        {
            EnsureInitialized();
            crowdJobHandle.Complete();

            List<Vector3> results = new List<Vector3>();
            for (int i = activeCount - 1; i >= 0 && results.Count < count; i--)
            {
                if (states[i] == 1 && Vector4.Distance(colors[i], groupColor) < 0.05f)
                {
                    results.Add(positions[i]);
                    RemoveCharacter(i);
                }
            }

            return results;
        }
        private void Update()
        {
            EnsureInitialized();
            if (activeCount == 0) return;

            // 1. Complete previous frame's job. This MUST happen before we dispose
            //    tempPositions and before we allocate a new one.
            crowdJobHandle.Complete();

            // Bug #2/#3 Fix: Manually dispose the previous frame's temp read-only snapshot.
            // Since we removed [DeallocateOnJobCompletion], this is the correct disposal point.
            // crowdJobHandle.Complete() above guarantees the job that owned it is finished.
            if (tempPositions.IsCreated) tempPositions.Dispose();

            // 2. Create a fresh snapshot of positions for the read-only separation check.
            //    This eliminates the Job System aliasing error (R/W to same NativeArray).
            tempPositions = new NativeArray<float3>(activeCount, Allocator.TempJob);
            NativeArray<float3>.Copy(positions, tempPositions, activeCount);

            float3 waitZoneMin = float3.zero;
            float3 waitZoneMax = float3.zero;
            bool constrainToWaitZone = false;
            
            // Find manually to avoid Assembly Def CS0234 (CrowdSystem -> LevelSystem cross-dependency limit)
            GameObject waitZoneObj = GameObject.Find("BusWaitZone");
            if (waitZoneObj != null)
            {
                var road = waitZoneObj.transform.Find("Road");
                if (road != null && road.TryGetComponent<Renderer>(out var renderer))
                {
                    var bounds = renderer.bounds;
                    // Provide a small margin so they don't exactly clip the edges visually
                    waitZoneMin = new float3(bounds.min.x + 0.2f, bounds.min.y, bounds.min.z + 0.2f);
                    waitZoneMax = new float3(bounds.max.x - 0.2f, bounds.max.y, bounds.max.z - 0.2f);
                    constrainToWaitZone = true;
                }
            }

            // 3. Schedule Movement Job for activeCount agents
            var movementJob = new CrowdMovementJob
            {
                deltaTime = Time.deltaTime,
                moveSpeed = moveSpeed,
                separationRadius = separationRadius,
                separationWeight = separationWeight,
                targetWeight = targetWeight,
                arrivalDistance = arrivalDistance,
                activeCount = activeCount,
                waitZoneMin = waitZoneMin,
                waitZoneMax = waitZoneMax,
                constrainToWaitZone = constrainToWaitZone,
                targets = targets,
                allPositions = tempPositions, // Read-only snapshot; disposed manually next frame
                positions = positions,
                velocities = velocities,
                states = states
            };

            JobHandle moveHandle = movementJob.Schedule(activeCount, 64); // Batch size 64

            // 4. Schedule Matrix Job — depends on move job completing first
            var matrixJob = new UpdateMatricesJob
            {
                positions = positions,
                velocities = velocities,
                states = states,
                matrices = matrices
            };

            crowdJobHandle = matrixJob.Schedule(activeCount, 64, moveHandle);
        }

        private void LateUpdate()
        {
            if (activeCount == 0) return;

            // Wait for matrix calculation to be done before rendering
            crowdJobHandle.Complete();

            if (spawnMode == SpawnMode.Prefab)
            {
                for (int i = 0; i < activeCount; i++)
                {
                    if (agentPrefabs[i] != null)
                    {
                        agentPrefabs[i].transform.position = positions[i];
                        float3 vel = velocities[i];
                        if (math.lengthsq(vel) > 0.001f)
                        {
                            agentPrefabs[i].transform.rotation = Quaternion.LookRotation(vel, Vector3.up);
                        }
                    }
                }
            }
            else
            {
                // Only draw if we have agents + mesh + mat
                if (subMeshes.Count == 0) return;

                // Render in batches of 1023 (DrawMeshInstanced hard limit)
                RenderBatches();
            }
        }

        private void RenderBatches()
        {
            int rendered = 0;
            while (rendered < activeCount)
            {
                int batchSize = Mathf.Min(1023, activeCount - rendered);

                colorBatchList.Clear();
                for (int j = 0; j < batchSize; j++)
                {
                    colorBatchList.Add(colors[rendered + j]);
                }
                propertyBlock.SetVectorArray(BaseColorID, colorBatchList);

                NativeArray<Matrix4x4>.Copy(matrices, rendered, batchMatrices, 0, batchSize);

                for (int s = 0; s < subMeshes.Count; s++)
                {
                    var sub = subMeshes[s];
                    Matrix4x4[] drawnMatrices = batchMatrices;

                    if (!sub.localMatrix.isIdentity)
                    {
                        drawnMatrices = tempSubmeshMatrices;
                        for (int i = 0; i < batchSize; i++)
                        {
                            drawnMatrices[i] = batchMatrices[i] * sub.localMatrix;
                        }
                    }

                    Graphics.DrawMeshInstanced(
                        sub.mesh,
                        0,
                        sub.material,
                        drawnMatrices,
                        batchSize,
                        propertyBlock,
                        ShadowCastingMode.On,
                        true, // Receive shadows
                        gameObject.layer
                    );
                }

                rendered += batchSize;
            }
        }

        #region Debug Context Menus
        [ContextMenu("Debug: Spawn 10 Agents")]
        public void DebugSpawn10() => DebugSpawn(10);

        [ContextMenu("Debug: Spawn 100 Agents")]
        public void DebugSpawn100() => DebugSpawn(100);

        [ContextMenu("Debug: Spawn 1000 Agents")]
        public void DebugSpawn1000() => DebugSpawn(1000);

        private void DebugSpawn(int count)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test Crowd System!");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = transform.position + new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));
                Vector3 target = pos + new Vector3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f));
                Color col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                SpawnCharacter(pos, target, col, -1);
            }
        }

        [ContextMenu("Debug: Set Random Targets")]
        public void DebugSetRandomTargets()
        {
            // Bug #4 Fix: Complete any in-flight jobs before reading positions[] on the Main Thread.
            // Previously, this could read/write the same NativeArray concurrently with a running job.
            crowdJobHandle.Complete();

            for (int i = 0; i < activeCount; i++)
            {
                float3 pos = positions[i];
                Vector3 target = new Vector3(pos.x + UnityEngine.Random.Range(-10f, 10f), 0, pos.z + UnityEngine.Random.Range(-10f, 10f));
                SetTarget(i, target);
            }
        }

        [ContextMenu("Debug: Remove Half")]
        public void DebugRemoveHalf()
        {
            int toRemove = activeCount / 2;
            for (int i = 0; i < toRemove; i++)
            {
                RemoveCharacter(UnityEngine.Random.Range(0, activeCount)); // Removing randomly shifts items, but still works
            }
        }

        [ContextMenu("Debug: Clear All")]
        public void DebugClearAll()
        {
            crowdJobHandle.Complete();
            if (spawnMode == SpawnMode.Prefab)
            {
                for (int i = 0; i < activeCount; i++)
                {
                    if (agentPrefabs[i] != null) Destroy(agentPrefabs[i]);
                    agentPrefabs[i] = null;
                }
            }
            activeCount = 0;
        }
        #endregion
    }
}
