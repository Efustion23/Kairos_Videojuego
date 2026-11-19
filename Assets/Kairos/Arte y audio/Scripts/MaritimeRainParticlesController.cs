using System;
using UnityEngine;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Controlador de efectos visuales y acústicos de lluvia torrencial marítima y salpicaduras en cubierta.
    /// Diseñado para la ambientación climática de tormenta nocturna en altamar (Christopher Veliz - Arte 3D y VFX).
    /// </summary>
    public class MaritimeRainParticlesController : MonoBehaviour
    {
        [Header("Parámetros de Lluvia Marina")]
        [Range(0f, 1f)] public float rainIntensity = 0.85f;
        [Range(0f, 60f)] public float windAngleDegrees = 22f;
        public float fallSpeed = 28f;
        public int maxRainDrops = 600;

        [Header("Salpicaduras en Cubierta Metálica")]
        public bool enableDeckSplashes = true;
        public Color rainColor = new Color(0.75f, 0.88f, 1.0f, 0.65f);
        public float splashLifetime = 0.12f;

        [Header("Audio Acústico de Tormenta")]
        public bool enableRainSound = true;
        [Range(0f, 1f)] public float rainAudioVolume = 0.6f;
        public AudioSource rainAudioSource;

        private ParticleSystem rainParticleSystem;
        private ParticleSystem splashParticleSystem;
        private AudioClip generatedRainClip;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            InitializeRainVfx();
        }

        public void InitializeRainVfx()
        {
            if (IsInitialized) return;

            SetupRainEmitters();
            SetupAudioLoop();

            IsInitialized = true;
        }

        private void SetupRainEmitters()
        {
            // Emisor principal de gotas
            GameObject rainObj = new GameObject("Maritime_Rain_Emitter");
            rainObj.transform.SetParent(transform, false);
            rainObj.transform.localPosition = new Vector3(0, 15f, 0);

            rainParticleSystem = rainObj.AddComponent<ParticleSystem>();
            var main = rainParticleSystem.main;
            main.maxParticles = maxRainDrops;
            main.startLifetime = 1.0f;
            main.startSpeed = fallSpeed;
            main.startSize = 0.04f;
            main.startColor = rainColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = rainParticleSystem.emission;
            emission.rateOverTime = maxRainDrops * rainIntensity;

            var shape = rainParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 0.5f, 30f);

            // Inclinación por ráfagas de viento oceánico
            rainObj.transform.localRotation = Quaternion.Euler(windAngleDegrees, 0, 0);

            // Emisor secundario de salpicaduras
            if (enableDeckSplashes)
            {
                GameObject splashObj = new GameObject("Maritime_Splash_Emitter");
                splashObj.transform.SetParent(transform, false);
                splashObj.transform.localPosition = Vector3.zero;

                splashParticleSystem = splashObj.AddComponent<ParticleSystem>();
                var sMain = splashParticleSystem.main;
                sMain.maxParticles = 200;
                sMain.startLifetime = splashLifetime;
                sMain.startSpeed = 1.8f;
                sMain.startSize = 0.08f;
                sMain.startColor = new Color(0.85f, 0.95f, 1f, 0.45f);

                var sEmission = splashParticleSystem.emission;
                sEmission.rateOverTime = 80f * rainIntensity;
            }
        }

        private void SetupAudioLoop()
        {
            if (!enableRainSound) return;

            generatedRainClip = GenerateProceduralRainNoise(2.0f);

            if (rainAudioSource == null)
            {
                var audioChild = new GameObject("Rain_Ambience_Audio");
                audioChild.SetActive(false);
                audioChild.transform.SetParent(transform, false);
                rainAudioSource = audioChild.AddComponent<AudioSource>();
                rainAudioSource.playOnAwake = false;
                rainAudioSource.clip = generatedRainClip;
                audioChild.SetActive(true);
            }
            else
            {
                rainAudioSource.playOnAwake = false;
                rainAudioSource.clip = generatedRainClip;
            }

            rainAudioSource.loop = true;
            rainAudioSource.volume = rainAudioVolume * rainIntensity;
            rainAudioSource.spatialBlend = 0.2f; // Audio envolvente semi-ambiental

            if (Application.isPlaying)
            {
                rainAudioSource.Play();
            }
        }

        public void SetRainIntensity(float intensity)
        {
            rainIntensity = Mathf.Clamp01(intensity);

            if (rainParticleSystem != null)
            {
                var em = rainParticleSystem.emission;
                em.rateOverTime = maxRainDrops * rainIntensity;
            }

            if (splashParticleSystem != null)
            {
                var sEm = splashParticleSystem.emission;
                sEm.rateOverTime = 80f * rainIntensity;
            }

            if (rainAudioSource != null)
            {
                rainAudioSource.volume = rainAudioVolume * rainIntensity;
            }
        }

        public void SetWindAngle(float angle)
        {
            windAngleDegrees = Mathf.Clamp(angle, 0f, 60f);
            if (rainParticleSystem != null)
            {
                rainParticleSystem.transform.localRotation = Quaternion.Euler(windAngleDegrees, 0, 0);
            }
        }

        /// <summary>
        /// Síntesis de ruido blanco filtrado simulando la caída densa de lluvia sobre metal y agua marina.
        /// </summary>
        public static AudioClip GenerateProceduralRainNoise(float durationSeconds)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * durationSeconds);
            float[] data = new float[totalSamples];

            System.Random rand = new System.Random(7721);
            float lastSample = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                // Ruido blanco atenuado con filtro paso bajo simple (brownian/pink noise blend)
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                lastSample = Mathf.Lerp(lastSample, white, 0.15f);

                // Modulación de ráfagas suaves de viento (0.8 Hz)
                float windMod = 0.7f + 0.3f * Mathf.Sin((float)i / sampleRate * Mathf.PI * 1.6f);
                data[i] = lastSample * windMod * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Procedural_Maritime_Rain", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && generatedRainClip != null)
            {
                Destroy(generatedRainClip);
            }
        }
    }
}
