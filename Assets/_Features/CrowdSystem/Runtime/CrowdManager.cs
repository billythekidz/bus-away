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
        public static CrowdManager Instance { get; private set; }

        [Header("Settings")]
        public int maxAgents = 1000;
        public float moveSpeed = 3f;
        public float separationRadius = 1.0f;
        public float separationWeight = 1.5f;
        public float targetWeight = 1.0f;
        public float arrivalDistance = 0.1f;

        [Header("Land Spawning")]
        public float landSpacingX = 4.0f;
        public float agentSpacingX = 0.6f;
        public float agentSpacingZ = 0.6f;
        public Vector3 landBaseOffset = new Vector3(0, 0, -2.0f);
        
        [Header("Rendering")]
        public Mesh agentMesh;
        public Material agentMaterial;

        // Native Arrays for Jobs
        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private NativeArray<float3> targets;
        private NativeArray<Matrix4x4> matrices;

        // Bug #2/#3 Fix: tempPositions stored as a field so we can manually dispose it
        // after the job completes, instead of relying on the removed [DeallocateOnJobCompletion].
        private NativeArray<float3> tempPositions;

        // Parallel array for colors to pass to MaterialPropertyBlock
        private Vector4[] colors;
        private Matrix4x4[] batchMatrices; // Temp array for graphics copy (max 1023)

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

            if (agentMaterial != null)
            {
                agentMaterial.enableInstancing = true;
            }
        }

        private void InitializeArrays()
        {
            positions = new NativeArray<float3>(maxAgents, Allocator.Persistent);
            velocities = new NativeArray<float3>(maxAgents, Allocator.Persistent);
            targets = new NativeArray<float3>(maxAgents, Allocator.Persistent);
            matrices = new NativeArray<Matrix4x4>(maxAgents, Allocator.Persistent);
            
            colors = new Vector4[maxAgents];
            batchMatrices = new Matrix4x4[1023];

            // Bug #5 Fix: pre-allocate the List with max capacity to avoid resizing.
            colorBatchList = new List<Vector4>(1023);

            propertyBlock = new MaterialPropertyBlock();
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
        public int SpawnCharacter(Vector3 position, Vector3 target, Color color)
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
            colors[index] = color;

            activeCount++;
            return index;
        }

        /// <summary>
        /// Spawns a batch of agents for one "land" column.
        /// </summary>
        public void SpawnLand(int landIndex, int agentCount, Color color)
        {
            Debug.Assert(agentCount % 4 == 0, $"SpawnLand: agentCount must be multiple of 4, got {agentCount}");

            Vector3 basePos = landBaseOffset + new Vector3(landIndex * landSpacingX, 0, 0);

            for (int i = 0; i < agentCount; i++)
            {
                int row = i / 4;
                int col = i % 4;
                Vector3 pos = basePos + new Vector3(col * agentSpacingX, 0, -row * agentSpacingZ);

                SpawnCharacter(pos, pos, color);
            }

            Debug.Log($"Land[{landIndex}] spawned: {agentCount} agents, color={color}");
        }

        /// <summary>
        /// Gets the current active agent count. Used for testing.
        /// </summary>
        public int GetActiveCountForTesting() => activeCount;

        /// <summary>
        /// Removes a character by index, swapping with the last agent to keep arrays packed.
        /// </summary>
        public void RemoveCharacter(int index)
        {
            EnsureInitialized();
            crowdJobHandle.Complete();

            if (index < 0 || index >= activeCount) return;

            int lastIndex = activeCount - 1;
            
            // Swap with last active instance if it's not the same
            if (index != lastIndex)
            {
                positions[index] = positions[lastIndex];
                velocities[index] = velocities[lastIndex];
                targets[index] = targets[lastIndex];
                colors[index] = colors[lastIndex];
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
                targets = targets,
                allPositions = tempPositions, // Read-only snapshot; disposed manually next frame
                positions = positions,
                velocities = velocities
            };

            JobHandle moveHandle = movementJob.Schedule(activeCount, 64); // Batch size 64

            // 4. Schedule Matrix Job — depends on move job completing first
            var matrixJob = new UpdateMatricesJob
            {
                positions = positions,
                velocities = velocities,
                matrices = matrices
            };

            crowdJobHandle = matrixJob.Schedule(activeCount, 64, moveHandle);
        }

        private void LateUpdate()
        {
            if (activeCount == 0 || agentMesh == null || agentMaterial == null) return;

            // Wait for matrix calculation to be done before rendering
            crowdJobHandle.Complete();

            // Render in batches of 1023 (DrawMeshInstanced hard limit)
            RenderBatches();
        }

        private void RenderBatches()
        {
            int rendered = 0;
            while (rendered < activeCount)
            {
                int batchSize = Mathf.Min(1023, activeCount - rendered);

                // Bug #5 Fix: Build the color list with exactly batchSize entries.
                // Previously, batchColors was always 1023 elements even for partial batches,
                // causing SetVectorArray to send stale data from the previous batch.
                colorBatchList.Clear();
                for (int j = 0; j < batchSize; j++)
                {
                    colorBatchList.Add(colors[rendered + j]);
                }
                propertyBlock.SetVectorArray(BaseColorID, colorBatchList);

                // Copy matrices from NativeArray to managed array for DrawMeshInstanced.
                // Cheap for <= 1023 items.
                NativeArray<Matrix4x4>.Copy(matrices, rendered, batchMatrices, 0, batchSize);

                Graphics.DrawMeshInstanced(
                    agentMesh,
                    0,
                    agentMaterial,
                    batchMatrices,
                    batchSize,
                    propertyBlock,
                    ShadowCastingMode.On,
                    true, // Receive shadows
                    gameObject.layer
                );

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
                SpawnCharacter(pos, target, col);
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
            activeCount = 0;
        }
        #endregion
    }
}
