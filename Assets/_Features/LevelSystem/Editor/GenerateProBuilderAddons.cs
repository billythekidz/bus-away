using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using BusAway.Level;
using System.Collections.Generic;

public class GenerateProBuilderAddons : MonoBehaviour
{
    [MenuItem("Bus Away/Generate ProBuilder Addons")]
    public static void Generate()
    {
        GenerateCrosswalk();
        GenerateBusBay();
        GenerateHalfTLeft();
        GenerateHalfTRight();
        Debug.Log("Generated ProBuilder Cell Types.");
    }

    private static Material GetMaterial(string hex, string name)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (ColorUtility.TryParseHtmlString(hex, out Color col)) mat.color = col;
        mat.name = name;
        return mat;
    }

    private static ProBuilderMesh CreateBox(Vector3 size, Vector3 pos, Material mat, Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        
        ProBuilderMesh pb = go.AddComponent<ProBuilderMesh>();
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,-0.5f), new Vector3(0.5f,-0.5f,0.5f), new Vector3(-0.5f,-0.5f,0.5f),
            new Vector3(-0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,-0.5f), new Vector3(0.5f,0.5f,0.5f), new Vector3(-0.5f,0.5f,0.5f)
        };
        
        Face[] faces = new Face[]
        {
            new Face(new int[] { 0,1,2, 0,2,3 }), // bottom
            new Face(new int[] { 4,7,6, 4,6,5 }), // top
            new Face(new int[] { 0,4,5, 0,5,1 }), // front
            new Face(new int[] { 1,5,6, 1,6,2 }), // right
            new Face(new int[] { 2,6,7, 2,7,3 }), // back
            new Face(new int[] { 3,7,4, 3,4,0 })  // left
        };

        for (int i=0; i<vertices.Length; i++) vertices[i] = Vector3.Scale(vertices[i], size);

        pb.RebuildWithPositionsAndFaces(vertices, faces);
        pb.GetComponent<Renderer>().sharedMaterial = mat;
        pb.Refresh();
        pb.ToMesh();
        
        return pb;
    }

    private static void GenerateCrosswalk()
    {
        GameObject root = new GameObject("Tile_Crosswalk_Real");
        Material borderMat = GetMaterial("#FFFFFF", "RoadBorder");
        Material asphaltMat = GetMaterial("#333847", "RoadCore");
        Material whitePaint = GetMaterial("#FFFFFF", "WhitePaint");

        float tSize = 1f;
        float aWidth = tSize * 0.75f;
        
        // Base
        CreateBox(new Vector3(1f, 0.1f, 1f), new Vector3(0, 0.05f, 0), borderMat, root.transform, "Border");
        // Asphalt
        CreateBox(new Vector3(aWidth, 0.11f, 1f), new Vector3(0, 0.055f, 0), asphaltMat, root.transform, "Asphalt");

        // Zebra Crossing (5 strips)
        for (int i = -2; i <= 2; i++)
        {
            float z = i * 0.15f;
            CreateBox(new Vector3(0.4f, 0.115f, 0.08f), new Vector3(0, 0.057f, z), whitePaint, root.transform, $"Stripe_{i}");
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Features/LevelSystem/Art/Tile_Crosswalk_Real.prefab");
        DestroyImmediate(root);
    }

    private static void GenerateBusBay()
    {
        GameObject root = new GameObject("Tile_BusStopBay_Real");
        Material borderMat = GetMaterial("#FFFFFF", "RoadBorder");
        Material asphaltMat = GetMaterial("#333847", "RoadCore");
        Material yellowPaint = GetMaterial("#FFD700", "YellowPaint");

        float tSize = 1f;

        // Base
        CreateBox(new Vector3(1f, 0.1f, 1f), new Vector3(0, 0.05f, 0), borderMat, root.transform, "Border");
        
        // Asphalt (Wider on the right side for the bay)
        // Normal width = 0.75 (from -0.375 to +0.375).
        // Bay width = we push the right side to +0.45.
        // Center of asphalt: X = 0.0375. Width = 0.825.
        CreateBox(new Vector3(0.825f, 0.11f, 1f), new Vector3(0.0375f, 0.055f, 0), asphaltMat, root.transform, "Asphalt_Bay");

        // Yellow Bus Stop markings
        CreateBox(new Vector3(0.81f, 0.115f, 0.05f), new Vector3(0.0375f, 0.057f, -0.4f), yellowPaint, root.transform, "BayLineFront");
        CreateBox(new Vector3(0.81f, 0.115f, 0.05f), new Vector3(0.0375f, 0.057f, 0.4f), yellowPaint, root.transform, "BayLineBack");

        // Shelter box (Representational)
        Material shelterMat = GetMaterial("#5588DD", "Shelter");
        CreateBox(new Vector3(0.1f, 0.25f, 0.3f), new Vector3(0.46f, 0.15f, 0), shelterMat, root.transform, "Shelter");
        CreateBox(new Vector3(0.15f, 0.02f, 0.35f), new Vector3(0.43f, 0.27f, 0), shelterMat, root.transform, "Shelter_Roof");

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Features/LevelSystem/Art/Tile_BusStopBay_Real.prefab");
        DestroyImmediate(root);
    }
    private static void GenerateHalfTLeft()
    {
        GameObject root = new GameObject("Tile_HalfT_Left");
        Material borderMat = GetMaterial("#FFFFFF", "RoadBorder");
        Material asphaltMat = GetMaterial("#333847", "RoadCore");

        // Base (White Border) covers entire cell
        CreateBox(new Vector3(1f, 0.1f, 1f), new Vector3(0, 0.05f, 0), borderMat, root.transform, "Border");
        
        // Asphalt Main (South border avoided, z = -0.4 to 0.4)
        CreateBox(new Vector3(1f, 0.11f, 0.8f), new Vector3(0, 0.055f, 0), asphaltMat, root.transform, "Asphalt_Main");

        // Asphalt North (Top-left 0.1x0.1 notch is avoided)
        // Asphalt covers x = -0.4 to 0.5; z = 0.4 to 0.5
        CreateBox(new Vector3(0.9f, 0.11f, 0.1f), new Vector3(0.05f, 0.055f, 0.45f), asphaltMat, root.transform, "Asphalt_North");

        // Curved Notch Filler (fills the 90 degree gap to be a smooth curve)
        Vector2[] arcPoly = new Vector2[] {
            new Vector2(-0.4f, 0.4f),
            new Vector2(-0.4f, 0.5f),
            new Vector2(-0.5f + 0.1f * Mathf.Cos(-22.5f * Mathf.Deg2Rad), 0.5f + 0.1f * Mathf.Sin(-22.5f * Mathf.Deg2Rad)),
            new Vector2(-0.5f + 0.1f * Mathf.Cos(-45.0f * Mathf.Deg2Rad), 0.5f + 0.1f * Mathf.Sin(-45.0f * Mathf.Deg2Rad)),
            new Vector2(-0.5f + 0.1f * Mathf.Cos(-67.5f * Mathf.Deg2Rad), 0.5f + 0.1f * Mathf.Sin(-67.5f * Mathf.Deg2Rad)),
            new Vector2(-0.5f, 0.4f)
        };
        CreateConvexExtrusion(arcPoly, 0.11f, 0.055f, asphaltMat, root.transform, "Asphalt_CurveFiller");

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Features/LevelSystem/Art/Tile_HalfT_Left.prefab");
        DestroyImmediate(root);
    }

    private static void GenerateHalfTRight()
    {
        GameObject root = new GameObject("Tile_HalfT_Right");
        Material borderMat = GetMaterial("#FFFFFF", "RoadBorder");
        Material asphaltMat = GetMaterial("#333847", "RoadCore");

        CreateBox(new Vector3(1f, 0.1f, 1f), new Vector3(0, 0.05f, 0), borderMat, root.transform, "Border");
        CreateBox(new Vector3(1f, 0.11f, 0.8f), new Vector3(0, 0.055f, 0), asphaltMat, root.transform, "Asphalt_Main");
        
        // Asphalt North (Top-right notch avoided, covers x = -0.5 to 0.4, z = 0.4 to 0.5)
        CreateBox(new Vector3(0.9f, 0.11f, 0.1f), new Vector3(-0.05f, 0.055f, 0.45f), asphaltMat, root.transform, "Asphalt_North");

        Vector2[] arcPolyRight = new Vector2[] {
            new Vector2(0.4f, 0.4f),
            new Vector2(0.5f, 0.4f),
            new Vector2(0.5f + 0.1f * Mathf.Cos(247.5f * Mathf.Deg2Rad), 0.5f + 0.1f * Mathf.Sin(247.5f * Mathf.Deg2Rad)),
            new Vector2(0.5f + 0.1f * Mathf.Cos(225.0f * Mathf.Deg2Rad), 0.5f + 0.1f * Mathf.Sin(225.0f * Mathf.Deg2Rad)),
            new Vector2(0.5f + 0.1f * Mathf.Cos(202.5f * Mathf.Deg2Rad), 0.5f + 0.1f * Mathf.Sin(202.5f * Mathf.Deg2Rad)),
            new Vector2(0.4f, 0.5f)
        };
        CreateConvexExtrusion(arcPolyRight, 0.11f, 0.055f, asphaltMat, root.transform, "Asphalt_CurveFiller");

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Features/LevelSystem/Art/Tile_HalfT_Right.prefab");
        DestroyImmediate(root);
    }

    private static ProBuilderMesh CreateConvexExtrusion(Vector2[] points, float height, float yOffset, Material mat, Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0, yOffset, 0);
        
        ProBuilderMesh pb = go.AddComponent<ProBuilderMesh>();
        
        int n = points.Length;
        Vector3[] vertices = new Vector3[n * 2];
        for (int i = 0; i < n; i++)
        {
            vertices[i] = new Vector3(points[i].x, -height/2, points[i].y);
            vertices[n + i] = new Vector3(points[i].x, height/2, points[i].y);
        }
        
        List<Face> faces = new List<Face>();
        
        List<int> bottomTris = new List<int>();
        for (int i = 1; i < n - 1; i++) { bottomTris.Add(0); bottomTris.Add(i+1); bottomTris.Add(i); }
        faces.Add(new Face(bottomTris.ToArray()));
        
        List<int> topTris = new List<int>();
        for (int i = 1; i < n - 1; i++) { topTris.Add(n); topTris.Add(n+i); topTris.Add(n+i+1); }
        faces.Add(new Face(topTris.ToArray()));
        
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            faces.Add(new Face(new int[] { i, n+i, n+next, i, n+next, next }));
        }
        
        pb.RebuildWithPositionsAndFaces(vertices, faces.ToArray());
        pb.GetComponent<Renderer>().sharedMaterial = mat;
        pb.Refresh();
        pb.ToMesh();
        
        return pb;
    }
}
