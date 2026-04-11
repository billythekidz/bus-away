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
            data.busStopLength = 2;
            data.busesPerStop = 2;
            data.agentsPerBus = 32;
            data.landColorPalette = new List<Color> { Color.red, Color.green, Color.blue, Color.yellow, Color.white };
            gen.activeLevelData = data;

            // Call BuildLevel to trigger BuildCrowdLands
            gen.BuildLevel();

            int landsCount = data.resolvedLands.Count;
            Assert.AreEqual(3, landsCount, "Land count should exactly match LevelDesignData.landCount.");

            int expectedTotalPerColor = data.busStopLength * data.busesPerStop * data.agentsPerBus;
            int expectedTotalAll = expectedTotalPerColor * data.landColorPalette.Count;
            int actualTotal = 0;

            foreach (var group in data.resolvedLands)
            {
                foreach (var chunk in group.chunks)
                {
                    actualTotal += chunk.agentCount;
                }
            }
            Assert.AreEqual(expectedTotalAll, actualTotal, "Total agents across all lands must match the bus formulas.");

            Object.DestroyImmediate(genGo);
            Object.DestroyImmediate(crowdGo);
            Object.DestroyImmediate(data);
        }
    }
}
