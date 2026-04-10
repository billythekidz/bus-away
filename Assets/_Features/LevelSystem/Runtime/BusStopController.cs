using UnityEngine;
using TMPro;

namespace BusAway.Gameplay
{
    public class BusStopController : MonoBehaviour
    {
        [Header("Data")]
        public Vector2Int gridPosition;
        public Color stopColor = Color.white;

        [Header("Visuals")]
        public Renderer[] coloredParts;
        public TextMeshPro numberText;

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

        public void SetNumber(int number)
        {
            if (numberText == null)
            {
                // Auto create a TMP text on the roof
                GameObject txtObj = new GameObject("RoofNumberText");
                txtObj.transform.SetParent(this.transform);

                // Find max Y in local space approximately based on renderer bounds
                // Because prop might have scale x2, we find relative top
                float localMaxY = 1.0f;
                if (coloredParts != null && coloredParts.Length > 0)
                {
                    Bounds b = coloredParts[0].bounds;
                    foreach (var r in coloredParts)
                    {
                        if (r != null) b.Encapsulate(r.bounds);
                    }
                    // Transform world bounds max Y back to local
                    localMaxY = this.transform.InverseTransformPoint(new Vector3(0, b.max.y, 0)).y;
                }

                txtObj.transform.localPosition = new Vector3(0, localMaxY + 0.05f, 0.25f);
                txtObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);

                numberText = txtObj.AddComponent<TextMeshPro>();
                numberText.alignment = TextAlignmentOptions.Center;
                numberText.fontSize = 3.5f;
                numberText.fontStyle = FontStyles.Bold;
                numberText.color = Color.white;
            }

            numberText.text = number.ToString();
        }
    }
}
