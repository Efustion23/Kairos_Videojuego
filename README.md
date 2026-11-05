# 🌊 KAIROS: Sombras en Altamar

<p align="center">
  <img src="docs/images/protagonista.jpeg" alt="KAIROS Protagonista" width="600" style="border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.4);" />
</p>

<p align="center">
  <b>Aventura de investigación 3D, navegación táctica, inspección logística y sigilo de contrainteligencia.</b><br>
  <i>"En el mar abierto, la ley no llega; la verdad es la única arma."</i>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3%20LTS-black?style=for-the-badge&logo=unity" alt="Unity 6 LTS" />
  <img src="https://img.shields.io/badge/C%23-12.0-blue?style=for-the-badge&logo=csharp" alt="C# 12" />
  <img src="https://img.shields.io/badge/Render%20Pipeline-HDRP%20%2F%20URP-00d4aa?style=for-the-badge" alt="HDRP / URP" />
  <img src="https://img.shields.io/badge/Version%20Control-Git%20LFS-orange?style=for-the-badge&logo=gitlfs" alt="Git LFS" />
  <img src="https://img.shields.io/badge/Licencias-CC0%20Assets-green?style=for-the-badge" alt="CC0 Assets" />
</p>

---

## 📖 Acerca del Juego

**KAIROS: Sombras en Altamar** es un videojuego de investigación y sigilo no violento inspirado en hechos reales, donde encarnas a un **Inspector Civil de Derechos Humanos y Pesca Marítima**. Tu misión es monitorear los océanos internacionales, detectar buques de flotas oscuras dedicados a la pesca ilegal (INDNR) y la trata laboral, abordar discretamente en medio de tormentas nocturnas, asegurar pruebas con sellado digital criptográfico y llevar a las corporaciones trasnacionales ante los tribunales marítimos.

### 🎯 Los 3 Pilares Fundamentales
1. **La Verdad es el Arma:** No existen armas de fuego ni combate violento. El progreso se logra recopilando evidencias físicas, testimonios de tripulantes y verificando los 11 Indicadores de Trabajo Forzado de la OIT.
2. **Capa sobre Capa:** Cada fase del bucle añade un nuevo sentido instrumental: satélites orbitales, sonar acústico volumétrico, vista térmica, ojos en cubierta y grafos judiciales.
3. **El Océano como Antagonista:** El mar no es un decorado; el estado de la marea (Escala Douglas 0 a 9) determina el cabeceo del buque, las ventanas de abordaje, la visibilidad y el comportamiento de la tripulación.

---

## 🔄 El Bucle de Juego (Las 4 Fases)

```mermaid
flowchart LR
    F1["🛰️ FASE 1: MACRO<br/>Rastreo Satelital & AIS"] -->|Índice de Riesgo >= 60| F2["📡 FASE 2: MID<br/>Sonar Acústico & Vóxeles"]
    F2 -->|Confianza >= 70%| F3["🔦 FASE 3: MICRO<br/>Infiltración y Pruebas SHA-256"]
    F3 -->|Evidencia Sellada| F4["⚖️ FASE 4: META<br/>Juicio y Red Financiera"]
    F4 -->|Fondos de Decomiso| F1
```

1. **Fase 1 — Rastreo Satelital (Macro):** Operación en el globo 3D del Centro de Control. Cruce de datos AIS, radar SAR, luces nocturnas (VIIRS) y cálculo del Índice de Riesgo ($IR$).
2. **Fase 2 — Sonar y Termografía (Mid):** Aproximación táctica en lancha/dron. Pulso acústico sincronizado con el oleaje, reconstrucción volumétrica de vóxeles (0.5 m) del casco y cálculo del Índice de Habitabilidad ($H$).
3. **Fase 3 — Infiltración y Documentación (Micro):** Abordaje nocturno con técnica de *"barco estático, mar en movimiento"*. Sigilo social y ambiental, registro fotográfico en el maletín estanco con hash SHA-256.
4. **Fase 4 — Juicio y Sala de Situación (Meta):** Construcción del grafo de causalidad penal contra empresas fantasma y la cúpula de *Tidewater Holdings*. Sentencias PSMA, Órdenes de Retención (WRO) y retorno presupuestario.

---

## 👥 Organización del Equipo (3 Desarrolladores)

Para garantizar un flujo de trabajo sin roces ni conflictos de escenas en Unity, el equipo se organiza bajo las siguientes disciplinas:

| Rol | Responsable | Módulos Principales |
| :--- | :--- | :--- |
| **Lead Gameplay & Systems Programmer** | **Persona 1** | Fase 1 (Satelital), Fase 3 (Controlador de Infiltración, IA de guardia), Criptografía SHA-256, Unit Tests. |
| **Technical Artist & Level Designer** | **Persona 2** | Fase 2 (Sonar/Vóxeles/Termografía), Escenarios 3D y modulares, Shaders de Océano y Clima, Iluminación y Post-Processing. |
| **UI/UX, Audio, Narrative & QA** | **Persona 3** | Interfaces de las 4 fases (Canvas/TMP), Fase 4 (Grafo Judicial), Diseño de audio FMOD, Códice de la OIT y revisión de PRs. |

