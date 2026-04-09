using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BusAway.CrowdSystem
{
    [BurstCompile]
    public struct CrowdMovementJob : IJobParallelFor
    {
        public float deltaTime;
        public float moveSpeed;
        public float separationRadius;
        public float separationWeight;
        public float targetWeight;
        public float arrivalDistance;
        public int activeCount;

        [ReadOnly] public NativeArray<float3> targets;
        [ReadOnly] public NativeArray<float3> allPositions; // All positions for separation check

        public NativeArray<float3> positions;
        public NativeArray<float3> velocities;

        public void Execute(int index)
        {
            float3 currentPos = positions[index];
            float3 targetPos = targets[index];
            float3 currentVel = velocities[index];

            // 1. Move to Target logic
            float3 toTarget = targetPos - currentPos;
            float distToTarget = math.length(toTarget);
            
            float3 desiredVel = float3.zero;

            if (distToTarget > arrivalDistance)
            {
                desiredVel = (toTarget / distToTarget) * moveSpeed * targetWeight;
            }

            // 2. Separation logic (O(N) check per agent but fast in Burst for N < 2000)
            float3 separationForce = float3.zero;
            int neighborCount = 0;
            float sqrSeparationRadius = separationRadius * separationRadius;

            for (int i = 0; i < activeCount; i++)
            {
                if (i == index) continue;

                float3 otherPos = allPositions[i];
                float3 offset = currentPos - otherPos;
                float sqrDist = math.lengthsq(offset);

                if (sqrDist < sqrSeparationRadius && sqrDist > 0.0001f)
                {
                    float dist = math.sqrt(sqrDist);
                    // Force is stronger when closer
                    separationForce += (offset / dist) * ((separationRadius - dist) / separationRadius);
                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            {
                separationForce /= neighborCount;
                desiredVel += separationForce * moveSpeed * separationWeight;
            }

            // 3. Update velocity and position
            // Simple interpolation for smooth velocity changes
            float3 newVel = math.lerp(currentVel, desiredVel, deltaTime * 10f); // 10f is responsiveness
            velocities[index] = newVel;
            positions[index] = currentPos + newVel * deltaTime;
        }
    }

    [BurstCompile]
    public struct UpdateMatricesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public NativeArray<float3> velocities;
        
        // Matrix mapping to unity's space
        public NativeArray<Matrix4x4> matrices;

        public void Execute(int index)
        {
            float3 pos = positions[index];
            float3 vel = velocities[index];

            // Calculate rotation facing velocity
            quaternion rot = quaternion.identity;
            if (math.lengthsq(vel) > 0.001f)
            {
                // We keep Y flat
                float3 flatVel = new float3(vel.x, 0, vel.z);
                if (math.lengthsq(flatVel) > 0.001f)
                {
                    rot = quaternion.LookRotationSafe(flatVel, new float3(0, 1, 0));
                }
            }

            // Standard Matrix construction: TRS
            matrices[index] = Matrix4x4.TRS(pos, rot, new float3(1, 1, 1));
        }
    }
}
