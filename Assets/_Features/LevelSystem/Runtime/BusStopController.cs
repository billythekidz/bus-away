using UnityEngine;

namespace BusAway.Gameplay
{
    public class BusStopController : MonoBehaviour
    {
        [Header("Data")]
        public Vector2Int gridPosition;
        public Color stopColor = Color.white;

        [Header("Visuals")]
        public Renderer[] coloredParts;

        private void Awake()
        {
            if (coloredParts == null || coloredParts.Length == 0)
            {
                // Auto-fetch renderers if not explicitly assigned
                coloredParts = GetComponentsInChildren<Renderer>();
            }
        }

        public void SetColor(Color newColor)
        {
            stopColor = newColor;

            if (coloredParts == null || coloredParts.Length == 0) return;

            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (var r in coloredParts)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", newColor);
                r.SetPropertyBlock(block);
            }
        }
    }
}
