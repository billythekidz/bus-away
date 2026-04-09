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
        // Chạy thẳng trong Editor khi thay đổi thông số
        if (!Application.isPlaying) 
        {
            GenerateRoad();
        }
    }

    public void GenerateRoad()
    {
        if (sharpPoints == null || sharpPoints.Count < 2) return;

        EnsureRenderersSetup();

        // 1. Tính toán đường cong (local space)
        List<Vector3> smoothedPoints = CalculateSmoothPath();

        // Convert local points sang World Space để truyền cho LineRenderers
        Vector3[] borderPoints = new Vector3[smoothedPoints.Count];
        Vector3[] innerPoints = new Vector3[smoothedPoints.Count];
        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(smoothedPoints[i]);
            borderPoints[i] = worldPos;
            innerPoints[i] = worldPos + new Vector3(0, 0.05f, 0); // Nhấc lõi lên một chút
        }

        // 2. Vẽ viền ngoài (nằm dưới)
        _borderRenderer.positionCount = borderPoints.Length;
        _borderRenderer.SetPositions(borderPoints);
        _borderRenderer.startWidth = roadWidth + borderThickness * 2;
        _borderRenderer.endWidth = roadWidth + borderThickness * 2;

        // 3. Vẽ lõi đường (nằm trên, nhỏ hơn)
        _roadRenderer.positionCount = innerPoints.Length;
        _roadRenderer.SetPositions(innerPoints);
        _roadRenderer.startWidth = roadWidth;
        _roadRenderer.endWidth = roadWidth;
    }

    private void EnsureRenderersSetup()
    {
        // Setup Border LineRenderer (Chuyển thành Child Object để ko ảnh hưởng Transform gốc)
        if (_borderRenderer == null)
        {
            Transform borderT = transform.Find("Road_Border");
            if (borderT == null)
            {
                GameObject borderGo = new GameObject("Road_Border");
                borderGo.transform.SetParent(transform);
                borderGo.transform.localPosition = Vector3.zero;
                borderGo.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                _borderRenderer = borderGo.AddComponent<LineRenderer>();
            }
            else _borderRenderer = borderT.GetComponent<LineRenderer>();
            
            _borderRenderer.useWorldSpace = true;
            _borderRenderer.numCapVertices = 10;
            _borderRenderer.numCornerVertices = 5;
            _borderRenderer.alignment = LineAlignment.TransformZ;
        }

        // Setup Inner Road LineRenderer
        if (_roadRenderer == null)
        {
            Transform innerT = transform.Find("InnerRoad_Fill");
            if (innerT == null)
            {
                GameObject innerGo = new GameObject("InnerRoad_Fill");
                innerGo.transform.SetParent(transform);
                innerGo.transform.localPosition = Vector3.zero;
                innerGo.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                _roadRenderer = innerGo.AddComponent<LineRenderer>();
            }
            else _roadRenderer = innerT.GetComponent<LineRenderer>();
            
            _roadRenderer.useWorldSpace = true;
            _roadRenderer.numCapVertices = 10;
            _roadRenderer.numCornerVertices = 5;
            _roadRenderer.alignment = LineAlignment.TransformZ;
        }

        // Assign materials if exist
        if (borderMaterial) _borderRenderer.sharedMaterial = borderMaterial;
        if (roadMaterial) _roadRenderer.sharedMaterial = roadMaterial;
        
        // Khóa cứng Child Rotation để Z-axis của LineRenderer hướng LÊN TRÊN (Up Vector)
        // Rotation (-90, 0, 0) nghĩa là Z-axis (Forward) bị ngửa lên trời (Top-down)
        _borderRenderer.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        _roadRenderer.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        
        _borderRenderer.alignment = LineAlignment.TransformZ;
        _roadRenderer.alignment = LineAlignment.TransformZ;

        // Xóa cảnh báo từ bản cũ nếu LineRenderer còn dính trên GameObject gốc
        LineRenderer oldSelfRenderer = GetComponent<LineRenderer>();
        if (oldSelfRenderer != null)
        {
            oldSelfRenderer.enabled = false; // Tắt tạm thời tránh lỗi, người dùng có thể Remove manually
        }
    }

    /// <summary>
    /// Chuyển mảng điểm vuông góc thành đường cong chuẩn Quadratic Bezier
    /// </summary>
    public List<Vector3> CalculateSmoothPath()
    {
        List<Vector3> path = new List<Vector3>();
        
        for (int i = 0; i < sharpPoints.Count; i++)
        {
            Vector3 current = sharpPoints[i];

            if (i == 0 || i == sharpPoints.Count - 1)
            {
                path.Add(current); // Điểm đầu và cuối giữ nguyên
                continue;
            }

            Vector3 prev = sharpPoints[i - 1];
            Vector3 next = sharpPoints[i + 1];

            Vector3 dirPrev = (prev - current).normalized;
            Vector3 dirNext = (next - current).normalized;

            // Xài CornerRadius, nhưng giới hạn không để cung tròn vượt quá nửa đoạn đường nối
            float distPrev = Vector3.Distance(prev, current);
            float distNext = Vector3.Distance(next, current);
            float actualRadius = Mathf.Min(cornerRadius, distPrev / 2.01f, distNext / 2.01f);

            Vector3 curveStart = current + dirPrev * actualRadius;
            Vector3 curveEnd = current + dirNext * actualRadius;

            // Chèn cung nội suy
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

    // Vẽ Handle ra Scene View để dễ thiết kế
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
