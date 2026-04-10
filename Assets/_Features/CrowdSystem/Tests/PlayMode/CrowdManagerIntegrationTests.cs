using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BusAway.CrowdSystem;

namespace BusAway.Tests.PlayMode
{
    public class CrowdManagerIntegrationTests
    {
        private GameObject go;
        private CrowdManager manager;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("CrowdManager");
            manager = go.AddComponent<CrowdManager>();
            manager.maxAgents = 100;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator SpawnLand_SpawnsCorrectNumberOfAgentsInGrid()
        {
            yield return null; // Wait 1 frame to initialize

            Assert.AreEqual(0, manager.GetActiveCountForTesting(), "Should start with 0 agents.");

            // Spawn 12 agents (3 rows of 4)
            manager.SpawnLand(0, 12, Color.red);

            yield return null;

            Assert.AreEqual(12, manager.GetActiveCountForTesting(), "Exactly 12 active agents should exist.");
        }
    }
}
