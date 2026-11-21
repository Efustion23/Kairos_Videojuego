using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Perfiles de snapshot para la mezcla maestra de audio (Semana 15 / S15-3).
    /// </summary>
    public enum AudioMixerSnapshotState
    {
        InfiltracionNormal,
        SonarActivo,
        DialogoTestigo,
        AlertaMaxima
    }

    /// <summary>
    /// Calibración de la mezcla maestra de audio y atenuación dinámica (ducking).
    /// Balancea en decibelios (dB) el sonar, motores, tormenta marina, pasos, música y voces de testigos.
    /// </summary>
    public class MasterAudioMixerCalibration : MonoBehaviour
    {
        [Header("AudioMixer Maestro")]
        public AudioMixer targetMixer;

        [Header("Niveles de Calibración Nominal (dB)")]
        [Range(-80f, 0f)] public float masterVolumeDb = 0f;
        [Range(-80f, 0f)] public float sonarVolumeDb = -2.5f;
        [Range(-80f, 0f)] public float ambienceVolumeDb = -8.0f;
        [Range(-80f, 6f)] public float dialogueVolumeDb = 2.0f;
        [Range(-80f, 0f)] public float musicVolumeDb = -6.0f;

        [Header("Atenuación Dinámica (Ducking)")]
        public float duckingMusicDb = -4.5f;
        public float duckingAmbienceDb = -3.0f;
        public float duckingTransitionSpeed = 4.0f;

        [Header("Estado Actual de Mezcla")]
        public AudioMixerSnapshotState currentState = AudioMixerSnapshotState.InfiltracionNormal;
        public bool autoTrackGameplayState = true;

        [SerializeField, HideInInspector] private System.Collections.Generic.List<string> exposedParamNames = new System.Collections.Generic.List<string>();
        private float sonarSnapshotTimer;

        private void OnValidate()
        {
            RefreshExposedParamsCache();
        }

        private void RefreshExposedParamsCache()
        {
#if UNITY_EDITOR
            exposedParamNames.Clear();
            if (targetMixer == null) return;
            var so = new UnityEditor.SerializedObject(targetMixer);
            var prop = so.FindProperty("m_ExposedParameters");
            if (prop == null) return;
            for (int i = 0; i < prop.arraySize; i++)
            {
                var nameProp = prop.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                    exposedParamNames.Add(nameProp.stringValue);
            }
#endif
        }

        private void Start()
        {
            if (targetMixer == null)
                targetMixer = SubmarineAudioSuite.Mixer;
            ApplyNominalLevels();
        }

        private void Update()
        {
            if (!Application.isPlaying || !autoTrackGameplayState) return;

            if (sonarSnapshotTimer > 0f)
            {
                sonarSnapshotTimer -= Time.unscaledDeltaTime;
                if (sonarSnapshotTimer <= 0f && currentState == AudioMixerSnapshotState.SonarActivo)
                    TransitionToState(AudioMixerSnapshotState.InfiltracionNormal, 0.5f);
                return;
            }

            var inf = InfiltrationController.I;
            if (inf == null) return;

            var desired = AudioMixerSnapshotState.InfiltracionNormal;
            if (inf.mode == InfiltrationController.Mode.Dialog)
            {
                desired = AudioMixerSnapshotState.DialogoTestigo;
            }
            else if (inf.Lockdown)
            {
                desired = AudioMixerSnapshotState.AlertaMaxima;
            }
            else if (inf.guards != null)
            {
                for (int i = 0; i < inf.guards.Length; i++)
                {
                    if (inf.guards[i] != null && !inf.guards[i].IsStunned && inf.guards[i].state == GuardState.Alerta)
                    {
                        desired = AudioMixerSnapshotState.AlertaMaxima;
                        break;
                    }
                }
            }

            if (desired != currentState)
                TransitionToState(desired, 0.45f);
        }

        /// <summary>
        /// Activa temporalmente el perfil SonarActivo para atenuar la tormenta durante el eco del pulso.
        /// </summary>
        public void PulseSonarSnapshot(float holdSeconds = 1.8f)
        {
            sonarSnapshotTimer = Mathf.Max(sonarSnapshotTimer, Mathf.Max(0.1f, holdSeconds));
            TransitionToState(AudioMixerSnapshotState.SonarActivo, 0.25f);
        }

        /// <summary>
        /// Aplica los parámetros en decibelios en el AudioMixer si los parámetros expuestos existen.
        /// </summary>
        public void ApplyNominalLevels()
        {
            if (targetMixer == null) return;

            SetMixerParamSafe("MasterVolume", masterVolumeDb);
            SetMixerParamSafe("SonarVolume", sonarVolumeDb);
            SetMixerParamSafe("AmbienceVolume", ambienceVolumeDb);
            SetMixerParamSafe("DialogueVolume", dialogueVolumeDb);
            SetMixerParamSafe("MusicVolume", musicVolumeDb);
        }

        public void TransitionToState(AudioMixerSnapshotState newState, float transitionTime = 1.0f)
        {
            currentState = newState;
            if (targetMixer == null) return;

            switch (newState)
            {
                case AudioMixerSnapshotState.InfiltracionNormal:
                    SetMixerParamSafe("MusicVolume", musicVolumeDb);
                    SetMixerParamSafe("AmbienceVolume", ambienceVolumeDb);
                    break;
                case AudioMixerSnapshotState.SonarActivo:
                    // Resalta el ping y eco atenuando ligeramente la tormenta
                    SetMixerParamSafe("AmbienceVolume", ambienceVolumeDb + duckingAmbienceDb);
                    break;
                case AudioMixerSnapshotState.DialogoTestigo:
                    // Prioriza la inteligibilidad de la voz de sobrevivientes aplicando ducking a música y ambiente
                    SetMixerParamSafe("MusicVolume", musicVolumeDb + duckingMusicDb);
                    SetMixerParamSafe("AmbienceVolume", ambienceVolumeDb + duckingAmbienceDb);
                    break;
                case AudioMixerSnapshotState.AlertaMaxima:
                    // Resalta la banda sonora de persecución y alarmas
                    SetMixerParamSafe("MusicVolume", musicVolumeDb + 2.0f);
                    break;
            }
        }

        private void SetMixerParamSafe(string paramName, float valueDb)
        {
            if (targetMixer != null && exposedParamNames != null && exposedParamNames.Contains(paramName))
            {
                targetMixer.SetFloat(paramName, valueDb);
            }
        }

        /// <summary>
        /// Crea e inicializa el nodo de calibración maestra con presets balanceados.
        /// </summary>
        public static GameObject CreateMasterAudioCalibration(AudioMixer mixer = null, Transform parent = null)
        {
            var go = new GameObject("Calibracion_AudioMixer_Maestro");
            if (parent != null) go.transform.SetParent(parent, false);

            var cal = go.AddComponent<MasterAudioMixerCalibration>();
            cal.targetMixer = mixer;
            cal.RefreshExposedParamsCache();
            cal.ApplyNominalLevels();

            return go;
        }
    }
}