👉 **Consulta la guía completa de trabajo en:** [`docs/ROLES_Y_GUIA_DE_TRABAJO.md`](docs/ROLES_Y_GUIA_DE_TRABAJO.md)

---

## 🌿 Convención de Ramas y Commits

### Estructura de Ramas
* **`main`**: Versiones estables y entregables finales. Protegida contra commits directos.
* **`develop`**: Rama principal de desarrollo e integración continua.
* **Ramas de trabajo:**
  * `feature/<nombre>`: Nuevas mecánicas o código (`feature/fase1-calculo-ir`).
  * `art/<nombre>`: Modelos 3D, escenarios o shaders (`art/buque-potero-modular`).
  * `ui/<nombre>`: Interfaces y menús (`ui/hud-infiltracion`).
  * `audio/<nombre>`: Efectos y ambientación sonora (`audio/tormenta-olas`).
  * `fix/<nombre>`: Corrección de bugs (`fix/vision-guardia-error`).

### Formato de Commits (Conventional Commits)
```bash
feat(fase1): agregar detector de anomalia por apagado de ais
art(shaders): optimizar calculo de espuma en ocean shader
fix(fase3): corregir collider de mamparo en cubierta B
```

👉 **Revisa todos los lineamientos en:** [`docs/CONVENCION_DE_COMMITS_Y_RAMAS.md`](docs/CONVENCION_DE_COMMITS_Y_RAMAS.md)

---

## 📁 Estructura del Repositorio

```text
Kairos_Videojuego/
├── Assets/
│   ├── Kairos/
│   │   ├── Generated/        # Mallas y assets procedurales
│   │   ├── Models/           # Modelos 3D (Props CC0 Poly Haven, barcos, protagonista)
│   │   ├── Prefabs/          # Prefabs modulares de salas, props y actores
│   │   ├── Resources/        # Audio CC0, datos y fuentes tipográficas
│   │   ├── Scenes/           # Escenas de Unity (Fases y sandboxes individuales)
│   │   ├── Scripts/          # Código fuente en C# dividido por fases
│   │   │   ├── Common/       # Singletons, utilidades e interfaces
│   │   │   ├── Phase1/       # Macro-gameplay y rastreo satelital
│   │   │   ├── Phase2/       # Sonar, vóxeles y termografía
│   │   │   ├── Phase3/       # Infiltración, player agent, IA y maletín
│   │   │   └── Phase4/       # Juicio, grafo financiero y sentencias
│   │   ├── Shaders/          # Shaders de océano, cielo nocturno y efectos térmicos
│   │   └── Tests/            # Pruebas unitarias EditMode y PlayMode
│   └── Settings/             # Configuraciones URP/HDRP y Post-Processing
├── docs/                     # Documentación técnica y diseño
│   ├── images/               # Capturas de pantalla y concept art
│   ├── GDD_RESUMEN_EJECUTIVO.md
│   ├── ROLES_Y_GUIA_DE_TRABAJO.md
│   ├── CONVENCION_DE_COMMITS_Y_RAMAS.md
│   └── PLAN_DE_TAREAS_SPRINTS.md
├── .gitattributes            # Configuración de Git LFS y UnityYamlMerge
├── .gitignore                # Ignorado inteligente de Library, Temp e IDEs
└── README.md
```

---

## 🚀 Guía de Inicio Rápido (Setup para el Equipo)

1. **Instalar Prerrequisitos:**
   * **Unity 6 LTS** (versión recomendada: `6000.3.x`).
   * **Git** y **Git LFS** ([Descargar Git LFS](https://git-lfs.github.com/)).
2. **Clonar el Repositorio con LFS:**
   ```bash
   git clone <URL_DEL_REPOSITORIO>
   cd Kairos_Videojuego
   git lfs install
   git lfs pull
   ```
3. **Abrir en Unity Hub:**
   * Abre Unity Hub, haz clic en **Add** y selecciona la carpeta `Kairos_Videojuego`.
   * Permite que Unity compile la biblioteca inicial de `Library/` (puede tardar unos minutos en la primera apertura).
4. **Crear tu Rama de Trabajo:**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/mi-primera-tarea
   ```

---

## 📚 Documentación Adicional

* 📘 [Resumen Ejecutivo del GDD y Fórmulas](docs/GDD_RESUMEN_EJECUTIVO.md)
* 👥 [Roles y Guía de Trabajo en Equipo](docs/ROLES_Y_GUIA_DE_TRABAJO.md)
* 🌿 [Convención de Commits y Ramas](docs/CONVENCION_DE_COMMITS_Y_RAMAS.md)
* 📋 [Plan de Tareas y Sprints](docs/PLAN_DE_TAREAS_SPRINTS.md)
