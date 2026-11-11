using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Sistema de audio espacial 3D para interiores del pesquero (Semana 13 / S13-3):
    /// Pasos metálicos sobre chapa húmeda, zumbido de compresores industriales y tormenta en ductos.
    /// </summary>
    public static class InteriorSpatialAudio
    {
        const int SampleRate = 44100;
        static readonly System.Random Rng = new System.Random(99);
        static float Noise() => (float)Rng.NextDouble() * 2f - 1f;
        static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        static AudioClip MakeClip(string name, float seconds, Func<float, float, float> gen, bool seamlessLoop = false)
        {
            int n = (int)(seconds * SampleRate);
            var samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                samples[i] = Mathf.Clamp(gen(t, t / seconds), -1f, 1f);
            }
            if (seamlessLoop)
            {
                int crossfade = SampleRate / 4;
                for (int i = 0; i < crossfade; i++)
                {
                    float a = i / (float)crossfade;
                    samples[i] = samples[i] * a + samples[n - crossfade + i] * (1f - a);
                }
                Array.Resize(ref samples, n - crossfade);
                n -= crossfade;
            }
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip stepMetalWet;
        static AudioClip compressorHum;
        static AudioClip stormVentilation;

        /// <summary>
        /// Pasos metálicos sobre chapa naval y rejilla húmeda: impacto seco de bota (110 Hz)
        /// seguido de reverberación corta y salpicadura metálica.
        /// </summary>
        public static AudioClip StepMetalWet => stepMetalWet ??= MakeClip("Prueba_StepMetalWet", 0.22f, (t, u) =>
        {
            float impact = (Sin(110f, t) * 0.7f + Sin(220f, t) * 0.25f) * Mathf.Exp(-u * 14f);
            float wetSplash = Noise() * 0.35f * Mathf.Exp(-u * 8f) * (u > 0.04f ? 1f : 0.2f);
            float metalRing = Sin(1650f, t) * 0.18f * Mathf.Exp(-u * 12f);
            return (impact + wetSplash + metalRing) * 0.95f;
        });

        /// <summary>
        /// Zumbido continuo de compresor frigorífico industrial (50 Hz con armónicos a 100/150 Hz y vibración de amoníaco).
        /// </summary>
        public static AudioClip CompressorHumLoop => compressorHum ??= MakeClip("Prueba_CompressorHum", 3.0f, (t, u) =>
        {
            float f50 = Sin(50f, t) * 0.45f;
            float f100 = Sin(100f, t) * 0.28f;
            float f150 = Sin(150f, t) * 0.18f;
            float motorRumble = Sin(12.5f, t) * 0.12f;
            float hum = (f50 + f100 + f150) * (1f + motorRumble);
            float hiss = Noise() * 0.04f;
            return (hum + hiss) * 0.85f;
        }, true);

        /// <summary>
        /// Silbido y turbulencia de viento/tormenta colándose por rendijas de mamparos y ventilación.
        /// </summary>
        public static AudioClip StormVentilationLoop => stormVentilation ??= MakeClip("Prueba_StormVentilation", 4.0f, (t, u) =>
        {
            float gust = Sin(0.6f, t) * 0.3f + 0.7f;
            float whistle = Sin(580f + 120f * Sin(1.2f, t), t) * 0.22f;
            float windNoise = Noise() * 0.38f;
            return (whistle + windNoise) * gust * 0.75f;
        }, true);

        /// <summary>
        /// Emite un sonido espacial 3D en la posición indicada con atenuación y enrutamiento a AudioMixer.
        /// </summary>
        public static void PlaySpatialAt(AudioClip clip, Vector3 position, float volume = 1f, float maxDistance = 25f, AudioMixerGroup group = null)
        {
            if (clip == null) return;
            var go = new GameObject("Prueba_SpatialAudio3D");
            go.transform.position = position;

            var a = go.AddComponent<AudioSource>();
            a.clip = clip;
            a.volume = volume;
            a.spatialBlend = 1.0f; // 100% 3D
            a.minDistance = 1.5f;
            a.maxDistance = maxDistance;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.outputAudioMixerGroup = group ?? SubmarineAudioSuite.SubmarineGroup ?? SubmarineAudioSuite.AmbienceGroup;
            a.Play();

            UnityEngine.Object.Destroy(go, clip.length + 0.1f);
        }

        /// <summary>
        /// Crea un emisor continuo espacial 3D (para maquinaria, compresores o ventilación).
        /// </summary>
        public static AudioSource CreateSpatialLoop(GameObject host, AudioClip clip, float volume = 0.3f, float maxDistance = 20f)
        {
            if (host == null || clip == null) return null;
            var a = host.AddComponent<AudioSource>();
            a.clip = clip;
            a.loop = true;
            a.volume = volume;
            a.spatialBlend = 1.0f; // 100% 3D
            a.minDistance = 2.0f;
            a.maxDistance = maxDistance;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.outputAudioMixerGroup = SubmarineAudioSuite.AmbienceGroup;
            a.Play();
            return a;
        }
    }
}
