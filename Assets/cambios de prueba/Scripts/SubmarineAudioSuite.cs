using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Biblioteca de efectos de sonido submarinos diegéticos para el pulso de sonar,
    /// eco en mamparos de acero, turbulencia hidrodinámica y crujidos bajo agua.
    /// Integrada con AudioMixer para mezcla y ecualización profesional.
    /// </summary>
    public static class SubmarineAudioSuite
    {
        const int SampleRate = 44100;
        static readonly System.Random Rng = new System.Random(88);
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

        static AudioClip pingAcoustic;
        static AudioClip ecoMetallic;
        static AudioClip subTurbulence;
        static AudioClip underwaterCreak;
        static AudioClip sonarReverb;

        /// <summary>
        /// Ping acústico activo del sonar submarino en cian (#3FE0D0): modulación chirp (1220 Hz -> 720 Hz)
        /// y dispersión en agua salada.
        /// </summary>
        public static AudioClip PingAcoustic => pingAcoustic ??= MakeClip("Prueba_PingAcoustic", 1.25f, (t, u) =>
        {
            float freq = Mathf.Lerp(1220f, 720f, Mathf.Pow(u, 0.42f));
            float carrier = Sin(freq, t);
            float subCarrier = Sin(freq * 0.5f, t) * 0.32f;
            float env = u < 0.02f ? (u / 0.02f) : Mathf.Exp(-u * 4.9f);
            float dispersion = 1f + 0.12f * Sin(36f, t);
            return (carrier + subCarrier) * env * dispersion * 0.85f;
        });

        /// <summary>
        /// Eco metálico: resonancia acústica multicapa de los mamparos de acero naval del buque pesquero
        /// (armónicos 440, 880, 1320, 2180 Hz) con flutter y rebote tardío.
        /// </summary>
        public static AudioClip EcoMetallic => ecoMetallic ??= MakeClip("Prueba_EcoMetallic", 1.75f, (t, u) =>
        {
            float m1 = Sin(440f, t) * 0.45f;
            float m2 = Sin(880f, t) * 0.35f;
            float m3 = Sin(1320f, t) * 0.22f;
            float m4 = Sin(2180f, t) * 0.14f;
            float flutter = Sin(16f, t) * 0.2f + 0.8f;
            float hullNoise = Noise() * 0.08f * Mathf.Exp(-u * 11f);
            float env = Mathf.Exp(-u * 3.3f);
            float bounce = u > 0.22f ? Mathf.Exp(-(u - 0.22f) * 4.6f) * 0.42f : 0f;
            return ((m1 + m2 + m3 + m4) * flutter + hullNoise) * (env + bounce) * 0.8f;
        });

        /// <summary>
        /// Turbulencia hidrodinámica submarina: flujo continuo, cavitación y estela de agua bajo el casco.
        /// </summary>
        public static AudioClip SubmarineTurbulence => subTurbulence ??= MakeClip("Prueba_SubTurbulence", 3.2f, (t, u) =>
        {
            float surge = Sin(0.85f, t) * 0.35f + 0.65f;
            float cavitation = Noise() * (0.28f + 0.18f * Sin(14f, t));
            float rumble = Sin(36f + 7f * Sin(1.8f, t), t) * 0.45f;
            return (rumble + cavitation * 0.4f) * surge * 0.72f;
        }, true);

        /// <summary>
        /// Crujidos metálicos bajo agua: esfuerzo y tensión hidrostática en cuadernas y chapa naval.
        /// </summary>
        public static AudioClip UnderwaterCreak => underwaterCreak ??= MakeClip("Prueba_UnderwaterCreak", 2.4f, (t, u) =>
        {
            float pitch = 56f + 24f * Mathf.Sin(t * 3.6f);
            float groan = Sin(pitch, t) * 0.58f + Sin(pitch * 2.05f, t) * 0.22f;
            float friction = Noise() * 0.18f * (Sin(17f, t) > 0f ? 1f : 0.25f);
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));
            return (groan + friction) * env * 0.82f;
        });

        /// <summary>
        /// Reverberación marina difusa: cola acústica por reflexión de la onda en el fondo marino y termoclina.
        /// </summary>
        public static AudioClip SonarReverb => sonarReverb ??= MakeClip("Prueba_SonarReverb", 2.2f, (t, u) =>
        {
            float tail1 = Sin(340f, t) * Mathf.Exp(-u * 2.7f) * 0.42f;
            float tail2 = Sin(170f, t) * Mathf.Exp(-u * 2.0f) * 0.35f;
            float diffuse = Noise() * 0.12f * Mathf.Exp(-u * 3.4f);
            return (tail1 + tail2 + diffuse) * 0.75f;
        });

        // ---------- AudioMixer y Enrutamiento ----------
        static AudioMixer mixerAsset;
        static AudioMixerGroup masterGrp, sonarGrp, submarineGrp, ambienceGrp;
        static bool mixerLoaded;

        public static AudioMixer Mixer { get { LoadMixer(); return mixerAsset; } }
        public static AudioMixerGroup MasterGroup { get { LoadMixer(); return masterGrp; } }
        public static AudioMixerGroup SonarGroup { get { LoadMixer(); return sonarGrp; } }
        public static AudioMixerGroup SubmarineGroup { get { LoadMixer(); return submarineGrp; } }
        public static AudioMixerGroup AmbienceGroup { get { LoadMixer(); return ambienceGrp; } }

        public static void LoadMixer()
        {
            if (mixerLoaded) return;
            mixerLoaded = true;
            try
            {
                mixerAsset = Resources.Load<AudioMixer>("SonarAudioMixer");
#if UNITY_EDITOR
                if (mixerAsset == null)
                    mixerAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/cambios de prueba/Audio/SonarAudioMixer.mixer");
#endif
                if (mixerAsset != null)
                {
                    var mg = mixerAsset.FindMatchingGroups("Master");
                    if (mg != null && mg.Length > 0) masterGrp = mg[0];

                    var sg = mixerAsset.FindMatchingGroups("Sonar");
                    if (sg != null && sg.Length > 0) sonarGrp = sg[0];
                    else sonarGrp = masterGrp;

                    var subg = mixerAsset.FindMatchingGroups("SubmarineSFX");
                    if (subg != null && subg.Length > 0) submarineGrp = subg[0];
                    else submarineGrp = sonarGrp ?? masterGrp;

                    var ag = mixerAsset.FindMatchingGroups("Ambience");
                    if (ag != null && ag.Length > 0) ambienceGrp = ag[0];
                    else ambienceGrp = masterGrp;
                }
            }
            catch { }
        }

        static AudioSource sharedSource;
        static AudioSource OutputSource
        {
            get
            {
                if (sharedSource == null)
                {
                    var go = new GameObject("Prueba_SubmarineAudioPlayer");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    sharedSource = go.AddComponent<AudioSource>();
                    sharedSource.playOnAwake = false;
                    sharedSource.ignoreListenerPause = true;
                }
                return sharedSource;
            }
        }

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            LoadMixer();
            if (submarineGrp != null) OutputSource.outputAudioMixerGroup = submarineGrp;
            OutputSource.PlayOneShot(clip, volume);
        }

        public static AudioSource CreateLoop(GameObject target, AudioClip clip, float volume = 0.2f)
        {
            LoadMixer();
            var src = target.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.volume = volume;
            src.outputAudioMixerGroup = submarineGrp ?? ambienceGrp ?? masterGrp;
            src.Play();
            return src;
        }
    }
}
