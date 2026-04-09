using UnityEngine;

namespace BusAway.Gameplay
{
    /// <summary>
    /// Automatically calculates the combined Bounding Box of the road network and fits it to the screen,
    /// regardless of device aspect ratio (e.g., iPad 4:3 or iPhone 19.5:9).
    /// </summary>
    public class LevelCameraFramer : MonoBehaviour
    {
        [Header("Framing Settings")]
        [Tooltip("Margin around the map boundaries to ensure roads do not touch the screen edges (useful for UI padding)")]
        public float padding = 4f;


        [Tooltip("Minimum orthographic size to prevent extreme zooming on small maps")]
        public float minOrthoSize = 5f;


        [Tooltip("Camera offset when looking down at the map (Top-down view)")]
        public Vector3 cameraPositionOffset = new Vector3(0, 50f, 0);

        /// <summary>
        /// Frame the camera to encapsulate all LineRenderers within the specified root transform.
        /// </summary>
        public void FrameLevel(Transform roadsRoot)
        {
            if (Camera.main == null)
            {
                Debug.LogWarning("Camera.main not found! Make sure your main camera is tagged as 'MainCamera'.");
                return;
            }

            Camera cam = Camera.main;

            // 1. Gather all LineRenderers (containing road vertices)

            LineRenderer[] renderers = roadsRoot.GetComponentsInChildren<LineRenderer>();
            if (renderers.Length == 0) return;

            // 2. Calculate combined Bounding Box of the whole map
            Bounds totalBounds = renderers[0].bounds;
            foreach (var lr in renderers)
            {
                totalBounds.Encapsulate(lr.bounds);
            }

            // 3. Move Camera to the center of the total X-Z bounds (keeping the top-down Y height)
            Vector3 center = totalBounds.center;
            cam.transform.position = new Vector3(center.x, cameraPositionOffset.y, center.z);

            // Force standard Top-down Camera rotation and Orthographic projection

            cam.transform.eulerAngles = new Vector3(90f, 0f, 0f);
            cam.orthographic = true; // Use Orthographic mode for exact bounding calculation

            // 4. Measure Screen Aspect Ratio
            float levelWidth = totalBounds.size.x + padding;
            float levelHeight = totalBounds.size.z + padding;

            float screenAspect = (float)Screen.width / (float)Screen.height;

            // Calculate Orthographic Scale for both scenarios:
            // - Vertical bounds fit (Square/Landscape screens)
            // - Horizontal bounds fit (Narrow/Portrait screens)
            float requiredOrthoVertical = levelHeight / 2f;
            float requiredOrthoHorizontal = (levelWidth / screenAspect) / 2f;

            // Take the maximum required size to ensure no clipping on any edge
            float finalSize = Mathf.Max(requiredOrthoVertical, requiredOrthoHorizontal);
            finalSize = Mathf.Max(finalSize, minOrthoSize);

            // Apply to Camera
            cam.orthographicSize = finalSize;

            Debug.Log($"<color=green>[AutoFramer]</color> Screen Aspect Ratio: {screenAspect:F2}. Ortho Zoom: {finalSize:F1}");
        }
    }
}
