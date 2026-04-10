using System.Collections.Generic;
using System.Linq;
using BusAway.Level;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;


public class GenerateProBuilderAddons : MonoBehaviour
{
    // [MenuItem("Bus Away/Generate/All Tiles")]
    // public static void GenerateAll()
    // {
    //     GenerateCrosswalk();
    //     GenerateBusBay();
    //     GenerateHalfTLeft();
    //     GenerateHalfTRight();
    //     Debug.Log("Generated All ProBuilder Cell Types.");
    // }

    private static Material GetMaterial(string hex, string name)
    {
        string matFolder = "Assets/_Features/LevelSystem/Art/Materials";
        if (!AssetDatabase.IsValidFolder("Assets/_Features/LevelSystem/Art"))
            AssetDatabase.CreateFolder("Assets/_Features/LevelSystem", "Art");
        if (!AssetDatabase.IsValidFolder(matFolder))
            AssetDatabase.CreateFolder("Assets/_Features/LevelSystem/Art", "Materials");

        string matPath = $"{matFolder}/{name}.mat";
        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existingMat != null)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color col)) existingMat.color = col;
            return existingMat;
        }

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (ColorUtility.TryParseHtmlString(hex, out Color col2)) mat.color = col2;
        mat.name = name;
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }

    private static GameObject CreateBox(Vector3 size, Vector3 pos, Material mat, Transform parent, string name)
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

        for (int i = 0; i < vertices.Length; i++) vertices[i] = Vector3.Scale(vertices[i], size);

        pb.RebuildWithPositionsAndFaces(vertices, faces);
        pb.GetComponent<Renderer>().sharedMaterial = mat;
        pb.Refresh();
        pb.ToMesh();

        // Important: Strip ProBuilder components so it becomes a standard prefab with normal meshes
        Mesh m = Instantiate(pb.GetComponent<MeshFilter>().sharedMesh);
        DestroyImmediate(pb); // Remove ProBuilder scripts
        go.GetComponent<MeshFilter>().sharedMesh = m;


        if (go.GetComponent<MeshRenderer>() == null)
        {
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        return go;
    }

    private static void SavePrefabWithMeshes(GameObject root, string prefabPath)
    {
        string meshDir = "Assets/_Features/LevelSystem/Art/TilesMesh";
        if (!AssetDatabase.IsValidFolder("Assets/_Features/LevelSystem/Art"))
            AssetDatabase.CreateFolder("Assets/_Features/LevelSystem", "Art");
        if (!AssetDatabase.IsValidFolder(meshDir))
            AssetDatabase.CreateFolder("Assets/_Features/LevelSystem/Art", "TilesMesh");

        int meshIndex = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh != null)
            {
                mf.sharedMesh.name = root.name + "_" + mf.gameObject.name + "_" + meshIndex;
                string exactPath = meshDir + "/" + mf.sharedMesh.name + ".asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(exactPath);
                
                if (existing != null)
                {
                    existing.Clear();
                    EditorUtility.CopySerialized(mf.sharedMesh, existing);
                    mf.sharedMesh = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(mf.sharedMesh, exactPath);
                }
                meshIndex++;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Bus Away/Generate/Crosswalk")]
    public static void GenerateCrosswalk()
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

        SavePrefabWithMeshes(root, "Assets/_Features/LevelSystem/Prefabs/Tile_Crosswalk_Real.prefab");
    }

    [MenuItem("Bus Away/Generate/Bus Stop Bay")]
    public static void GenerateBusBay()
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

        SavePrefabWithMeshes(root, "Assets/_Features/LevelSystem/Prefabs/Tile_BusStopBay_Real.prefab");
    }
    // ──────────────────────────────────────────────────────────────
    //  HalfT generation — polygon-based flat planes, 1×1 square tiles
    //  Matches T_Junction_Real style with larger arc radius (0.25)
    // ──────────────────────────────────────────────────────────────

    private static List<Vector3> ArcPoints(float cx, float cz, float radius, float startDeg, float endDeg, float y, int segments = 8)
    {
        var pts = new List<Vector3>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angleRad = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            pts.Add(new Vector3(cx + Mathf.Cos(angleRad) * radius, y, cz + Mathf.Sin(angleRad) * radius));
        }
        return pts;
    }

    private static Mesh FlatPolygonMesh(List<Vector3> poly, float y)
    {
        int n = poly.Count;
        var verts = new Vector3[n];
        var uvs   = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            verts[i] = new Vector3(poly[i].x, y, poly[i].z);
            uvs[i]   = new Vector2(poly[i].x + 0.5f, poly[i].z + 0.5f);
        }

        // Ear-clipping triangulation for potentially concave polygon (XZ plane)
        // Unity uses CCW for backface culling, but points must be CW to face UP in a left-handed coordinate system.
        // The HalfTPolygon provides points in CW order.
        var indices = new List<int>(Enumerable.Range(0, n));
        var tris    = new List<int>();

        int safety = n * n + 10;
        while (indices.Count > 2 && safety-- > 0)
        {
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int prev = (i - 1 + indices.Count) % indices.Count;
                int next = (i + 1)                 % indices.Count;
                int ip = indices[prev], ic = indices[i], in_ = indices[next];
                Vector2 a = new Vector2(verts[ip].x, verts[ip].z);
                Vector2 b = new Vector2(verts[ic].x, verts[ic].z);
                Vector2 c = new Vector2(verts[in_].x, verts[in_].z);

                // For CW polygon, an ear forms a RIGHT turn, so Cross2D must be < 0
                if (Cross2D(a, b, c) >= 0f) continue;

                // No other vertex inside this triangle
                bool hasInside = false;
                for (int j = 0; j < indices.Count; j++)
                {
                    if (j == prev || j == i || j == next) continue;
                    Vector2 p = new Vector2(verts[indices[j]].x, verts[indices[j]].z);
                    if (PointInTriangle2D(p, a, b, c)) { hasInside = true; break; }
                }
                if (hasInside) continue;

                tris.Add(ip); tris.Add(ic); tris.Add(in_);
                indices.RemoveAt(i);
                earFound = true;
                break;
            }
            if (!earFound) break; // degenerate, abort
        }

        var m = new Mesh();
        m.vertices  = verts;
        m.triangles = tris.ToArray();
        m.uv        = uvs;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static float Cross2D(Vector2 a, Vector2 b, Vector2 c)
        => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    private static bool PointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross2D(p, a, b);
        float d2 = Cross2D(p, b, c);
        float d3 = Cross2D(p, c, a);
        bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
        bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNeg && hasPos);
    }



    /// <summary>
    /// Build the polygon outline for a HalfT tile.
    /// keepPositiveZ=true  -> HalfT_Left
    /// keepPositiveZ=false -> HalfT_Right
    /// leftEdge: X position for open left border (border=-0.4, road=-0.35)
    /// arcRadius: radius of the inner concave arc (larger = more pronounced T)
    /// outerRadius: radius of the outer rounded corner (0.1 matches T_Junction_Real)
    /// </summary>
    private static List<Vector3> HalfTPolygon(float y, bool keepPositiveZ,
        float leftEdge = -0.4f, float arcRadius = 0.25f, float outerRadius = 0.1f)
    {
        float right   = 0.5f;
        float arcCX   = right - arcRadius;   // concave arc centre X
        float outerCX = right - outerRadius; // outer corner arc centre X

        var poly = new List<Vector3>();

        if (keepPositiveZ)
        {
            // HalfT_Left: open South (Z=+0.5) and West (X=-0.5)
            // Go CW: NW -> NE notch -> concave arc -> SE outer arc -> SW
            poly.Add(new Vector3(-0.5f,    y,  0.5f));   
            poly.Add(new Vector3(leftEdge, y,  0.5f));   
            poly.Add(new Vector3(arcCX,    y,  0.5f));   
            // Inner arc sweeping inwards from facing North (90deg) to facing East (0deg)
            poly.AddRange(ArcPoints(arcCX, 0f, arcRadius, 90f, 0f, y, 8));
            // Outer arc SE corner: 0 to -90
            poly.Add(new Vector3(right,    y, -(0.5f - outerRadius)));
            poly.AddRange(ArcPoints(outerCX, -(0.5f - outerRadius), outerRadius, 0f, -90f, y, 6));
            poly.Add(new Vector3(leftEdge, y, -0.5f));   
            poly.Add(new Vector3(-0.5f,    y, -0.5f));   
        }
        else
        {
            // HalfT_Right: open North (Z=-0.5) and West (X=-0.5)
            // We build it CW so normals face UP: NW -> NE outer arc -> SE notch -> concave arc -> SW
            poly.Add(new Vector3(-0.5f,    y,  0.5f));   
            poly.Add(new Vector3(leftEdge, y,  0.5f));
            // Outer arc NE corner: 90 to 0
            poly.AddRange(ArcPoints(outerCX, (0.5f - outerRadius), outerRadius, 90f, 0f, y, 6)); 
            poly.Add(new Vector3(right,    y,  (0.5f - outerRadius)));
            // Inner arc: 0 to -90
            poly.AddRange(ArcPoints(arcCX, 0f, arcRadius, 0f, -90f, y, 8));
            poly.Add(new Vector3(arcCX,    y, -0.5f));
            poly.Add(new Vector3(leftEdge, y, -0.5f));
            poly.Add(new Vector3(-0.5f,    y, -0.5f));   
        }

        // Just in case, clean any floating point duplicates
        var cleanPoly = new List<Vector3>();
        foreach (var p in poly)
        {
            if (cleanPoly.Count == 0 || Vector3.Distance(cleanPoly[cleanPoly.Count - 1], p) > 0.001f)
            {
                cleanPoly.Add(p);
            }
        }
        
        // Also check if first matches last
        if (cleanPoly.Count > 0 && Vector3.Distance(cleanPoly[0], cleanPoly[cleanPoly.Count - 1]) < 0.001f)
            cleanPoly.RemoveAt(cleanPoly.Count - 1);

        return cleanPoly;
    }


    private static void CreateHalfT(string tileName, string prefabPath, bool keepPositiveZ)
    {
        var tJunction = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Features/LevelSystem/Prefabs/Tile_T_Junction_Real.prefab");
        if (tJunction == null) { Debug.LogError("Tile_T_Junction_Real.prefab not found!"); return; }

        Material borderMat = null, roadMat = null;
        foreach (var mf in tJunction.GetComponentsInChildren<MeshFilter>(true))
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (mf.name == "Border") borderMat = mr?.sharedMaterial;
            if (mf.name == "Road")   roadMat   = mr?.sharedMaterial;
        }
        if (borderMat == null || roadMat == null)
        {
            Debug.LogError("Could not find Border/Road materials on T_Junction_Real!"); return;
        }

        GameObject root = new GameObject(tileName);

        // Border layer — Y=0.02, leftEdge=-0.4, arcRadius=0.25
        var borderPoly = HalfTPolygon(0f, keepPositiveZ, leftEdge: -0.4f, arcRadius: 0.25f);
        Mesh borderMesh = FlatPolygonMesh(borderPoly, 0.02f);
        borderMesh.name = tileName + "_Border";
        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(root.transform, false);
        borderGO.AddComponent<MeshFilter>().sharedMesh = borderMesh;
        borderGO.AddComponent<MeshRenderer>().sharedMaterial = borderMat;

        // Road layer — Y=0.05, leftEdge=-0.35 (inset 0.05), arcRadius=0.20
        var roadPoly = HalfTPolygon(0f, keepPositiveZ, leftEdge: -0.35f, arcRadius: 0.20f);
        Mesh roadMesh = FlatPolygonMesh(roadPoly, 0.05f);
        roadMesh.name = tileName + "_Road";
        var roadGO = new GameObject("Road");
        roadGO.transform.SetParent(root.transform, false);
        roadGO.AddComponent<MeshFilter>().sharedMesh = roadMesh;
        roadGO.AddComponent<MeshRenderer>().sharedMaterial = roadMat;

        SavePrefabWithMeshes(root, prefabPath);
    }

    [MenuItem("Bus Away/Generate/Half T Left")]
    public static void GenerateHalfTLeft()
    {
        CreateHalfT("Tile_HalfT_Left",
            "Assets/_Features/LevelSystem/Prefabs/Tile_HalfT_Left.prefab",
            keepPositiveZ: true);
    }

    [MenuItem("Bus Away/Generate/Half T Right")]
    public static void GenerateHalfTRight()
    {
        CreateHalfT("Tile_HalfT_Right",
            "Assets/_Features/LevelSystem/Prefabs/Tile_HalfT_Right.prefab",
            keepPositiveZ: false);
    }






    private static GameObject CreateConvexExtrusion(Vector2[] points, float height, float yOffset, Material mat, Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(0, yOffset, 0);


        ProBuilderMesh pb = go.AddComponent<ProBuilderMesh>();


        int n = points.Length;
        Vector3[] vertices = new Vector3[n * 2];
        for (int i = 0; i < n; i++)
        {
            vertices[i] = new Vector3(points[i].x, -height / 2, points[i].y);
            vertices[n + i] = new Vector3(points[i].x, height / 2, points[i].y);
        }


        List<Face> faces = new List<Face>();


        List<int> bottomTris = new List<int>();
        for (int i = 1; i < n - 1; i++) { bottomTris.Add(0); bottomTris.Add(i + 1); bottomTris.Add(i); }
        faces.Add(new Face(bottomTris.ToArray()));


        List<int> topTris = new List<int>();
        for (int i = 1; i < n - 1; i++) { topTris.Add(n); topTris.Add(n + i); topTris.Add(n + i + 1); }
        faces.Add(new Face(topTris.ToArray()));


        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            faces.Add(new Face(new int[] { i, n + i, n + next, i, n + next, next }));
        }


        pb.RebuildWithPositionsAndFaces(vertices, faces.ToArray());
        pb.GetComponent<Renderer>().sharedMaterial = mat;
        pb.Refresh();
        pb.ToMesh();

        Mesh m = Instantiate(pb.GetComponent<MeshFilter>().sharedMesh);
        DestroyImmediate(pb);
        go.GetComponent<MeshFilter>().sharedMesh = m;


        if (go.GetComponent<MeshRenderer>() == null)
        {
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        return go;
    }
}
