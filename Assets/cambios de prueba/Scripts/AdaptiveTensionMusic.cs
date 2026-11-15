using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Sistema de música adaptativa de tensión e infiltración nocturna (Semana 14 / S14-3).
    /// Controla la transición no lineal entre 3 capas sonoras (Calma, Sospecha, Alerta)
    /// en función del nivel de sospecha del barco pesquero (0 a 100%).
    /// </summary>
    public class AdaptiveTensionMusic : MonoBehaviour
    {
        [Header("Nivel de Tensión")]
        [Range(0f, 100f)]
        public float suspicionLevel = 0f;

        [Header("Capas Sonoras (Stems)")]
        public AudioSource calmSource;
        public AudioSource suspicionSource;
        public AudioSource alertSource;

        [Header("Ponderaciones Actuales")]
        [Range(0f, 1f)] public float calmWeight = 1f;
        [Range(0f, 1f)] public float suspicionWeight = 0f;
        [Range(0f, 1f)] public float alertWeight = 0f;

        [Header("Velocidad de Transición")]
        public float crossfadeSpeed = 3.5f;

        private void Update()
        {
            UpdateWeights(suspicionLevel);
            ApplyVolumes(Time.deltaTime * crossfadeSpeed);
        }

        /// <summary>
        /// Calcula la distribución de pesos de cada stem según el porcentaje de sospecha.
        /// </summary>
        public void UpdateWeights(float suspicion)
        {
            suspicion = Mathf.Clamp(suspicion, 0f, 100f);
            suspicionLevel = suspicion;

            if (suspicion < 35f)
            {
                // Zona 1: Calma e infiltración sigilosa (0% a 35%)
                float t = suspicion / 35f;
                calmWeight = Mathf.Lerp(1.0f, 0.45f, t);
                suspicionWeight = Mathf.Lerp(0.0f, 0.55f, t);
                alertWeight = 0f;
            }
            else if (suspicion < 75f)
            {
                // Zona 2: Sospecha e investigación de guardias (35% a 75%)
                float t = (suspicion - 35f) / 40f;
                calmWeight = Mathf.Lerp(0.45f, 0.0f, t);
                suspicionWeight = Mathf.Lerp(0.55f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
                alertWeight = Mathf.Lerp(0.0f, 0.70f, t);
            }
            else
            {
                // Zona 3: Alerta máxima y persecución (75% a 100%)
                float t = (suspicion - 75f) / 25f;
                calmWeight = 0f;
                suspicionWeight = Mathf.Lerp(0.70f, 0.20f, t);
                alertWeight = Mathf.Lerp(0.70f, 1.0f, t);
            }
        }

        /// <summary>
        /// Aplica suavemente los volúmenes a las fuentes de audio.
        /// </summary>
        public void ApplyVolumes(float lerpFactor)
        {
            if (calmSource != null)
            {
                calmSource.volume = Mathf.Lerp(calmSource.volume, calmWeight, lerpFactor);
            }
            if (suspicionSource != null)
            {
                suspicionSource.volume = Mathf.Lerp(suspicionSource.volume, suspicionWeight, lerpFactor);
            }
            if (alertSource != null)
            {
                alertSource.volume = Mathf.Lerp(alertSource.volume, alertWeight, lerpFactor);
            }
        }

        public void SetSuspicionInstant(float suspicion)
        {
            UpdateWeights(suspicion);
            if (calmSource != null) calmSource.volume = calmWeight;
            if (suspicionSource != null) suspicionSource.volume = suspicionWeight;
            if (alertSource != null) alertSource.volume = alertWeight;
        }

        /// <summary>
        /// Genera proceduralmente un stem de audio para pruebas y ejecución directa sin assets binarios pesados.
        /// </summary>
        public static AudioClip GenerateStemClip(string name, float baseFreq, float rhythmBpm, float durationSec = 2.5f)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * durationSec);
            float[] data = new float[totalSamples];

            float beatInterval = 60f / Mathf.Max(rhythmBpm, 1f);
            int samplesPerBeat = Mathf.FloorToInt(sampleRate * beatInterval);

            for (int i = 0; i < totalSamples; i++)
            {
                float time = (float)i / sampleRate;
                // Onda base senoidal con sub-armónicos oscuros
                float fundamental = Mathf.Sin(2f * Mathf.PI * baseFreq * time);
                float subBass = Mathf.Sin(2f * Mathf.PI * (baseFreq * 0.5f) * time) * 0.5f;

                // Modulación rítmica de tensión
                int beatPos = i % samplesPerBeat;
                float beatEnvelope = Mathf.Exp(-4.0f * ((float)beatPos / samplesPerBeat));

                data[i] = (fundamental * 0.6f + subBass * 0.4f) * (0.35f + 0.65f * beatEnvelope) * 0.4f;
            }

            var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Fábrica que ensambla el GameObject completo con sus 3 stems y configuración de audio.
        /// </summary>
        public static GameObject CreateAdaptiveMusicSystem(Transform parent = null)
        {
            var go = new GameObject("MusicaAdaptativa_Infiltracion");
            if (parent != null) go.transform.SetParent(parent, false);

            var system = go.AddComponent<AdaptiveTensionMusic>();

            // 1. Stem de Calma / Sigilo (drone marítimo grave a 55 Hz / 45 BPM)
            var calmGo = new GameObject("Stem_Calma_Sigilo");
            calmGo.transform.SetParent(go.transform, false);
            system.calmSource = calmGo.AddComponent<AudioSource>();
            system.calmSource.clip = GenerateStemClip("CalmStealthStem", 55f, 45f);
            system.calmSource.loop = true;
            system.calmSource.playOnAwake = false;

            // 2. Stem de Sospecha (pulso tenso a 110 Hz / 80 BPM)
            var suspGo = new GameObject("Stem_Sospecha_Tension");
            suspGo.transform.SetParent(go.transform, false);
            system.suspicionSource = suspGo.AddComponent<AudioSource>();
            system.suspicionSource.clip = GenerateStemClip("SuspicionTensionStem", 110f, 80f);
            system.suspicionSource.loop = true;
            system.suspicionSource.playOnAwake = false;

            // 3. Stem de Alerta Máxima (arpegio de alarma a 220 Hz / 130 BPM)
            var alertGo = new GameObject("Stem_Alerta_Persecucion");
            alertGo.transform.SetParent(go.transform, false);
            system.alertSource = alertGo.AddComponent<AudioSource>();
            system.alertSource.clip = GenerateStemClip("AlertChaseStem", 220f, 130f);
            system.alertSource.loop = true;
            system.alertSource.playOnAwake = false;

            system.SetSuspicionInstant(0f);
            return go;
        }
    }
}
