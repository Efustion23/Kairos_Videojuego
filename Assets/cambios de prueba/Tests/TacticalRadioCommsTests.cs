using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Kairos.Pruebas;

namespace Kairos.Pruebas.Tests
{
    [TestFixture]
    public class TacticalRadioCommsTests
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
        public void RadioComms_InitializesFiltersAndSourcesCorrectly()
        {
            GameObject go = new GameObject("Test_RadioComms");
            spawnedObjects.Add(go);

            var radio = go.AddComponent<TacticalRadioCommsAudio>();
            radio.InitializeRadioComms();

            Assert.IsTrue(radio.IsInitialized, "El sistema de radio táctica debe quedar inicializado.");
            Assert.IsNotNull(radio.voiceAudioSource, "El AudioSource de voz debe estar asignado.");
            Assert.IsNotNull(radio.fxAudioSource, "El AudioSource secundario de FX de squelch debe estar asignado.");

            var highPass = go.GetComponent<AudioHighPassFilter>();
            var lowPass = go.GetComponent<AudioLowPassFilter>();
            Assert.IsNotNull(highPass, "Debe crearse el filtro pasa-altos para cortar frecuencias graves.");
            Assert.IsNotNull(lowPass, "Debe crearse el filtro pasa-bajos para limitar el canal a frecuencias de voz.");
            Assert.AreEqual(350f, highPass.cutoffFrequency, 1.0f);
            Assert.AreEqual(3200f, lowPass.cutoffFrequency, 1.0f);
        }

        [Test]
        public void ProceduralRogerBeep_GeneratesDualToneCorrectly()
        {
            AudioClip beep = TacticalRadioCommsAudio.GenerateRogerBeepClip();
            Assert.IsNotNull(beep, "El clip Roger Beep sintetizado no debe ser nulo.");
            Assert.Greater(beep.samples, 0, "El Roger Beep debe contener muestras de audio.");
            Assert.AreEqual("Radio_Roger_Beep", beep.name);

            float[] samples = new float[beep.samples];
            beep.GetData(samples, 0);

            // Verificar que no sea silencio
            float maxAmp = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > maxAmp) maxAmp = Mathf.Abs(samples[i]);
            }
            Assert.Greater(maxAmp, 0.1f, "El tono Roger Beep debe tener amplitud audible.");
            Object.DestroyImmediate(beep);
        }

        [Test]
        public void ProceduralPttBurst_GeneratesDecayingNoise()
        {
            AudioClip ptt = TacticalRadioCommsAudio.GeneratePttBurstClip(0.08f);
            Assert.IsNotNull(ptt);
            Assert.AreEqual("Radio_PTT_Open", ptt.name);

            float[] samples = new float[ptt.samples];
            ptt.GetData(samples, 0);

            // Verificar que las primeras muestras tengan mayor amplitud que las últimas (decaimiento)
            float startEnergy = 0f;
            float endEnergy = 0f;
            int half = samples.Length / 2;

            for (int i = 0; i < half; i++) startEnergy += Mathf.Abs(samples[i]);
            for (int i = half; i < samples.Length; i++) endEnergy += Mathf.Abs(samples[i]);

            Assert.Greater(startEnergy, endEnergy, "La ráfaga de squelch PTT debe decaer en energía.");
            Object.DestroyImmediate(ptt);
        }
    }
}
