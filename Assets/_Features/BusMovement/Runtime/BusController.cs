using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BusMovement
{
    public class BusController : MonoBehaviour
    {
        [Header("Bus Parts")]
        public Transform busFloor;
        public Transform busWallF;
        public Transform busWallR;
        public Transform busWallB;
        
        [Header("Wheels")]
        public Transform wheelFL;
        public Transform wheelFR;
        public Transform wheelBL;
        public Transform wheelBR;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float turnSpeed = 180f;
        public float wheelRadius = 0.5f;

        [Header("VFX & Animation")]
        public ParticleSystem exhaustVFX;
        public Animator busAnimator;
        public TrailRenderer[] skidMarks;
        public ParticleSystem sparkBlingVFX;
        
        private bool isMoving = false;
        private Coroutine moveRoutine;

        public void MoveAlongPath(List<Vector3> pathPoints)
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveRoutine(pathPoints));
        }

        private IEnumerator MoveRoutine(List<Vector3> pathPoints)
        {
            isMoving = true;
            if (exhaustVFX != null && !exhaustVFX.isPlaying) exhaustVFX.Play();
            if (busAnimator != null) busAnimator.SetBool("IsMoving", true);
            SetSkidMarksEmitting(true);

            foreach (var point in pathPoints)
            {
                // Simple 2D distance check ignoring Y 
                Vector3 targetFlat = new Vector3(point.x, transform.position.y, point.z);

                while (Vector3.Distance(transform.position, targetFlat) > 0.05f)
                {
                    Vector3 direction = (targetFlat - transform.position).normalized;
                    
                    // Rotate towards destination
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                    }

                    // Move
                    float distanceToMove = moveSpeed * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, targetFlat, distanceToMove);
                    
                    // Animate Wheels
                    float rotationDelta = (distanceToMove / (2f * Mathf.PI * wheelRadius)) * 360f;
                    SpinWheels(rotationDelta);

                    yield return null;
                    targetFlat = new Vector3(point.x, transform.position.y, point.z); // Update in case path points move
                }
            }

            isMoving = false;
            if (exhaustVFX != null) exhaustVFX.Stop();
            if (busAnimator != null) busAnimator.SetBool("IsMoving", false);
            SetSkidMarksEmitting(false);
        }

        private void SpinWheels(float degrees)
        {
            // Usually wheels rotate along X axis, adjust Space and Axis as needed for the BusV2 mesh
            if (wheelFL) wheelFL.Rotate(degrees, 0, 0, Space.Self);
            if (wheelFR) wheelFR.Rotate(degrees, 0, 0, Space.Self);
            if (wheelBL) wheelBL.Rotate(degrees, 0, 0, Space.Self);
            if (wheelBR) wheelBR.Rotate(degrees, 0, 0, Space.Self);
        }

        private void SetSkidMarksEmitting(bool emit)
        {
            if (skidMarks != null)
            {
                foreach(var mk in skidMarks)
                {
                    if (mk != null) mk.emitting = emit;
                }
            }
        }

        public void OnAgentBoarded()
        {
            if (sparkBlingVFX != null)
            {
                sparkBlingVFX.Play();
            }
        }
    }
}
