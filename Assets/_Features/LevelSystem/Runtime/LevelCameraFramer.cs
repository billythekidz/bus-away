using UnityEngine;

namespace BusAway.Gameplay
{
    /// <summary>
    /// Automatically frames the camera to show the entire level from an isometric 3D perspective.
    /// Calculates combined bounds of all renderers under the roads root and positions the camera
    /// at a configurable elevation angle looking at the center.
    /// </summary>
    public class LevelCameraFramer : MonoBehaviour
    {
        [Header("3D Framing Settings")]
        [Tooltip("Extra margin around the level bounds to prevent clipping at screen edges")]
        public float padding = 4f;

        [Tooltip("Elevation angle in degrees (0 = ground level, 90 = top-down)")]
        [Range(20f, 85f)]
        public float elevationAngle = 72f;

        [Tooltip("Horizontal orbit angle in degrees (0 = looking from +Z towards -Z)")]
        public float orbitAngle = 0f;

        [Tooltip("Extra distance multiplier to pull the camera back")]
        [Range(1f, 3f)]
        public float distanceMultiplier = 1.4f;

        [Tooltip("Camera field of view for perspective mode (lower = flatter, more orthographic feel)")]
        public float fieldOfView = 35f;

        /// <summary>
        /// Frame the camera to show all 3D geometry within the specified root transform.
        /// Uses Renderer bounds (works with Cubes, Cylinders, MeshRenderers, etc.)
        /// </summary>
        public void FrameLevel(Transform roadsRoot)
        {
            if (Camera.main == null)
            {
                Debug.LogWarning("[LevelCameraFramer] Camera.main not found! Make sure your main camera is tagged as 'MainCamera'.");
                return;
            }

            Camera cam = Camera.main;

            // 1. Gather all Renderers (Cubes, Cylinders, MeshRenderers, etc.)
            Renderer[] renderers = roadsRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[LevelCameraFramer] No renderers found under roads root.");
                return;
            }

            // 2. Calculate combined Bounding Box of the whole level
            Bounds totalBounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                totalBounds.Encapsulate(r.bounds);
            }

            // 3. Calculate required camera distance to fit everything
            Vector3 center = totalBounds.center;
            float boundsSize = Mathf.Max(totalBounds.size.x, totalBounds.size.z) + padding;

            // Use Perspective projection for true 3D look
            cam.orthographic = false;
            cam.fieldOfView = fieldOfView;

            // Calculate distance needed to fit the bounds in view
            float halfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = (boundsSize * 0.5f) / Mathf.Tan(halfFov);
            distance *= distanceMultiplier;

            // 4. Position camera at elevation + orbit angle
            float elevRad = elevationAngle * Mathf.Deg2Rad;
            float orbitRad = orbitAngle * Mathf.Deg2Rad;

            Vector3 cameraOffset = new Vector3(
                Mathf.Sin(orbitRad) * Mathf.Cos(elevRad) * distance,
                Mathf.Sin(elevRad) * distance,
                Mathf.Cos(orbitRad) * Mathf.Cos(elevRad) * distance
            );

            cam.transform.position = center + cameraOffset;

            // 5. Look at the center of the level
            cam.transform.LookAt(center);

            Debug.Log($"<color=green>[LevelCameraFramer]</color> 3D Isometric framing complete. Distance: {distance:F1}, Center: {center}");
        }
    }
}
