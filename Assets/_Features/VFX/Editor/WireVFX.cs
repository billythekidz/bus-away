using UnityEngine;
using UnityEditor;

public class WireVFX : EditorWindow
{
    [MenuItem("Tools/Wire VFX To Bus Controller")]
    public static void Wire()
    {
        string prefabPath = "Assets/_Features/LevelSystem/Prefabs/BusV2.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        BusMovement.BusController controller = prefabRoot.GetComponent<BusMovement.BusController>();
        if (controller != null)
        {
            // Find skidmarks under BL and BR
            Transform bl = prefabRoot.transform.Find("Wheel_BL/SkidMarkTrail");
            Transform br = prefabRoot.transform.Find("Wheel_BR/SkidMarkTrail");
            Transform fl = prefabRoot.transform.Find("Wheel_FL/SkidMarkTrail");
            Transform fr = prefabRoot.transform.Find("Wheel_FR/SkidMarkTrail");

            var skidList = new System.Collections.Generic.List<TrailRenderer>();
            if (bl != null) skidList.Add(bl.GetComponent<TrailRenderer>());
            if (br != null) skidList.Add(br.GetComponent<TrailRenderer>());
            if (fl != null) skidList.Add(fl.GetComponent<TrailRenderer>());
            if (fr != null) skidList.Add(fr.GetComponent<TrailRenderer>());

            controller.skidMarks = skidList.ToArray();

            Transform spark = prefabRoot.transform.Find("SparkBlingVFX");
            if (spark != null)
            {
                controller.sparkBlingVFX = spark.GetComponent<ParticleSystem>();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log("Successfully wired VFX to BusController!");
        }
        else
        {
            Debug.LogError("BusController not found on BusV2 prefab.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
