using NUnit.Framework;
using UnityEngine;

namespace Kairos.Pruebas.Tests
{
    public class Semana13InteriorAndAudioTests
    {
        [Test]
        public void PasosMetalicosHumedaTienenImpactoYFrecuenciaCorrecta()
        {
            var clip = InteriorSpatialAudio.StepMetalWet;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0.15f);
            Assert.AreEqual(44100, clip.frequency);

            var samples = new float[1024];
            clip.GetData(samples, 0);
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
                if (Mathf.Abs(samples[i]) > peak) peak = Mathf.Abs(samples[i]);

            Assert.Greater(peak, 0.2f, "El paso metálico debe tener un transitorio de impacto marcado.");
        }

        [Test]
        public void CompresorFrigorificoGeneraZumbidoContinuo()
        {
            var clip = InteriorSpatialAudio.CompressorHumLoop;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 2.0f);
            Assert.AreEqual(44100, clip.frequency);
        }

        [Test]
        public void VentilacionTormentaGeneraTurbulenciaDeViento()
        {
            var clip = InteriorSpatialAudio.StormVentilationLoop;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 3.0f);
            Assert.AreEqual(44100, clip.frequency);
        }

        [Test]
        public void DecoracionPuenteGeneraConsolaYMesaDeCartas()
        {
            var go = new GameObject("TestBridgeDecor");
            var comp = go.AddComponent<BridgeAndCabinInterior>();
            var bridge = comp.DecorateBridge(go.transform);

            Assert.IsNotNull(bridge);
            Assert.IsNotNull(bridge.transform.Find("ConsolaTimoneria"));
            Assert.IsNotNull(bridge.transform.Find("MesaCartasNauticas"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void RigDeGuardiaConfiguraHazDeLinternaHalogena()
        {
            var guardGo = new GameObject("TestGuardRig");
            var rig = guardGo.AddComponent<GuardLanternPatrolRig>();

            Assert.IsNotNull(rig.lanternLight);
            Assert.AreEqual(LightType.Spot, rig.lanternLight.type);
            Assert.Greater(rig.lanternLight.intensity, 2.0f);
            Assert.Greater(rig.lanternLight.range, 10f);

            rig.SetAlert(true);
            Assert.Greater(rig.lanternLight.intensity, 3.5f);

            Object.DestroyImmediate(guardGo);
        }
    }
}
