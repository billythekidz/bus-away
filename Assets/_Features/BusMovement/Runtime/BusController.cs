using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BusMovement
{
    public class BusController : MonoBehaviour
    {
        public Color busColor;
        public int currentPassengerCount = 0;

        [Header("Bus Parts")]
        public Transform busFloor;
        public Transform busWallF;
        public Transform busWallR;
        public Transform busWallB;
        public Transform busWallL;


        [Header("Wheels")]
        public Transform wheelFL;
        public Transform wheelFR;
        public Transform wheelBL;
        public Transform wheelBR;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float turnSpeed = 360f;
        public float wheelRadius = 0.5f;

        [Header("VFX & Animation")]
        public ParticleSystem exhaustVFX;
        public TrailRenderer[] skidMarks;
        public ParticleSystem sparkBlingVFX;

        [HideInInspector]
        public Vector2Int currentGridPos;
        [HideInInspector]
        public Vector2Int previousGridPos;
        [HideInInspector]
        public Vector2Int targetGridPos;
        [HideInInspector]
        public bool isMoving;

        private PrimeTween.Sequence moveSequence;
        private PrimeTween.Sequence brakeSequence;
        private PrimeTween.Tween wobbleTween;
        private PrimeTween.Tween idleTween;
        private Transform visualContainer;
        private Vector3 lastPos;

        public event System.Action<BusController> OnPathComplete;

        private void Awake()
        {
            // Dynamically wrap the visuals so we can wobble/jerk them together cleanly
            visualContainer = new GameObject("VisualContainer").transform;
            visualContainer.SetParent(transform, false);


            if (busFloor) busFloor.SetParent(visualContainer, true);
            if (busWallF) busWallF.SetParent(visualContainer, true);
            if (busWallR) busWallR.SetParent(visualContainer, true);
            if (busWallB) busWallB.SetParent(visualContainer, true);
            if (busWallL) busWallL.SetParent(visualContainer, true);

            if (skidMarks != null)
            {
                foreach (var sm in skidMarks)
                {
                    if (sm != null)

                    {
                        sm.time *= 0.33f;
                    }
                }
            }

            lastPos = transform.position;

            StartVibration();
        }

        private void StartVibration()
        {
            if (!idleTween.isAlive && visualContainer != null)
            {
                // Engine vibrate (slight rapid vertical shake)
                idleTween = PrimeTween.Tween.LocalPosition(visualContainer, new Vector3(0, 0.02f, 0), 0.035f, PrimeTween.Ease.InOutSine, cycles: -1, cycleMode: PrimeTween.CycleMode.Yoyo);
            }
        }

        private void StopVibration()
        {
            if (idleTween.isAlive)
            {
                idleTween.Stop();
                if (visualContainer != null) visualContainer.localPosition = Vector3.zero;
            }
        }

        private void Update()
        {
            if (moveSequence.isAlive)
            {
                float distanceMoved = Vector3.Distance(transform.position, lastPos);
                if (distanceMoved > 0.0001f)
                {
                    float rotDelta = (distanceMoved / (2f * Mathf.PI * wheelRadius)) * 360f;
                    SpinWheels(rotDelta);
                }
            }
            lastPos = transform.position;
        }

        public void MoveAlongPath(List<Vector3> pathPoints, bool isFinalStop = false)
        {
            if (pathPoints == null || pathPoints.Count == 0) return;

            isMoving = true;

            moveSequence.Stop();
            wobbleTween.Stop();
            brakeSequence.Stop();
            StopVibration();


            if (visualContainer)

            {
                visualContainer.localRotation = Quaternion.identity;
                visualContainer.localPosition = Vector3.zero;
            }


            if (exhaustVFX != null && !exhaustVFX.isPlaying) exhaustVFX.Play();
            SetSkidMarksEmitting(true);

            moveSequence = PrimeTween.Sequence.Create();


            Vector3 currentPos = transform.position;

            foreach (var point in pathPoints)
            {
                Vector3 targetFlat = new Vector3(point.x, transform.position.y, point.z);
                float distance = Vector3.Distance(currentPos, targetFlat);
                if (distance < 0.01f) continue;


                float duration = distance / moveSpeed;

                // Translation & Rotation in parallel
                var posTween = PrimeTween.Tween.Position(transform, targetFlat, duration, PrimeTween.Ease.Linear);
                moveSequence.Chain(posTween);

                Vector3 direction = (targetFlat - currentPos).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion startRot = transform.rotation;
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    float rotDuration = Mathf.Min(duration, duration * 0.75f); // Soft curve cornering


                    moveSequence.Group(PrimeTween.Tween.Custom(0f, 1f, rotDuration, onValueChange: t =>

                    {
                        if (this != null && transform != null)
                            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    }, ease: PrimeTween.Ease.InOutQuad));
                }

                currentPos = targetFlat;
            }

            // Start Wobbling the body (Chòng chành)
            wobbleTween = PrimeTween.Tween.LocalRotation(visualContainer, new Vector3(0, 0, 3f), 0.2f, PrimeTween.Ease.InOutSine, cycles: -1, cycleMode: PrimeTween.CycleMode.Yoyo);

            // On Complete handling
            moveSequence.OnComplete(() =>
            {
                isMoving = false;
                if (isFinalStop)
                {
                    wobbleTween.Stop();
                    if (exhaustVFX != null) exhaustVFX.Stop();
                    SetSkidMarksEmitting(false);

                    // Khựng xe tạo cảm giác phanh
                    brakeSequence.Stop();
                    brakeSequence = PrimeTween.Sequence.Create()
                        .Chain(PrimeTween.Tween.LocalRotation(visualContainer, new Vector3(6f, 0, 0), 0.15f, PrimeTween.Ease.OutQuad))
                        .Chain(PrimeTween.Tween.LocalRotation(visualContainer, Vector3.zero, 0.25f, PrimeTween.Ease.OutBounce))
                        .OnComplete(() =>
                        {
                            StartVibration();
                            OnPathComplete?.Invoke(this);
                        });
                }
                else
                {
                    OnPathComplete?.Invoke(this);
                }
            });
        }


        private void SpinWheels(float degreesDelta)
        {
            if (wheelFL) wheelFL.Rotate(degreesDelta, 0, 0, Space.Self);
            if (wheelFR) wheelFR.Rotate(degreesDelta, 0, 0, Space.Self);
            if (wheelBL) wheelBL.Rotate(degreesDelta, 0, 0, Space.Self);
            if (wheelBR) wheelBR.Rotate(degreesDelta, 0, 0, Space.Self);
        }

        private void SetSkidMarksEmitting(bool emit)
        {
            if (skidMarks != null)
            {
                foreach (var mk in skidMarks)
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

        public void SetColor(Color baseColor)
        {
            this.busColor = baseColor;
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            // Thay đổi value (độ sáng) của màu nền (sáng hơn xíu) và tường (đậm hơn xíu)
            // Đảm bảo cắp giá trị không bị vượt quá 1 hoặc dưới 0
            Color floorColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v + 0.15f));
            Color wallColor = Color.HSVToRGB(h, s, Mathf.Clamp01(v - 0.15f));

            ApplyColorToProp(busFloor, floorColor);
            ApplyColorToProp(busWallF, wallColor);
            ApplyColorToProp(busWallR, wallColor);
            ApplyColorToProp(busWallB, wallColor);
            ApplyColorToProp(busWallL, wallColor);
        }

        private void ApplyColorToProp(Transform part, Color color)
        {
            if (part == null) return;

            // Tìm Renderer trên chính object đó hoặc thư mục con

            var renderer = part.GetComponent<Renderer>();
            if (renderer == null) renderer = part.GetComponentInChildren<Renderer>();


            if (renderer != null)
            {
                // Dùng MaterialPropertyBlock để đổi màu mà không cần sinh thêm (instantiate) Material mới -> Optimize hiệu năng
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color); // URP thường dùng _BaseColor
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
