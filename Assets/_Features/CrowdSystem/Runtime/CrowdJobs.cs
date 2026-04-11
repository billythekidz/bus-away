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

        public float3 waitZoneMin;
        public float3 waitZoneMax;
        public bool constrainToWaitZone;

        [ReadOnly] public NativeArray<float3> targets;
        // Bug #2 Fix: Removed deprecated [DeallocateOnJobCompletion] attribute.
        // This attribute was removed in com.unity.collections@2.x.
        // CrowdManager now manually disposes this array after crowdJobHandle.Complete().
        [ReadOnly] public NativeArray<float3> allPositions;

        public NativeArray<float3> positions;
        public NativeArray<float3> velocities;

        [ReadOnly] public NativeArray<int> states;

        public void Execute(int index)
        {
            float3 currentPos = positions[index];
            float3 targetPos = targets[index];
            float3 currentVel = velocities[index];
            int state = states[index];

            float3 toTarget = targetPos - currentPos;
            float distToTarget = math.length(toTarget);
            
            // STATE 0: In Land (Rigid Block Movement, "đồng loạt toàn khối")
            if (state == 0)
            {
                // Move uniformly without pushing each other
                if (distToTarget > 0.001f)
                {
                    float step = moveSpeed * deltaTime;
                    if (distToTarget <= step) 
                    {
                        positions[index] = targetPos;
                    } 
                    else 
                    {
                        positions[index] = currentPos + (toTarget / distToTarget) * step;
                    }
                }
                velocities[index] = float3.zero;
                return;
            }

            // STATE 1: Boids Mode (Moving to BusWaitZone)
            float sqrSeparationRadius = separationRadius * separationRadius;
            // Đám đông tụ tập: giảm ranh giới cá nhân xuống để đứng sát nhau hơn
            float currentSeparationRadius = separationRadius;
            if (state == 1)
            {
                currentSeparationRadius = 0.5f; 
                sqrSeparationRadius = currentSeparationRadius * currentSeparationRadius;
            }

            float3 separationForce = float3.zero;
            int neighborCount = 0;

            for (int i = 0; i < activeCount; i++)
            {
                if (i == index) continue;

                float3 otherPos = allPositions[i];
                float3 offset = currentPos - otherPos;
                
                float sqrDist = math.lengthsq(offset);

                if (sqrDist < sqrSeparationRadius)
                {
                    if (sqrDist < 0.0001f)
                    {
                        offset = new float3(index - i, 0, i - index) * 0.01f;
                        sqrDist = math.lengthsq(offset);
                    }

                    float dist = math.sqrt(sqrDist);
                    // Lực đẩy mềm
                    float ratio = 1.0f - (dist / currentSeparationRadius);
                    
                    // QUICK MATHS: Target Force có độ lớn max = 1.0. 
                    // Để giữ Agent cách nhau một khoảng (vd 0.3m), Lực Đẩy phải lớn hơn 1.0 khi qua mốc 0.3m!
                    // Nhân ratio với 4.0f: Max đẩy là 4.0. Điểm cân bằng với Target(1.0) là tại ratio=0.25 (cách ~0.37m).
                    float forceStrength = ratio * 4.0f;
                    
                    float3 sepDir = offset / dist;
                    separationForce += sepDir * forceStrength; 
                    neighborCount++;
                }
            }

            // Mở giới hạn đẩy cự ly gần để chúng luôn có cơ hội hất văng boids khác 
            // trước khi bị ép bẹp dí vào tường
            float sepLen = math.length(separationForce);
            if (sepLen > 5.0f)
            {
                separationForce = (separationForce / sepLen) * 5.0f;
            }

            // Kéo về target
            float3 seekForce = float3.zero;
            if (distToTarget > arrivalDistance)
            {
                seekForce = toTarget / distToTarget;
            }

            // Hợp lực
            float3 steering = (seekForce * targetWeight) + (separationForce * separationWeight);
            
            if (math.lengthsq(steering) > 1.0f)
            {
                steering = math.normalize(steering);
            }

            float3 desiredVel = steering * moveSpeed;

            // Update velocity and position smoothly
            float3 newVel = math.lerp(currentVel, desiredVel, deltaTime * 10f);
            
            // NGHIÊM CẤM DI CHUYỂN TRỤC Y THEO YÊU CẦU: "force trục Y không đc thay đổi pos y"
            newVel.y = 0f;
            velocities[index] = newVel;
            
            float3 nextPos = currentPos + newVel * deltaTime;
            nextPos.y = currentPos.y; // Giữ nguyên tọa độ cứng Y ngay từ lúc spawn
            
            if (constrainToWaitZone && state == 1)
            {
                nextPos.x = math.clamp(nextPos.x, waitZoneMin.x, waitZoneMax.x);
                nextPos.z = math.clamp(nextPos.z, waitZoneMin.z, waitZoneMax.z);
            }
            positions[index] = nextPos;
        }
    }

    [BurstCompile]
    public struct UpdateMatricesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public NativeArray<float3> velocities;
        [ReadOnly] public NativeArray<int> states;
        
        // Matrix mapping to unity's space
        public NativeArray<Matrix4x4> matrices;

        public void Execute(int index)
        {
            float3 pos = positions[index];
            float3 vel = velocities[index];
            int state = states[index];

            quaternion rot = quaternion.identity;

            if (state == 1)
            {
                // Force agents to strictly face the road (negative Z) without ANY rotation from velocity.
                // This guarantees no jittering, spinning, or tilting.
                rot = quaternion.LookRotationSafe(new float3(0, 0, -1), new float3(0, 1, 0));
            }
            else
            {
                if (math.lengthsq(vel) > 0.001f)
                {
                    // We keep Y flat
                    float3 flatVel = new float3(vel.x, 0, vel.z);
                    if (math.lengthsq(flatVel) > 0.001f)
                    {
                        rot = quaternion.LookRotationSafe(flatVel, new float3(0, 1, 0));
                    }
                }
            }

            // Bug #6 Fix: Use explicit Vector3/Quaternion types instead of float3/quaternion
            // to ensure correct Burst compilation and avoid ambiguous implicit conversions.
            matrices[index] = Matrix4x4.TRS(
                new Vector3(pos.x, pos.y, pos.z),
                new Quaternion(rot.value.x, rot.value.y, rot.value.z, rot.value.w),
                Vector3.one
            );
        }
    }
}
