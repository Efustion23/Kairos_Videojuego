using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Kairos.Pruebas;

namespace Kairos.Pruebas.Tests
{
    [TestFixture]
    public class Semana14AdaptiveMusicTests
    {
        private List<GameObject> spawnedObjects;
        private List<AudioClip> generatedClips;

        [SetUp]
        public void Setup()
        {
            spawnedObjects = new List<GameObject>();
            generatedClips = new List<AudioClip>();
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

            for (int i = 0; i < generatedClips.Count; i++)
            {
                if (generatedClips[i] != null)
                {
                    Object.DestroyImmediate(generatedClips[i]);
                }
            }
            generatedClips.Clear();
        }

        [Test]
        public void CrearSistemaMusica_InicializaFuentesYStemsCorrectamente()
        {
            var musicSystemGo = AdaptiveTensionMusic.CreateAdaptiveMusicSystem(null);
            spawnedObjects.Add(musicSystemGo);

            var music = musicSystemGo.GetComponent<AdaptiveTensionMusic>();
            Assert.IsNotNull(music, "El componente AdaptiveTensionMusic debe estar presente.");

            Assert.IsNotNull(music.calmSource, "La fuente de calma debe estar asignada.");
            Assert.IsNotNull(music.suspicionSource, "La fuente de sospecha debe estar asignada.");
            Assert.IsNotNull(music.alertSource, "La fuente de alerta debe estar asignada.");

            Assert.IsTrue(music.calmSource.loop, "Los stems deben estar configurados en bucle.");
            Assert.IsTrue(music.suspicionSource.loop);
            Assert.IsTrue(music.alertSource.loop);

            Assert.AreEqual(1.0f, music.calmWeight, 0.05f, "A nivel 0 de sospecha, la calma debe iniciar en 1.0.");
            Assert.AreEqual(0.0f, music.alertWeight, 0.05f, "A nivel 0 de sospecha, la alerta debe estar en 0.");
        }

        [Test]
        public void UpdateWeights_ZonaCalma_PonderaCalmaPredominante()
        {
            var go = new GameObject("MusicTest");
            spawnedObjects.Add(go);
            var music = go.AddComponent<AdaptiveTensionMusic>();

            music.UpdateWeights(15f);
            Assert.Greater(music.calmWeight, music.suspicionWeight, "En 15% de sospecha, la calma debe superar la sospecha.");
            Assert.AreEqual(0f, music.alertWeight, "En zona de calma, la alerta debe permanecer en cero.");
        }

        [Test]
        public void UpdateWeights_ZonaSospecha_PonderaSospechaMaxima()
        {
            var go = new GameObject("MusicTest");
            spawnedObjects.Add(go);
            var music = go.AddComponent<AdaptiveTensionMusic>();

            music.UpdateWeights(60f);
            Assert.Greater(music.suspicionWeight, music.calmWeight, "En 60% de sospecha, el stem de sospecha debe superar la calma.");
            Assert.Greater(music.alertWeight, 0f, "Debe comenzar la introducción sutil de percusión de alerta.");
        }

        [Test]
        public void UpdateWeights_ZonaAlerta_PonderaAlertaPredominante()
        {
            var go = new GameObject("MusicTest");
            spawnedObjects.Add(go);
            var music = go.AddComponent<AdaptiveTensionMusic>();

            music.UpdateWeights(90f);
            Assert.Greater(music.alertWeight, music.suspicionWeight, "En 90% de sospecha, la alerta debe predominar.");
            Assert.AreEqual(0f, music.calmWeight, "La pista de calma debe estar completamente silenciada.");
        }

        [Test]
        public void GenerateStemClip_ProduceAudioClipValido()
        {
            var clip = AdaptiveTensionMusic.GenerateStemClip("TestStem", 60f, 60f, 1.0f);
            generatedClips.Add(clip);

            Assert.IsNotNull(clip, "El clip generado no debe ser nulo.");
            Assert.AreEqual(44100, clip.frequency);
            Assert.Greater(clip.samples, 0);
        }

        [Test]
        public void EnsureStemsReady_RegeneraClipsNulosTrasGuardarEscena()
        {
            var musicSystemGo = AdaptiveTensionMusic.CreateAdaptiveMusicSystem(null);
            spawnedObjects.Add(musicSystemGo);

            var music = musicSystemGo.GetComponent<AdaptiveTensionMusic>();
            music.calmSource.clip = null;
            music.suspicionSource.clip = null;
            music.alertSource.clip = null;

            music.EnsureStemsReady();

            Assert.IsNotNull(music.calmSource.clip, "El stem de calma debe regenerarse si es nulo.");
            Assert.IsNotNull(music.suspicionSource.clip, "El stem de sospecha debe regenerarse si es nulo.");
            Assert.IsNotNull(music.alertSource.clip, "El stem de alerta debe regenerarse si es nulo.");
            Assert.IsTrue(music.calmSource.loop);
            Assert.IsTrue(music.suspicionSource.loop);
            Assert.IsTrue(music.alertSource.loop);
        }
    }
}
