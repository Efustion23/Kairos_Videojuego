using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Controlador integral de demostración y telemetría de Arte 3D, Shaders y Audio.
    /// Diseñado para la presentación, sustentación y verificación de los entregables de Christopher Veliz.
    /// Permite alternar en tiempo real todas las tecnologías desarrolladas mediante teclado o interfaz táctica.
    /// </summary>
    public class ChristopherArtAudioShowcase : MonoBehaviour
    {
        public enum ShowcaseMode
        {
            None = 0,
            SonarSweep = 1,
            ThermalVisor = 2,
            BridgeAndGuard = 3,
            EvidenceOutline = 4,
            MaritimeRainAndFog = 5,
            TacticalRadio = 6,
            CinematicTrailer = 7
        }

        [Header("Estado de Demostración")]
        public ShowcaseMode activeMode = ShowcaseMode.SonarSweep;
        public bool showTelemetryHud = true;

        [Header("Referencias Opcionales")]
        public SonarWaveRadialSweep sonarComponent;
        public ThermalVisorBoatController thermalComponent;
        public BridgeAndCabinInterior bridgeComponent;
        public InspectablePropItem evidenceComponent;
        public MaritimeRainParticlesController rainComponent;
        public TacticalRadioCommsAudio radioComponent;
        public TrailerCinematicSequencer cinematicComponent;

        private float fpsAccumulator = 0f;
        private int fpsFrames = 0;
        private float currentFps = 60f;
        private float fpsTimer = 0.5f;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            InitializeShowcase();
        }

        public void InitializeShowcase()
        {
            if (IsInitialized) return;

            // Autodetectar componentes en la jerarquía si no están asignados
            sonarComponent = sonarComponent ?? FindFirstObjectByType<SonarWaveRadialSweep>();
            thermalComponent = thermalComponent ?? FindFirstObjectByType<ThermalVisorBoatController>();
            bridgeComponent = bridgeComponent ?? FindFirstObjectByType<BridgeAndCabinInterior>();
            evidenceComponent = evidenceComponent ?? FindFirstObjectByType<InspectablePropItem>();
            rainComponent = rainComponent ?? FindFirstObjectByType<MaritimeRainParticlesController>();
            radioComponent = radioComponent ?? FindFirstObjectByType<TacticalRadioCommsAudio>();
            cinematicComponent = cinematicComponent ?? FindFirstObjectByType<TrailerCinematicSequencer>();

            IsInitialized = true;
        }

        private void Update()
        {
            UpdateFpsMeter();
            HandleKeyboardShortcuts();
        }

        private void UpdateFpsMeter()
        {
            fpsAccumulator += Time.unscaledDeltaTime;
            fpsFrames++;
            fpsTimer -= Time.unscaledDeltaTime;

            if (fpsTimer <= 0f)
            {
                currentFps = fpsFrames / fpsAccumulator;
                fpsAccumulator = 0f;
                fpsFrames = 0;
                fpsTimer = 0.5f;
            }
        }

        private void HandleKeyboardShortcuts()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchMode(ShowcaseMode.SonarSweep);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchMode(ShowcaseMode.ThermalVisor);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchMode(ShowcaseMode.BridgeAndGuard);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchMode(ShowcaseMode.EvidenceOutline);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchMode(ShowcaseMode.MaritimeRainAndFog);
            if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchMode(ShowcaseMode.TacticalRadio);
            if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchMode(ShowcaseMode.CinematicTrailer);
            if (Input.GetKeyDown(KeyCode.H)) showTelemetryHud = !showTelemetryHud;
        }

        public void SwitchMode(ShowcaseMode newMode)
        {
            activeMode = newMode;

            switch (newMode)
            {
                case ShowcaseMode.SonarSweep:
                    if (sonarComponent != null) sonarComponent.FireSonarPulse(sonarComponent.transform.position);
                    break;
                case ShowcaseMode.ThermalVisor:
                    if (thermalComponent != null) thermalComponent.ToggleThermalVisor();
                    break;
                case ShowcaseMode.EvidenceOutline:
                    if (evidenceComponent != null) evidenceComponent.SetHighlight(true);
                    break;
                case ShowcaseMode.TacticalRadio:
                    if (radioComponent != null) radioComponent.PlayRadioTransmission(null);
                    break;
                case ShowcaseMode.CinematicTrailer:
                    if (cinematicComponent != null) cinematicComponent.PlaySequence();
                    break;
            }
        }

        private void OnGUI()
        {
            if (!showTelemetryHud) return;

            GUI.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 0.92f);
            GUI.Box(new Rect(15, 15, 480, 240), "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.90f, 0.85f) }
            };

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };

            GUIStyle highlightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.85f, 0.35f) }
            };

            GUILayout.BeginArea(new Rect(25, 20, 460, 230));

            GUILayout.Label("KAIROS: SOMBRAS EN ALTAMAR · SHOWCASE ARTE & AUDIO", titleStyle);
            GUILayout.Label("Desarrollador: Christopher Veliz (Semanas 12 a 15)", textStyle);
            GUILayout.Space(4);

            GUILayout.Label($"Rendimiento: {currentFps:F1} FPS | Modo Activo: {activeMode}", highlightStyle);
            GUILayout.Space(4);

            GUILayout.Label("[1] Barrido Sonar Cian 3FE0D0 + Audio Diegético", textStyle);
            GUILayout.Label("[2] Visor Térmico FLIR (Cobalto a Ámbar)", textStyle);
            GUILayout.Label("[3] Puente de Mando + Linterna de Guardia", textStyle);
            GUILayout.Label("[4] Evidencias OIT + Shader Resplandor Silueta", textStyle);
            GUILayout.Label("[5] Lluvia Torrencial + Salpicaduras en Cubierta", textStyle);
            GUILayout.Label("[6] Radiofonía Táctica UHF/VHF + Roger Beep", textStyle);
            GUILayout.Label("[7] Secuenciador Cinemático de Tráiler Oficial", textStyle);
            GUILayout.Label("[H] Ocultar / Mostrar este HUD de telemetría", textStyle);

            GUILayout.EndArea();
        }
    }
}
