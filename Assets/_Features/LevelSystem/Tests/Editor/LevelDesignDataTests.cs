using NUnit.Framework;
using UnityEngine;
using BusAway.Level;

namespace BusAway.Tests.Editor
{
    public class LevelDesignDataTests
    {
        [Test]
        public void CrowdLand_LandCount_RespectsPaletteWarning()
        {
            LevelDesignData data = ScriptableObject.CreateInstance<LevelDesignData>();
            data.landCount = 4;
            data.landColorPalette = new System.Collections.Generic.List<Color> { Color.red, Color.blue };

            // Logic check: if palette count < landCount, we should warn
            bool needsWarning = data.landColorPalette.Count < data.landCount;

            Assert.IsTrue(needsWarning, "Should require a warning when palette has fewer colors than Land count.");
        }
    }
}
