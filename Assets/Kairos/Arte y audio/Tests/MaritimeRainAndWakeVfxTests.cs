using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Kairos.Pruebas;

namespace Kairos.Pruebas.Tests
{
    [TestFixture]
    public class MaritimeRainAndWakeVfxTests
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
        public void RainController_InitializesEmittersAndAudioCorrectly()
        {
            GameObject go = new GameObject("Test_RainController");
            spawnedObjects.Add(go);

            var rain = go.AddComponent<MaritimeRainParticlesController>();
            rain.InitializeRainVfx();

            Assert.IsTrue(rain.IsInitialized, "El controlador de lluvia marítima debe quedar inicializado.");
            Assert.IsNotNull(rain.rainAudioSource, "El componente AudioSource para lluvia debe estar configurado.");
            Assert.IsNotNull(rain.rainAudioSource.clip, "El clip de audio procedural de lluvia debe haberse generado.");
            Assert.AreEqual("Procedural_Maritime_Rain", rain.rainAudioSource.clip.name);
        }

        [Test]
        public void RainController_SetRainIntensity_AdjustsAudioAndParameters()
        {
            GameObject go = new GameObject("Test_RainIntensity");
            spawnedObjects.Add(go);

            var rain = go.AddComponent<MaritimeRainParticlesController>();
            rain.InitializeRainVfx();

            rain.SetRainIntensity(0.35f);
            Assert.AreEqual(0.35f, rain.rainIntensity, 0.001f);
            Assert.AreEqual(rain.rainAudioVolume * 0.35f, rain.rainAudioSource.volume, 0.01f);

            rain.SetWindAngle(35f);
            Assert.AreEqual(35f, rain.windAngleDegrees, 0.01f);
        }

        [Test]
        public void ProceduralRainAudio_GeneratesValidSamples()
        {
            AudioClip clip = MaritimeRainParticlesController.GenerateProceduralRainNoise(0.5f);
            Assert.IsNotNull(clip, "El clip sintetizado no debe ser nulo.");
            Assert.Greater(clip.samples, 0, "El clip debe contener muestras PCM.");
            Assert.AreEqual(44100, clip.frequency, "La frecuencia de muestreo debe ser 44.1 kHz.");

            float[] samples = new float[100];
            clip.GetData(samples, 0);
            bool hasNonZero = false;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > 0.0001f)
                {
                    hasNonZero = true;
                    break;
                }
            }
            Assert.IsTrue(hasNonZero, "Las muestras sintetizadas de lluvia deben contener datos de onda.");
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void BoatWakeShader_CanBeFoundOrMaterialCreated()
        {
            Shader wakeShader = Shader.Find("Kairos/Pruebas/BoatWakeFoam");
            if (wakeShader != null)
            {
                Material mat = new Material(wakeShader);
                Assert.IsNotNull(mat);
                Assert.IsTrue(mat.HasProperty("_FoamColor"));
                Assert.IsTrue(mat.HasProperty("_Speed"));
                Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void RainController_AsignaMaterialUrpSinSombrasEnRenderers()
        {
            GameObject go = new GameObject("Test_RainMaterials");
            spawnedObjects.Add(go);

            var rain = go.AddComponent<MaritimeRainParticlesController>();
            rain.InitializeRainVfx();

            Assert.IsNotNull(rain.RainEmitter);
            var rainRenderer = rain.RainEmitter.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(rainRenderer.sharedMaterial, "El emisor de lluvia debe tener un material URP asignado.");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, rainRenderer.shadowCastingMode);

            Assert.IsNotNull(rain.SplashEmitter);
            var splashRenderer = rain.SplashEmitter.GetComponent<ParticleSystemRenderer>();
            Assert.IsNotNull(splashRenderer.sharedMaterial, "El emisor de salpicaduras debe tener un material URP asignado.");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, splashRenderer.shadowCastingMode);
        }

        [Test]
        public void RainController_AdaptaIntensidadYVientoPorSector()
        {
            GameObject go = new GameObject("Test_RainSectors");
            spawnedObjects.Add(go);

            var rain = go.AddComponent<MaritimeRainParticlesController>();
            rain.InitializeRainVfx();

            rain.ApplySectorWeather(1, 4f);
            float guineaIntensity = rain.rainIntensity;
            float guineaWind = rain.windAngleDegrees;

            rain.ApplySectorWeather(2, 6f);
            Assert.Greater(rain.rainIntensity, guineaIntensity, "La tormenta monzónica del Sector 3 debe tener mayor intensidad.");
            Assert.Greater(rain.windAngleDegrees, guineaWind, "La tormenta del Sector 3 debe inclinar más el viento.");
        }
    }
}
