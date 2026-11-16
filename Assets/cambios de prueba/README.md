# Entregables de Christopher Veliz: Arte 3D, Iluminación y Audio

Esta carpeta aislada contiene todos los recursos, artefactos, shaders, audio y componentes desarrollados para las tareas asignadas a **Christopher Veliz (CV)** en el plan de desarrollo de KAIROS: Sombras en Altamar.

---

## 🌊 Semana 12: `[S12-3]` VFX de Sonar, Post-Procesado Térmico y SFX Submarinos
- **`Shaders/SonarRadialSweep.shader`**: Shader URP para barrido radial en superficie marina (`#3FE0D0`).
- **`Shaders/SonarScreenSweep.shader`**: Shader URP en espacio de pantalla con borde cian brillante y ondulaciones concéntricas.
- **`Profiles/ThermalProfile.asset`**: Volume Profile URP con gradiente cobalto (-18°C) a ámbar brillante (37°C para presencia humana viva).
- **`Audio/SonarAudioMixer.mixer`**: AudioMixer configurado con grupos `Master`, `Sonar`, `SubmarineSFX` y `Ambience`.
- **`Scripts/SubmarineAudioSuite.cs`**: Síntesis procedural y enrutamiento de ping de sonar cian, eco en chapa, turbulencia hidrodinámica y crujidos bajo agua.
- **`Scripts/ThermalVisorBoatController.cs`**: Alternador de visor térmico FLIR en la lancha con tecla rápida `[V]`.
- **`Scripts/SonarWaveRadialSweep.cs`**: Generador de onda radial en superficie y pantalla con sincronía acústica.
- **`Scripts/MatsSonarPrueba.cs`**: Factoría de materiales en runtime.
- **`Tests/SonarVfxAudioTests.cs`**: Suite de tests unitarios EditMode para la Semana 12.

---

## 🚢 Semana 13: `[S13-3]` Modelado de Camarotes Internos, Personajes y Pasos sobre Metal
- **`Scripts/BridgeAndCabinInterior.cs`**: Ambientación modular del puente de mando (timonería, radar CRT, mesa de derrota con flexo de iluminación tenue) y camarote del contramaestre (literas empotradas, taquillas metálicas y aplique tenue).
- **`Scripts/GuardLanternPatrolRig.cs`**: Rig de patrulla con linterna halógena para los guardias, sincronización del balanceo durante la marcha y modo alerta ante sospecha.
- **`Scripts/InteriorSpatialAudio.cs`**: Sistema de audio espacial 3D diegético para:
  - Pasos metálicos sobre rejilla y chapa húmeda (`StepMetalWet`).
  - Zumbido industrial continuo de compresores de frío a 50 Hz (`CompressorHumLoop`).
  - Silbido de viento y tormenta colándose en mamparos (`StormVentilationLoop`).
- **`Tests/Semana13InteriorAndAudioTests.cs`**: Pruebas unitarias EditMode para verificar la síntesis de audio 3D, el rig de linterna y la ambientación de interiores.

---

## 🔍 Semana 14: `[S14-3]` Objetos Coleccionables 3D, Shader de Resaltado y Música Adaptativa
- **`Shaders/InspectableOutlineGlow.shader`**: Shader URP de resplandor silueta y efecto Fresnel interactivo con pulsación senoidal ajustable para guiar visualmente al jugador hacia evidencias críticas.
- **`Scripts/InspectableEvidenceProps.cs`**: Modelado procedural, jerarquía física y metadatos OIT para las 4 evidencias del caso:
  - *Bitácora Real de Navegación* (OIT C188 - Art. 8: Falsificación de registros náuticos y jornadas abusivas).
  - *Lote de Pasaportes Retenidos* (OIT C029: Retención de documentos de identidad en trabajo forzoso).
  - *Radio VHF Satelital Clandestina* (Directrices OMI/OIT: Frecuencia no declarada para transbordos).
  - *Terminal Plotter GPS Náutico* (Normativa INDNR: Registro de coordenadas con apagado intencional de AIS).
