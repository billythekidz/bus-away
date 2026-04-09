using System;
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
        
        [Header("Rendering")]
        public Mesh agentMesh;
        public Material agentMaterial;

        // Native Arrays for Jobs
        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private NativeArray<float3> targets;
        private NativeArray<Matrix4x4> matrices;
        
        // Parallel array for colors to pass to MaterialPropertyBlock
        private Vector4[] colors;
        private Vector4[] batchColors; // Temp array for max 1023 instances
        private Matrix4x4[] batchMatrices; // Temp array for graphics fallback

        private int activeCount = 0;
        private JobHandle crowdJobHandle;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

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
            batchColors = new Vector4[1023];
            batchMatrices = new Matrix4x4[1023];
            
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            // Ensure jobs are completed before disposing
            crowdJobHandle.Complete();

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

            // 1. Ensure previous frame's job is complete
            crowdJobHandle.Complete();

            // Create a temp copy of positions to avoid Job System aliasing check (reading and writing to same array)
            // 12KB copy is incredibly cheap (0.00x ms)
            NativeArray<float3> tempPositions = new NativeArray<float3>(activeCount, Allocator.TempJob);
            NativeArray<float3>.Copy(positions, tempPositions, activeCount);

            // 2. Schedule Movement Job only for activeCount
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
                allPositions = tempPositions, // This array will be auto-disposed because we added [DeallocateOnJobCompletion]
                positions = positions,
                velocities = velocities
            };

            JobHandle moveHandle = movementJob.Schedule(activeCount, 64); // Batch size 64

            // 3. Schedule Matrix Job
            var matrixJob = new UpdateMatricesJob
            {
                positions = positions,
                velocities = velocities,
                matrices = matrices
            };

            // Matrix job depends on move job
            crowdJobHandle = matrixJob.Schedule(activeCount, 64, moveHandle);
        }

        private void LateUpdate()
        {
            if (activeCount == 0 || agentMesh == null || agentMaterial == null) return;

            // Wait for matrix calculation to be done before rendering
            crowdJobHandle.Complete();

            // Render in batches of 1023
            RenderBatches();
        }

        private void RenderBatches()
        {
            int rendered = 0;
            while (rendered < activeCount)
            {
                int batchSize = Mathf.Min(1023, activeCount - rendered);

                // Copy colors and (if using DrawMeshInstanced) matrices to managed arrays for this batch
                Array.Copy(colors, rendered, batchColors, 0, batchSize);
                
                propertyBlock.SetVectorArray(BaseColorID, batchColors);

                // For absolute compatibility with DrawMeshInstanced (works on all pipelines without NativeArray support)
                // We copy matrices. It's cheap for 1000 items.
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
