using NUnit.Framework;
using UnityEngine;
using BusAway.Gameplay;
using BusAway.Level;
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

            var data = ScriptableObject.CreateInstance<LevelDesignData>();
            data.minLandCount = 2;
            data.maxLandCount = 4;
            data.minAgentsPerLand = 8;
            data.maxAgentsPerLand = 12;
            data.landColorPalette = new List<Color> { Color.red, Color.green, Color.blue, Color.yellow, Color.white };
            gen.activeLevelData = data;

            // Call BuildLevel to trigger BuildCrowdLands
            gen.BuildLevel();

            int landsCount = data.resolvedLands.Count;
            Assert.IsTrue(landsCount >= 2 && landsCount <= 4, $"Land count {landsCount} out of bounds.");

            foreach (var land in data.resolvedLands)
            {
                Assert.IsTrue(land.agentCount >= 8 && land.agentCount <= 12, $"Agent count {land.agentCount} out of bounds.");
                Assert.AreEqual(0, land.agentCount % 4, $"Agent count {land.agentCount} is not a multiple of 4.");
            }

            Object.DestroyImmediate(genGo);
            Object.DestroyImmediate(data);
        }
    }
}
