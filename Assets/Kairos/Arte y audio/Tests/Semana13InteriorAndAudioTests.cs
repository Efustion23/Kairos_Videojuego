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

        [Test]
        public void RigDeGuardiaOptimizaSombrasYRespondeAAturdimiento()
        {
            var guardGo = new GameObject("TestGuardStunRig");
            var rig = guardGo.AddComponent<GuardLanternPatrolRig>();

            Assert.AreEqual(LightShadows.None, rig.lanternLight.shadows, "Las linternas de patrulla no deben saturar el atlas de sombras URP.");

            rig.SetStunned(true);
            Assert.IsTrue(rig.isStunned);
            Assert.IsFalse(rig.lanternLight.enabled);
            Assert.AreEqual(0f, rig.lanternLight.intensity, 0.001f);

            rig.SetStunned(false);
            Assert.IsFalse(rig.isStunned);
            Assert.IsTrue(rig.lanternLight.enabled);
            Assert.Greater(rig.lanternLight.intensity, 2.0f);

            Object.DestroyImmediate(guardGo);
        }

        [Test]
        public void InteriorSpatialAudio_RestauraLoopCompresorSiClipEsNulo()
        {
            var host = new GameObject("Compresores_Test");
            var sysGo = new GameObject("InteriorAudio_Test");
            var spatial = sysGo.AddComponent<InteriorSpatialAudio>();

            var src = InteriorSpatialAudio.CreateSpatialLoop(host, InteriorSpatialAudio.CompressorHumLoop, 0.4f, 20f);
            Assert.IsNotNull(src);
            src.clip = null;

            int restored = spatial.EnsureRuntimeSpatialLoops(host);
            Assert.AreEqual(1, restored);
            Assert.AreEqual(InteriorSpatialAudio.CompressorHumLoop, src.clip);

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(sysGo);
        }
    }
}
