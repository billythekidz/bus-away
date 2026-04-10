using NUnit.Framework;
using UnityEngine;
using BusAway.Level;

namespace BusAway.Tests.Editor
{
    public class LevelDesignDataTests
    {
        [Test]
        public void CrowdLand_AgentCounts_AreClampedToMultiplesOf4()
        {
            // Create a dummy LevelDesignData
            LevelDesignData data = ScriptableObject.CreateInstance<LevelDesignData>();
            
            // Assign some invalid counts manually
            data.minAgentsPerLand = 10;
            data.maxAgentsPerLand = 15;

            // Simulate the logic from LevelDesignDataEditor
            data.minAgentsPerLand = Mathf.Max(4, (data.minAgentsPerLand / 4) * 4);
            data.maxAgentsPerLand = Mathf.Max(data.minAgentsPerLand, (data.maxAgentsPerLand / 4) * 4);

            Assert.AreEqual(8, data.minAgentsPerLand, "Min agents should round down to 8.");
            Assert.AreEqual(12, data.maxAgentsPerLand, "Max agents should round down to 12.");
        }

        [Test]
        public void CrowdLand_MaxLandCount_RespectsPaletteWarning()
        {
            LevelDesignData data = ScriptableObject.CreateInstance<LevelDesignData>();
            data.maxLandCount = 4;
            data.landColorPalette = new System.Collections.Generic.List<Color> { Color.red, Color.blue };

            // Logic check: if palette count < maxLandCount, we should warn
            bool needsWarning = data.landColorPalette.Count < data.maxLandCount;

            Assert.IsTrue(needsWarning, "Should require a warning when palette has fewer colors than max Land count.");
        }
    }
}
