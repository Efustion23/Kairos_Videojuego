# 🎨🔊 KAIROS — Módulo de Arte 3D, VFX, Iluminación y Audio

> **Responsable:** Veliz Cochachi Christopher Jesus (`@christopher-26`)  
> **Ruta en el proyecto:** `Assets/Kairos/Arte y audio/`  
> **Ensamblado:** `Kairos.Pruebas` (`Assets/Kairos/Arte y audio/Scripts/Kairos.Pruebas.asmdef`)

---

## 📦 Arquitectura del Módulo

### 1. Audio Diegético, Espacial y Adaptativo
- **`Audio/Resources/SonarAudioMixer.mixer`**: AudioMixer maestro con grupos `Master`, `Sonar`, `SubmarineSFX` y `Ambience`.
- **`Scripts/SubmarineAudioSuite.cs`**: Biblioteca procedural de SFX submarinos (`PingAcoustic`, `EcoMetallic`, `SubmarineTurbulence`, `UnderwaterCreak`, `SonarReverb`) y enrutamiento al AudioMixer.
- **`Scripts/InteriorSpatialAudio.cs`**: Audio espacial 3D para cubiertas e interiores (`StepMetalWet`, `CompressorHumLoop`, `StormVentilationLoop`) con recuperación automática de loops procedurales en tiempo de ejecución.
- **`Scripts/AdaptiveTensionMusic.cs`**: Música adaptativa de 3 capas (`Calma`, `Sospecha`, `Alerta`) sincronizada con la sospecha de los guardias, `Lockdown`, regeneración automática de stems en Play Mode y respeto del volumen global.
- **`Scripts/MasterAudioMixerCalibration.cs`**: Calibración nominal en decibelios y snapshots dinámicos (`InfiltracionNormal`, `SonarActivo`, `DialogoTestigo`, `AlertaMaxima`) reactivos a los eventos de Fase 2 y Fase 3.
- **`Scripts/TacticalRadioCommsAudio.cs`**: Simulador VHF/UHF con filtrado pasa-banda (`350 Hz – 3200 Hz`), squelch PTT y tono Roger Beep conectado a hitos de abordaje, bloqueo de evidencias y sonar.

### 2. VFX, Shaders URP e Iluminación Nocturna
- **`Shaders/SonarRadialSweep.shader` & `Shaders/SonarScreenSweep.shader`**: Barrido radial cian (`#3FE0D0`) en superficie marina y en espacio de pantalla (`SonarWaveRadialSweep.cs` + `MatsSonarPrueba.cs`) con liberación limpia de materiales y canvas temporales.
- **`Shaders/BoatWakeFoam.shader`**: Estela hidrodinámica de espuma para la lancha RHIB.
- **`Shaders/InspectableOutlineGlow.shader`**: Resaltado forense por proximidad para evidencias coleccionables (`InspectablePropItem.cs` + `InspectableEvidenceProps.cs`).
- **`Scripts/MaritimeRainParticlesController.cs`**: Emisores de lluvia torrencial y salpicaduras en cubierta con material URP sin sombras, seguimiento horizontal de cámara, atenuación automática bajo cubierta y adaptación climática a los 3 sectores oceánicos.
- **`Scripts/MaritimeFogAtmosphereController.cs`**: Controlador de niebla marina, reflectores halógenos con parpadeo por salitre y sincronización no destructiva con `SectorTheme` en los 3 sectores.
- **`Scripts/GuardLanternPatrolRig.cs`**: Rig de linterna halógena de patrulla optimizado (`LightShadows.None` para proteger el atlas de sombras URP) con respuesta automática al estado de alerta y aturdimiento (`GuardAI.IsStunned`).
- **`Scripts/BridgeAndCabinInterior.cs`** & **`Scripts/ThermalVisorBoatController.cs`**: Ambientación del puente de mando y perfil térmico cobalto/ámbar.
- **`Profiles/NightMaritimePostProcessProfile.asset`** & **`Profiles/ThermalProfile.asset`**: Perfiles URP de post-procesado nocturno y termografía.

### 3. Cinemática de Tráiler y Showroom Interactivo
- **`Scripts/TrailerCinematicSequencer.cs`**: Secuenciador de tomas suaves interpoladas con presets para los **3 sectores oceánicos** (`LoadDefaultTrailerShots` y `LoadSectorShots(0 | 1 | 2)`).
- **`Scripts/ChristopherArtAudioShowcase.cs`**: Controlador central de exhibición para sustentar todas las entregas de Arte 3D, Iluminación y Audio:
  - HUD interactivo OnGUI con monitor de FPS en tiempo real.
  - Conmutador en caliente de efectos mediante teclado (`[1]` Sonar, `[2]` Visor Térmico, `[3]` Puente/Camarote, `[4]` Evidencias OIT, `[5]` Lluvia Marina, `[6]` Radio Táctica, `[7]` Cinemática de Tráiler).

---

## 🧪 Suites de Pruebas EditMode (`Tests/`)

| Archivo | Cobertura |
| :--- | :--- |
| `SonarVfxAudioTests.cs` | Ping acústico, eco metálico, turbulencia, crujidos, shaders de sonar y `ThermalProfile.asset` |
| `MaritimeRainAndWakeVfxTests.cs` | Inicialización de lluvia, intensidad/viento por sector, materiales URP y shader de estela |
| `TacticalRadioCommsTests.cs` | Filtros pasa-banda, Roger Beep, squelch PTT y `PlayTacticalCue` |
| `Semana13InteriorAndAudioTests.cs` | Pasos metálicos, compresores 3D con recuperación runtime, puente y linterna optimizada/aturdimiento |
| `Semana14AdaptiveMusicTests.cs` | Pesos por zona de tensión, generación de stems y recuperación en runtime (`EnsureStemsReady`) |
| `Semana14PropsAndShaderTests.cs` | Evidencias OIT, resumen forense y shader `InspectableOutlineGlow` |
| `Semana15LightingProfileTests.cs` | Reflectores, niebla marina y `NightMaritimePostProcessProfile.asset` |
| `Semana15MasterDeliveryTests.cs` | Calibración del mixer, pulso sonar, secuenciador multi-sector y suite completa S12–S15 |
| `ShowcaseIntegrationTests.cs` | Integración del showroom y conmutación de módulos |
