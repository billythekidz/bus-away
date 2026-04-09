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
}
