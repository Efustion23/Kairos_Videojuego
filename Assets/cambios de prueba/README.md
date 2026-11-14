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

## 🔍 Semana 14: `[S14-3]` Objetos Coleccionables 3D y Shader de Resaltado Interactivo
- **`Shaders/InspectableOutlineGlow.shader`**: Shader URP de resplandor silueta y efecto Fresnel interactivo con pulsación senoidal ajustable para guiar visualmente al jugador hacia evidencias críticas.
- **`Scripts/InspectableEvidenceProps.cs`**: Modelado procedural, jerarquía física y metadatos OIT para las 4 evidencias del caso:
  - *Bitácora Real de Navegación* (OIT C188 - Art. 8: Falsificación de registros náuticos y jornadas abusivas).
  - *Lote de Pasaportes Retenidos* (OIT C029: Retención de documentos de identidad en trabajo forzoso).
  - *Radio VHF Satelital Clandestina* (Directrices OMI/OIT: Frecuencia no declarada para transbordos).
  - *Terminal Plotter GPS Náutico* (Normativa INDNR: Registro de coordenadas con apagado intencional de AIS).
- **`Tests/Semana14PropsAndShaderTests.cs`**: Suite de tests automatizados EditMode para verificar la inicialización de colisionadores, renderers, metadatos jurídicos y activación dinámica del resplandor de proximidad.
