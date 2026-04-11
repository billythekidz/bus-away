using UnityEngine;

namespace BusAway.Gameplay
{
    /// <summary>
    /// Automatically adjusts the camera FOV to fit the entire level from its fixed position.
    /// Calculates combined bounds of all renderers under the roads root and finds the optimal FOV.
    /// </summary>
    public class LevelCameraFramer : MonoBehaviour
    {
        [Header("3D Framing Settings")]
        [Tooltip("Extra margin around the level bounds to prevent clipping at screen edges")]
        public float padding = 4f;

        /// <summary>
        /// Adjusts the camera FOV to show all 3D geometry within the specified root transform,
        /// keeping its current position and rotation intact.
        /// </summary>
        public void FrameLevel(Transform roadsRoot)
        {
            if (Camera.main == null)
            {
                Debug.LogWarning("[LevelCameraFramer] Camera.main not found! Make sure your main camera is tagged as 'MainCamera'.");
                return;
            }

            Camera cam = Camera.main;

            // 1. Gather all Renderers
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

            // Apply padding to bounds
            totalBounds.Expand(padding);

            // 3. Get 8 corners of the bounding box
            Vector3 center = totalBounds.center;
            Vector3 extents = totalBounds.extents;
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[3] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[6] = center + new Vector3(-extents.x, extents.y, extents.z);
            corners[7] = center + new Vector3(extents.x, extents.y, extents.z);

            // 4. Transform to camera local space and calculate max required tangent angles
            float maxTan = 0f;
            foreach (Vector3 corner in corners)
            {
                Vector3 localPos = cam.transform.InverseTransformPoint(corner);
                // Ensure the corner is in front of the camera to avoid negative/zero division
                if (localPos.z <= 0.01f)
                {
                    continue; // Skip calculating FOV for point behind or too close
                }

                // Required vertical tan for this point's Y distance
                float tanY = Mathf.Abs(localPos.y) / localPos.z;
                
                // Required vertical tan for this point's X distance, accounting for screen aspect ratio
                float tanX = (Mathf.Abs(localPos.x) / localPos.z) / cam.aspect;

                maxTan = Mathf.Max(maxTan, tanY, tanX);
            }

            if (maxTan > 0)
            {
                cam.orthographic = false;
                cam.fieldOfView = 2f * Mathf.Atan(maxTan) * Mathf.Rad2Deg;
                Debug.Log($"<color=green>[LevelCameraFramer]</color> FOV adjusted to: {cam.fieldOfView:F1}");
            }
            else
            {
                Debug.LogWarning("[LevelCameraFramer] Could not calculate FOV. Camera might be inside the map bounds or looking away.");
            }
        }
    }
}
