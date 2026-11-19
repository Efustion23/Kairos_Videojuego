using System;
using System.Collections;
using UnityEngine;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Simulador acústico diegético de radiofónico táctico UHF/VHF militar para guardacostas (Christopher Veliz - Audio).
    /// Genera proceduralmente la apertura de squelch, ruido de fondo de estática y tono "Roger Beep",
    /// aplicando filtrado pasa-banda analógico característico de comunicaciones marítimas en altamar.
    /// </summary>
    public class TacticalRadioCommsAudio : MonoBehaviour
    {
        [Header("Configuración de Radiofrecuencia")]
        [Range(0f, 1f)] public float masterRadioVolume = 0.85f;
        public float highPassCutoff = 350f;  // Corte de graves
        public float lowPassCutoff = 3200f;  // Corte de agudos (canal de voz telefónico/UHF)

        [Header("Efectos Acústicos de Transmisión")]
        public bool enablePttBurst = true;
        public bool enableRogerBeep = true;
        public bool enableBackgroundStatic = true;
        [Range(0f, 1f)] public float staticHissVolume = 0.25f;

        [Header("Fuentes de Audio")]
        public AudioSource voiceAudioSource;
        public AudioSource fxAudioSource;

        private AudioClip pttOpenClip;
        private AudioClip rogerBeepClip;
        private AudioClip staticHissClip;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            InitializeRadioComms();
        }

        public void InitializeRadioComms()
        {
            if (IsInitialized) return;

            SetupAudioSources();
            GenerateRadioSfxClips();

            IsInitialized = true;
        }

        private void SetupAudioSources()
        {
            if (voiceAudioSource == null)
            {
                GameObject voiceChild = new GameObject("Radio_Voice_Audio");
                voiceChild.SetActive(false);
                voiceChild.transform.SetParent(transform, false);
                voiceAudioSource = voiceChild.AddComponent<AudioSource>();
                voiceAudioSource.playOnAwake = false;
                voiceChild.SetActive(true);
            }

            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.spatialBlend = 0.0f; // Sonido 2D en cascos del operador
            voiceAudioSource.volume = masterRadioVolume;

            // Filtro pasa-banda para efecto walkie-talkie en el mismo nodo que voiceAudioSource
            GameObject filterHost = voiceAudioSource.gameObject;
            var highPass = filterHost.GetComponent<AudioHighPassFilter>();
            if (highPass == null) highPass = filterHost.AddComponent<AudioHighPassFilter>();
            if (highPass != null) highPass.cutoffFrequency = highPassCutoff;

            var lowPass = filterHost.GetComponent<AudioLowPassFilter>();
            if (lowPass == null) lowPass = filterHost.AddComponent<AudioLowPassFilter>();
            if (lowPass != null) lowPass.cutoffFrequency = lowPassCutoff;

            // AudioSource secundario para beeps y clics mecánicos del micro PTT
            GameObject fxChild = new GameObject("Radio_Squelch_FX");
            fxChild.SetActive(false);
            fxChild.transform.SetParent(transform, false);
            fxAudioSource = fxChild.AddComponent<AudioSource>();
            fxAudioSource.playOnAwake = false;
            fxAudioSource.spatialBlend = 0.0f;
            fxAudioSource.volume = masterRadioVolume * 0.9f;
            fxChild.SetActive(true);
        }

        private void GenerateRadioSfxClips()
        {
            pttOpenClip = GeneratePttBurstClip(0.09f);
            rogerBeepClip = GenerateRogerBeepClip();
            staticHissClip = GenerateStaticNoiseClip(1.5f);
        }

        /// <summary>
        /// Dispara una transmisión vocal por radio con squelch inicial y roger beep de confirmación.
        /// </summary>
        public void PlayRadioTransmission(AudioClip vocalClip)
        {
            InitializeRadioComms();

            if (fxAudioSource != null && enablePttBurst && pttOpenClip != null)
            {
                fxAudioSource.PlayOneShot(pttOpenClip);
            }

            if (voiceAudioSource != null && vocalClip != null)
            {
                voiceAudioSource.clip = vocalClip;
                if (Application.isPlaying)
                {
                    voiceAudioSource.Play();
                    StartCoroutine(HandleTransmissionEnd(vocalClip.length));
                }
            }
        }

        private IEnumerator HandleTransmissionEnd(float duration)
        {
            yield return new WaitForSeconds(duration);

            if (enableRogerBeep && fxAudioSource != null && rogerBeepClip != null)
            {
                fxAudioSource.PlayOneShot(rogerBeepClip);
            }
        }

        /// <summary>
        /// Ráfaga inicial de conmutación de frecuencia y apertura de squelch.
        /// </summary>
        public static AudioClip GeneratePttBurstClip(float durationSeconds)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * durationSeconds);
            float[] data = new float[samples];
            System.Random rand = new System.Random(4041);

            for (int i = 0; i < samples; i++)
            {
                float envelope = 1.0f - ((float)i / samples);
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                data[i] = noise * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create("Radio_PTT_Open", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Tono militar doble de cierre de canal (Roger Beep: 1040 Hz seguido de 880 Hz).
        /// </summary>
        public static AudioClip GenerateRogerBeepClip()
        {
            int sampleRate = 44100;
            float tone1Duration = 0.07f;
            float tone2Duration = 0.07f;
            int samples1 = Mathf.CeilToInt(sampleRate * tone1Duration);
            int samples2 = Mathf.CeilToInt(sampleRate * tone2Duration);
            int totalSamples = samples1 + samples2;
            float[] data = new float[totalSamples];

            // Tono 1: 1040 Hz
            for (int i = 0; i < samples1; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin((float)i / samples1 * Mathf.PI);
                data[i] = Mathf.Sin(2.0f * Mathf.PI * 1040f * t) * env * 0.6f;
            }

            // Tono 2: 880 Hz
            for (int i = 0; i < samples2; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin((float)i / samples2 * Mathf.PI);
                data[samples1 + i] = Mathf.Sin(2.0f * Mathf.PI * 880f * t) * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Radio_Roger_Beep", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Loop continuo de estática de canal sintonizado en banda marina VHF canal 16.
        /// </summary>
        public static AudioClip GenerateStaticNoiseClip(float durationSeconds)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * durationSeconds);
            float[] data = new float[samples];
            System.Random rand = new System.Random(8812);

            for (int i = 0; i < samples; i++)
            {
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                data[i] = noise * 0.18f;
            }

            AudioClip clip = AudioClip.Create("Radio_Static_Loop", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (pttOpenClip != null) Destroy(pttOpenClip);
                if (rogerBeepClip != null) Destroy(rogerBeepClip);
                if (staticHissClip != null) Destroy(staticHissClip);
            }
        }
    }
}
