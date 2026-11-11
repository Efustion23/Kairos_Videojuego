# Entregable de Pruebas: VFX de Sonar, Post-Procesado Térmico y SFX Submarino (KAI-S12-03 #9)

Esta carpeta contiene todos los artefactos, shaders, perfiles y componentes desarrollados de forma aislada para la tarea **[S12-3] (Issue #9)**:

## Estructura de Archivos
- **`Shaders/`**:
  - `SonarScreenSweep.shader`: Shader URP para barrido radial en espacio de pantalla con borde cian brillante (`#3FE0D0`) y ondulaciones concéntricas a 60 FPS estables.
  - `SonarRadialSweep.shader`: Shader URP para el frente de onda expansivo en la superficie marina.
- **`Profiles/`**:
  - `ThermalProfile.asset`: Volume Profile URP de visión térmica con gradiente de azul cobalto profundo (`#2050FF`, -18°C) a ámbar brillante (`#FFD27A`, 37°C para presencia humana viva).
- **`Audio/`**:
  - `SonarAudioMixer.mixer`: AudioMixer configurado con grupos `Master`, `Sonar`, `SubmarineSFX` y `Ambience`.
- **`Scripts/`**:
  - `SubmarineAudioSuite.cs`: Síntesis procedural y enrutamiento al AudioMixer de ping acústico cian, eco metálico resonante, turbulencia hidrodinámica continua, crujidos bajo agua y reverberación marina.
  - `ThermalVisorBoatController.cs`: Controlador para alternar la visión térmica con tecla rápida `[V]` en la lancha, activando el `ThermalProfile` y desplegando el HUD FLIR.
  - `SonarWaveRadialSweep.cs`: Generador coordinado de onda expansiva radial y barrido en pantalla con sincronía acústica.
  - `MatsSonarPrueba.cs`: Factoría de materiales para los efectos de sonar en tiempo de ejecución.
- **`Tests/`**:
  - `SonarVfxAudioTests.cs`: Pruebas unitarias EditMode para verificar la síntesis de audio, shaders y perfiles térmicos.
