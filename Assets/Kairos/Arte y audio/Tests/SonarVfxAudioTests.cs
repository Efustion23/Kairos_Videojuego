using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kairos.Pruebas.Tests
{
    public class SonarVfxAudioTests
    {
        [Test]
        public void PingAcusticoPruebaGeneraAudioValido()
        {
            var clip = SubmarineAudioSuite.PingAcoustic;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 1.0f);
            Assert.AreEqual(44100, clip.frequency);

            var samples = new float[1024];
            clip.GetData(samples, 0);
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
                if (Mathf.Abs(samples[i]) > peak) peak = Mathf.Abs(samples[i]);

            Assert.Greater(peak, 0.1f, "El ping acústico debe contener amplitud de señal suficiente.");
        }

        [Test]
        public void EcoMetalicoPruebaGeneraResonancia()
        {
            var clip = SubmarineAudioSuite.EcoMetallic;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 1.5f);
            Assert.AreEqual(44100, clip.frequency);

            var samples = new float[2048];
            clip.GetData(samples, 0);
            float energy = 0f;
            for (int i = 0; i < samples.Length; i++) energy += samples[i] * samples[i];
            Assert.Greater(energy, 1.0f, "El eco metálico debe poseer energía acústica audible.");
        }

        [Test]
        public void TurbulenciaSubmarinaGeneraAudioContinuo()
        {
            var clip = SubmarineAudioSuite.SubmarineTurbulence;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 2.5f);
            Assert.AreEqual(44100, clip.frequency);
        }

        [Test]
        public void CrujidosMetalicosSubmarinosGeneranTension()
        {
            var clip = SubmarineAudioSuite.UnderwaterCreak;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 2.0f);
            Assert.AreEqual(44100, clip.frequency);
        }

        [Test]
        public void ReverberacionSubmarinaGeneraColaAcustica()
        {
            var clip = SubmarineAudioSuite.SonarReverb;
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 1.8f);
            Assert.AreEqual(44100, clip.frequency);
        }

        [Test]
        public void ShaderScreenSweepInstanciaMaterialValido()
        {
            var mat = MatsSonarPrueba.CreateScreenSweep(new Color(0.247f, 0.878f, 0.816f), progress: 0.5f, origin: new Vector2(0.5f, 0.5f));
            Assert.IsNotNull(mat);
            if (mat.HasProperty("_Progress"))
                Assert.AreEqual(0.5f, mat.GetFloat("_Progress"), 0.001f);
        }

        [Test]
        public void ShaderRadialSweepInstanciaMaterialValido()
        {
            var mat = MatsSonarPrueba.CreateRadialSweep(new Color(0.247f, 0.878f, 0.816f), radius: 25f, width: 14f);
            Assert.IsNotNull(mat);
            if (mat.HasProperty("_Radius"))
                Assert.AreEqual(25f, mat.GetFloat("_Radius"), 0.001f);
        }

#if UNITY_EDITOR
        [Test]
        public void PerfilTermicoEnCambiosDePruebaExisteYTieneContrasteCobaltoAmbar()
        {
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/cambios de prueba/Profiles/ThermalProfile.asset");
            Assert.IsNotNull(profile, "El asset ThermalProfile.asset debe existir en la carpeta de prueba.");

            Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var ca));
            Assert.Greater(ca.contrast.value, 40f);

            Assert.IsTrue(profile.TryGet<Bloom>(out var bloom));
            Assert.Greater(bloom.intensity.value, 1.5f);

            Assert.IsTrue(profile.TryGet<ShadowsMidtonesHighlights>(out var smh));
            Assert.AreEqual(0.12f, smh.shadows.value.x, 0.05f); // Sombras en azul cobalto
        }
#endif
    }
}