- **`Scripts/AdaptiveTensionMusic.cs`**: Motor de música adaptativa por stems (Calma/Sigilo, Sospecha/Tensión y Alerta Máxima) reactivo en tiempo real al porcentaje de sospecha (0 a 100%).
- **`Tests/Semana14PropsAndShaderTests.cs`**: Suite de tests automatizados EditMode para verificar la inicialización de colisionadores, renderers, metadatos jurídicos y activación dinámica del resplandor de proximidad.
- **`Tests/Semana14AdaptiveMusicTests.cs`**: Suite de pruebas EditMode para verificar la síntesis de stems, ponderaciones dinámicas de volumen y crossfade por sospecha.

---

## 💡 Semana 15: `[S15-3]` Iluminación Nocturna, Niebla Volumétrica y Perfil de Post-Procesado
- **`Profiles/NightMaritimePostProcessProfile.asset`**: Volume Profile URP para ambientación nocturna marítima (Tonemapping ACES, corrección de color cinemática con tinte azul marino, bloom cálido para focos de cubierta y grano analógico sutil).
- **`Scripts/MaritimeFogAtmosphereController.cs`**: Controlador integral de atmósfera oceánica:
  - Niebla marina volumétrica y densidad atmosférica nocturna.
  - Reflectores halógenos de cubierta con simulación de parpadeo eléctrico por corrosión salina.
  - Baliza estroboscópica de peligro en el mástil del pesquero.
- **`Tests/Semana15LightingProfileTests.cs`**: Pruebas unitarias EditMode para verificar la inicialización de luces, configuración de niebla marítima y carga de componentes del perfil de post-procesado.

---

## 🎬 Semana 15 (Cierre): `[S15-3]` Balance Maestro de Audio, Secuenciador Cinemático y Suite Final
- **`Scripts/MasterAudioMixerCalibration.cs`**: Calibración en decibelios (dB) de la mezcla maestra de audio del videojuego:
  - Nivelación nominal: Master (0 dB), Sonar (-2.5 dB), Ambiente y Tormenta (-8 dB), Diálogos (+2 dB) y Música (-6 dB).
  - Sistema de transiciones y ducking automático para priorizar la inteligibilidad de testimonios y el impacto acústico del sonar.
- **`Scripts/TrailerCinematicSequencer.cs`**: Secuenciador cinemático de tomas orbitales de cámara para el tráiler oficial y menú principal:
  - *Toma 1:* Plano general del Hai Feng 77 entre niebla espesa y oleaje nocturno.
  - *Toma 2:* Travelling rasante lateral sobre la estela de la lancha de asalto RHIB.
  - *Toma 3:* Enfoque vertical a la escala de gato y reflectores de popa.
  - *Toma 4:* Travelling interior en el puente de mando frente al radar y evidencias OIT.
- **`Tests/Semana15MasterDeliveryTests.cs`**: Suite de integración y verificación final EditMode que valida todos los componentes desarrollados por Christopher Veliz (Semanas 12 a 15).

---

## 🌧️ Sistema de Lluvia Torrencial Marina y Estela de Lancha
- **Shaders/BoatWakeFoam.shader**: Shader URP en espacio de superficie para la estela de espuma en V y agua batida que deja la lancha RHIB al navegar a toda velocidad.
- **Scripts/MaritimeRainParticlesController.cs**: Controlador integral de lluvia marina torrencial:
  - Emisor procedural de 600 gotas con inclinación por viento oceánico.
  - Generación de salpicaduras sobre la cubierta metálica del pesquero.
  - Audio acústico procedural sintetizado de impacto de gotas sobre chapa metálica.
- **Tests/MaritimeRainAndWakeVfxTests.cs**: Suite de tests automatizados EditMode para validación de partículas, shaders y síntesis acústica de tormenta.
