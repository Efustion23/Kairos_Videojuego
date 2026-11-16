using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Kairos.Pruebas;

namespace Kairos.Pruebas.Tests
{
    [TestFixture]
    public class ShowcaseIntegrationTests
    {
        private List<GameObject> spawnedObjects;

        [SetUp]
        public void Setup()
        {
            spawnedObjects = new List<GameObject>();
        }

        [TearDown]
        public void Teardown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        [Test]
        public void Showcase_InitializesCorrectlyWithDefaultMode()
        {
            GameObject go = new GameObject("Test_Showcase");
            spawnedObjects.Add(go);

            var showcase = go.AddComponent<ChristopherArtAudioShowcase>();
            showcase.InitializeShowcase();

            Assert.IsTrue(showcase.IsInitialized, "El showcase debe inicializarse correctamente.");
            Assert.AreEqual(ChristopherArtAudioShowcase.ShowcaseMode.SonarSweep, showcase.activeMode);
            Assert.IsTrue(showcase.showTelemetryHud);
        }

        [Test]
        public void Showcase_SwitchMode_UpdatesActiveState()
        {
            GameObject go = new GameObject("Test_ShowcaseModeSwitch");
            spawnedObjects.Add(go);

            var showcase = go.AddComponent<ChristopherArtAudioShowcase>();
            showcase.InitializeShowcase();

            showcase.SwitchMode(ChristopherArtAudioShowcase.ShowcaseMode.ThermalVisor);
            Assert.AreEqual(ChristopherArtAudioShowcase.ShowcaseMode.ThermalVisor, showcase.activeMode);

            showcase.SwitchMode(ChristopherArtAudioShowcase.ShowcaseMode.MaritimeRainAndFog);
            Assert.AreEqual(ChristopherArtAudioShowcase.ShowcaseMode.MaritimeRainAndFog, showcase.activeMode);

            showcase.SwitchMode(ChristopherArtAudioShowcase.ShowcaseMode.TacticalRadio);
            Assert.AreEqual(ChristopherArtAudioShowcase.ShowcaseMode.TacticalRadio, showcase.activeMode);
        }
    }
}
