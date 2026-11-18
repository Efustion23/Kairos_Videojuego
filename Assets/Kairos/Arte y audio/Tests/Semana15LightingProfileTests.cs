using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Kairos.Pruebas;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kairos.Pruebas.Tests
{
    [TestFixture]
    public class Semana15LightingProfileTests
    {
        private List<GameObject> spawnedObjects;
        private bool originalFog;
        private Color originalFogColor;
        private float originalFogDensity;

        [SetUp]
        public void Setup()
        {
            spawnedObjects = new List<GameObject>();
            originalFog = RenderSettings.fog;
            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;
        }

        [TearDown]
        public void Teardown()
        {
            RenderSettings.fog = originalFog;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;

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
        public void CrearAtmosferaMaritima_InicializaLucesYReflectoresCorrectamente()
        {
            var root = MaritimeFogAtmosphereController.CreateMaritimeAtmosphere(null);
            spawnedObjects.Add(root);

            var ctrl = root.GetComponent<MaritimeFogAtmosphereController>();
            Assert.IsNotNull(ctrl, "Debe contener el controlador MaritimeFogAtmosphereController.");
            Assert.AreEqual(2, ctrl.deckFloodlights.Count, "Debe instanciar los reflectores de proa y popa.");
            Assert.IsNotNull(ctrl.mastBeaconLight, "Debe contener la baliza de mástil.");
            Assert.IsNotNull(ctrl.postProcessVolume, "Debe vincular un Volume de post-procesado.");
            Assert.IsTrue(ctrl.postProcessVolume.isGlobal, "El Volume debe ser global.");
        }

        [Test]
        public void ApplyAtmosphericSettings_ConfiguraNieblaMaritimaCorrectamente()
        {
            var go = new GameObject("FogTest");
            spawnedObjects.Add(go);
            var ctrl = go.AddComponent<MaritimeFogAtmosphereController>();
            ctrl.enableMarineFog = true;
            ctrl.marineFogColor = new Color(0.04f, 0.08f, 0.12f, 1f);
            ctrl.fogDensity = 0.035f;

            ctrl.ApplyAtmosphericSettings();

            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(0.035f, RenderSettings.fogDensity, 0.001f);
            Assert.AreEqual(0.04f, RenderSettings.fogColor.r, 0.01f);
        }

#if UNITY_EDITOR
        [Test]
        public void PostProcessProfile_CargaYValidaComponentes()
        {
            string profilePath = "Assets/Kairos/Arte y audio/Profiles/NightMaritimePostProcessProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            Assert.IsNotNull(profile, "El asset NightMaritimePostProcessProfile debe existir y ser legible.");
            Assert.IsTrue(profile.components.Count >= 3, "El perfil debe contener múltiples overrides de post-procesado.");
        }
#endif
    }
}
