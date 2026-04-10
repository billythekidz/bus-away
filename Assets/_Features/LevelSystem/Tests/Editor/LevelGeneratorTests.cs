using NUnit.Framework;
using UnityEngine;
using BusAway.Gameplay;
using BusAway.Level;
using BusAway.CrowdSystem;
using System.Collections.Generic;

namespace BusAway.Tests.Editor
{
    public class LevelGeneratorTests
    {
        [Test]
        public void BuildCrowdLands_GeneratesLandsWithinBounds()
        {
            var genGo = new GameObject("LevelGen");
            var gen = genGo.AddComponent<LevelGenerator>();

            var crowdGo = new GameObject("CrowdManager");
            var crowdManager = crowdGo.AddComponent<CrowdManager>();
            crowdManager.rowsPerLand = 5;

            var data = ScriptableObject.CreateInstance<LevelDesignData>();
            data.landCount = 3;
            data.landColorPalette = new List<Color> { Color.red, Color.green, Color.blue, Color.yellow, Color.white };
            gen.activeLevelData = data;

            // Call BuildLevel to trigger BuildCrowdLands
            gen.BuildLevel();

            int landsCount = data.resolvedLands.Count;
            Assert.AreEqual(3, landsCount, "Land count should exactly match LevelDesignData.landCount.");

            foreach (var land in data.resolvedLands)
            {
                Assert.AreEqual(20, land.agentCount, "Agent count should be exactly rowsPerLand * 4.");
            }

            Object.DestroyImmediate(genGo);
            Object.DestroyImmediate(crowdGo);
            Object.DestroyImmediate(data);
        }
    }
}
