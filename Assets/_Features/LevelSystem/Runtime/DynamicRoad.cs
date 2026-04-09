using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DynamicRoad : MonoBehaviour
{
    [Header("Road Path (Define sharp corners)")]
    public List<Vector3> sharpPoints = new List<Vector3>() { 
        new Vector3(0, 0, 0), 
        new Vector3(0, 0, 5), 
        new Vector3(5, 0, 5) 
    };

    [Header("Dimensions")]
    public float roadWidth = 2.0f;
    public float borderThickness = 0.25f;
    
    [Header("Smoothing")]
    public float cornerRadius = 1.5f;
    [Range(3, 20)] 
    public int cornerSegments = 10;
    
    [Header("Rendering")]
    public Material roadMaterial;   
    public Material borderMaterial; 

    private LineRenderer _roadRenderer;
    private LineRenderer _borderRenderer;

    public void SetupFromData(BusAway.Level.RoadSegmentData data)
    {
        this.sharpPoints = new List<Vector3>(data.sharpPoints);
        this.roadWidth = data.roadWidth;
        this.cornerRadius = data.cornerRadius;
        
        GenerateRoad();
    }

    void Update()
    {
        // Auto-update in Editor when values change
        if (!Application.isPlaying) 
        {
            GenerateRoad();
        }
    }

    public void GenerateRoad()
    {
        if (sharpPoints == null || sharpPoints.Count < 2) return;

        // Clear old generated geometry
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        List<Vector3> smoothedPoints = CalculateSmoothPath();

        // 1. Build Border (Thick bottom layer)
        float currentBorderW = roadWidth + borderThickness * 2;
        Build3DPath(smoothedPoints, currentBorderW, 0.2f, 0.0f, borderMaterial, "Border");

        // 2. Build Core (Slightly narrower and sits on top)
        Build3DPath(smoothedPoints, roadWidth, 0.25f, 0.05f, roadMaterial, "Core");
    }

    private void Build3DPath(List<Vector3> points, float width, float height, float yOffset, Material mat, string prefix)
    {
        GameObject group = new GameObject($"{prefix}_Group");
        group.transform.SetParent(this.transform);
        group.transform.localPosition = Vector3.zero;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];

            // Ignore overlapping points
            float distance = Vector3.Distance(p1, p2);
            if (distance < 0.01f) continue;

            // Create Segment (Cube)
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = $"{prefix}_Segment_{i}";
            seg.transform.SetParent(group.transform);

            Vector3 center = (p1 + p2) / 2f;
            center.y = yOffset + height / 2f;
            seg.transform.localPosition = center;

            Vector3 dir = p2 - p1;
            seg.transform.localRotation = Quaternion.LookRotation(dir);

            // Scale: X(Width), Y(Height), Z(Length)
            seg.transform.localScale = new Vector3(width, height, distance);
            if (mat != null) seg.GetComponent<Renderer>().sharedMaterial = mat;

            // Create Joint (Cylinder) to fill corners perfectly
            GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            joint.name = $"{prefix}_Joint_{i}";
            joint.transform.SetParent(group.transform);
            
            Vector3 jPos = p1;
            jPos.y = yOffset + height / 2f;
            joint.transform.localPosition = jPos;
            
            // Default Cylinder is 2 units tall, diameter 1.
            // Scale X and Z to width, Y to half height
            joint.transform.localScale = new Vector3(width, height / 2f, width);
            if (mat != null) joint.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // Final Cap Joint
        if (points.Count > 0)
        {
            GameObject endJoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            endJoint.name = $"{prefix}_Joint_End";
            endJoint.transform.SetParent(group.transform);
            
            Vector3 jPos = points[points.Count - 1];
            jPos.y = yOffset + height / 2f;
            endJoint.transform.localPosition = jPos;
            endJoint.transform.localScale = new Vector3(width, height / 2f, width);
            if (mat != null) endJoint.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    /// <summary>
    /// Converts sharp corner points into a smoothed Quadratic Bezier curve
    /// </summary>
    public List<Vector3> CalculateSmoothPath()
    {
        List<Vector3> path = new List<Vector3>();
        
        for (int i = 0; i < sharpPoints.Count; i++)
        {
            Vector3 current = sharpPoints[i];

            if (i == 0 || i == sharpPoints.Count - 1)
            {
                path.Add(current); // Preserve start and end nodes
                continue;
            }

            Vector3 prev = sharpPoints[i - 1];
            Vector3 next = sharpPoints[i + 1];

            Vector3 dirPrev = (prev - current).normalized;
            Vector3 dirNext = (next - current).normalized;

            // Cap CornerRadius so curve segment doesn't overlap onto next joints
            float distPrev = Vector3.Distance(prev, current);
            float distNext = Vector3.Distance(next, current);
            float actualRadius = Mathf.Min(cornerRadius, distPrev / 2.01f, distNext / 2.01f);

            Vector3 curveStart = current + dirPrev * actualRadius;
            Vector3 curveEnd = current + dirNext * actualRadius;

            // Insert interpolated curve nodes
            for (int s = 0; s <= cornerSegments; s++)
            {
                float t = s / (float)cornerSegments;
                Vector3 pointOnCurve = GetQuadraticBezierPoint(t, curveStart, current, curveEnd);
                path.Add(pointOnCurve);
            }
        }
        return path;
    }

    private Vector3 GetQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }

    // Draw Handles in Scene View for visual editing
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < sharpPoints.Count; i++)
        {
            Vector3 worldPt = transform.TransformPoint(sharpPoints[i]);
            Gizmos.DrawSphere(worldPt, 0.2f);
            if (i < sharpPoints.Count - 1)
            {
                Vector3 nextPt = transform.TransformPoint(sharpPoints[i+1]);
                Gizmos.DrawLine(worldPt, nextPt);
            }
        }
    }
}
