# 📋 Plan de Tareas y Sprints (Equipo de 3)

> **KAIROS: Sombras en Altamar**  
> *Desglose de tareas del plan de producción adaptadas a la capacidad de 3 desarrolladores.*

---

## 1. Roadmap de Hitos Clave

| Hito | Nombre | Meta Principal |
| :---: | :--- | :--- |
| **H0** | **Arranque y Configuración (Actual)** | Repositorio organizado, Git LFS activo, roles asignados y guía de trabajo. |
| **H1** | **Fin de Preproducción (Mes 1–3)** | Spikes técnicos aprobados a 60 fps (Shader de agua, barco estático, IA básica). |
| **H2** | **Prototipo Jugable (Mes 4–7)** | Caso completo jugable de principio a fin en greybox (las 4 fases conectadas). |
| **H3** | **Vertical Slice (Mes 8–12)** | Caso *Hai Feng 77* (Sector 1) con calidad visual y sonora final. |
| **H4** | **Contenido de Lanzamiento** | 3 Sectores oceánicos, 8 casos principales y árbol de mejoras. |

---

## 2. Sprint 1: Cimientos de las 4 Fases y Entorno

### 🧑‍💻 Persona 1 (Gameplay & Systems Programmer)
- [ ] **T1.1 (Fase 1):** Implementar la lógica matemática del **Índice de Riesgo ($IR$)** en C# con pruebas unitarias en `Assets/Kairos/Tests/EditMode/CaseRulesTests.cs`.
- [ ] **T1.2 (Fase 1):** Crear el generador de trayectorias AIS y detección básica de anomalías (apagado de transpondedor > 12 h).
- [ ] **T1.3 (Fase 3):** Refactorizar `PlayerAgent.cs` para soportar estados de sigilo (de pie, agachado, correr) y resbalón en cubierta mojada.
- [ ] **T1.4 (Fase 3):** Implementar sistema de sellado criptográfico de pruebas (cálculo de hash SHA-256 de las fotos tomadas).

### 🎨 Persona 2 (Technical Artist & Level Designer)
- [ ] **T2.1 (Océano):** Ajustar el shader de agua (`Ocean.shader`) y script `SeaStateController` con parámetros de oleaje Douglas 0–9.
- [ ] **T2.2 (Escenario):** Crear kit modular de pasillos, compuertas estancas y escaleras metálicas para el buque potero *Hai Feng 77* (usando ProBuilder o Blender).
- [ ] **T2.3 (Props):** Crear prefabs optimizados con colliders y materiales URP/HDRP a partir de los modelos de Poly Haven (`barrel_03`, `plastic_crate`, etc.).
- [ ] **T2.4 (Fase 2):** Prototipo de visualización de nube de puntos acústica con VFX Graph / Shader para el sonar.

### 🎧 Persona 3 (UI/UX, Audio, Narrative & Integration)
- [ ] **T3.1 (UI Fase 1):** Diseñar el mockup interactivo del mapa táctico orbital (selector de capas AIS, SAR, VIIRS).
- [ ] **T3.2 (UI Fase 3):** Diseñar el HUD minimalista de infiltración (indicador de visibilidad, sospecha de IA y maletín de evidencias).
- [ ] **T3.3 (Audio):** Implementar el gestor de audio ambiental (sonido de oleaje dinámico, viento según tormenta y crujidos del casco).
- [ ] **T3.4 (Narrativa):** Redactar las fichas del Códice correspondientes al Convenio C188 de la OIT y los antecedentes del *Hai Feng 77*.

---

## 3. Sprint 2: Conexión entre Fases e Integración Jugable

### 🧑‍💻 Persona 1 (Gameplay & Systems)
- [ ] Implementar la máquina de estados del juego (`GameLoopManager.cs`) que transfiere datos entre Fase 1 $\rightarrow$ Fase 2 $\rightarrow$ Fase 3 $\rightarrow$ Fase 4.
- [ ] Conectar la detección de los conos de visión (`VisionFan.cs`) con el nivel de alerta del contramaestre y marineros.

### 🎨 Persona 2 (Tech Art & Level Design)
- [ ] Ensamblar la escena de la cubierta principal con iluminación nocturna, focos poteros y niebla volumétrica.
- [ ] Implementar la técnica de *"barco estático, mar en movimiento"* para que los personajes caminen sobre cubierta sin sufrir inestabilidad física.

### 🎧 Persona 3 (UI/UX & Integration)
- [ ] Implementar la interfaz de la Fase 4 (`JuicioController.cs`): vinculación visual de pruebas con los 11 Indicadores de la OIT.
- [ ] Crear el sistema de inspección de documentos (zoom, rotación y lectura de bitácoras falsificadas).
- [ ] Primera sesión de QA / pruebas de regresión del prototipo integrado.
