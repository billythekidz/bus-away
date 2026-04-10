using UnityEngine;
using UnityEditor;
using System.IO;

public class VFXCreator : EditorWindow
{
    [MenuItem("Tools/Generate Demo VFX")]
    public static void GenerateVFXs()
    {
        string folder = "Assets/_Features/VFX/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(current + "/" + parts[i]))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current += "/" + parts[i];
            }
        }

        CreateSkidMarkVFX(folder);
        CreateSparkBlingVFX(folder);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("VFXs created successfully in " + folder);
    }

    private static void CreateSkidMarkVFX(string folder)
    {
        GameObject go = new GameObject("SkidMarkTrail");
        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        
        trail.time = 1.5f;
        trail.minVertexDistance = 0.1f;
        
        // Width curve
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0.4f);
        widthCurve.AddKey(1f, 0.4f);
        trail.widthCurve = widthCurve;
        
        // Color gradient (Black, fading out)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 0.0f), new GradientColorKey(new Color(0.1f, 0.1f, 0.1f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trail.colorGradient = gradient;
        
        // Material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        AssetDatabase.CreateAsset(mat, $"{folder}/SkidMaterial.mat");
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sharedMaterial = mat;

        trail.emitting = true;
        trail.alignment = LineAlignment.TransformZ; // Align to ground
        
        PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/SkidMarkTrail.prefab");
        DestroyImmediate(go);
    }

    private static void CreateSparkBlingVFX(string folder)
    {
        GameObject go = new GameObject("SparkBlingVFX");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        // Main module
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor = new Color(1f, 0.9f, 0.2f, 1f); // Golden/Yellow
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.playOnAwake = false;

        // Emission module
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10, 15) });

        // Shape module
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        // Size over lifetime
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.8f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLife.color = grad;

        // Renderer material
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetColor("_Color", new Color(1f, 0.9f, 0.2f, 1f));
        
        try {
            AssetDatabase.CreateAsset(mat, $"{folder}/SparkBlingMaterial.mat");
        } catch { } // Ignore if exists
        renderer.sharedMaterial = mat;
        
        PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/SparkBlingVFX.prefab");
        DestroyImmediate(go);
    }
}
