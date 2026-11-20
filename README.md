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
  <img src="https://img.shields.io/badge/Render%20Pipeline-URP-00d4aa?style=for-the-badge" alt="URP" />
  <img src="https://img.shields.io/badge/Version%20Control-Git%20LFS-orange?style=for-the-badge&logo=gitlfs" alt="Git LFS" />
  <img src="https://img.shields.io/badge/Licencias-CC0%20%2F%20CC%20BY-green?style=for-the-badge" alt="CC0 / CC BY" />
</p>

---

## 👥 Equipo de Desarrollo y Asignación de Roles (Semanas 10–15)

| Código | Integrante | Cuenta GitHub | Rol Principal | Rama Oficial en GitHub | Etiqueta Trello |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **AB** | **Benites Fierro Alexander Aiken** | [`@Alexander5642`](https://github.com/Alexander5642) | **Dirección de Diseño, UI/UX, Narrativa y Gestión de Escenas** | [`feature/alexander-ui-narrativa`](https://github.com/Efustion23/Kairos_Videojuego/tree/feature/alexander-ui-narrativa) | 🟦 Azul Cian (`Diseño y UI`) |
| **DT** | **Tornero Bermudez Diego Ali Gadiel** | [`@Efustion23`](https://github.com/Efustion23) | **Programación Principal en Unity, Mecánicas, Sonar e IA** | [`feature/diego-gameplay-ia`](https://github.com/Efustion23/Kairos_Videojuego/tree/feature/diego-gameplay-ia) | 🟧 Ámbar/Naranja (`Programación Unity`) |
| **CV** | **Veliz Cochachi Christopher Jesus** | [`@christopher-26`](https://github.com/christopher-26) | **Dirección de Arte 3D, Entorno Marítimo, Iluminación y Audio** | [`feature/christopher-arte-audio`](https://github.com/Efustion23/Kairos_Videojuego/tree/feature/christopher-arte-audio) | 🟩 Verde Esmeralda (`Arte 3D y Audio`) |

👉 **Consulta la guía completa de trabajo y protocolos en:** [`docs/ROLES_Y_GUIA_DE_TRABAJO.md`](docs/ROLES_Y_GUIA_DE_TRABAJO.md)  
👉 **Consulta el desglose de las 18 tarjetas del Tablero de Trello en:** [`docs/PLAN_DE_TAREAS_SPRINTS.md`](docs/PLAN_DE_TAREAS_SPRINTS.md)

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

### 🌍 Los 3 sectores

| Sector | Pesquería | Buque objetivo | Escena de la Fase 3 |
| :--- | :--- | :--- | :--- |
| 1. Atlántico Sur (milla 201) | Poteros | Hai Feng 77 | `03_Infiltracion` |
| 2. Golfo de Guinea | Arrastreros | Kestrel Explorer | `03_Infiltracion_S2` |
| 3. Océano Índico Occidental | Palangreros y frigoríficos | Oceanic Crown 9 | `03_Infiltracion_S3` |

Los tres barcos comparten el esqueleto interior (pasillos, celdas, habilitación y puente) y cambian el aparejo de cubierta, las rondas, las celdas, el sitio de las pruebas y la tripulación.

---

## 🌿 Estructura de Ramas en GitHub

* **`main`**: Rama para entregas finales y versiones evaluadas estables.
* **`develop`**: Rama base de integración diaria.
* **Ramas activas de cada integrante:**
  * [`feature/alexander-ui-narrativa`](https://github.com/Efustion23/Kairos_Videojuego/tree/feature/alexander-ui-narrativa): UI/UX, HUDs, Diálogos y Maletín de Pruebas.
  * [`feature/diego-gameplay-ia`](https://github.com/Efustion23/Kairos_Videojuego/tree/feature/diego-gameplay-ia): Movimiento, Físicas, Sonar e Inteligencia Artificial.
  * [`feature/christopher-arte-audio`](https://github.com/Efustion23/Kairos_Videojuego/tree/feature/christopher-arte-audio): Arte 3D Modular, Shaders, Iluminación y Audio.

👉 **Revisa la guía de commits y Pull Requests en:** [`docs/CONVENCION_DE_COMMITS_Y_RAMAS.md`](docs/CONVENCION_DE_COMMITS_Y_RAMAS.md)

---

## 📁 Estructura del Repositorio

```text
Kairos_Videojuego/
├── Assets/
│   ├── Kairos/
│   │   ├── Arte y audio/     # Módulo de arte y audio: audio espacial, mixer, lluvia, niebla y sus pruebas
│   │   ├── Generated/        # Mallas y materiales que crea el generador (no se editan a mano)
│   │   ├── Models/           # Personajes, props CC0 (Props/) y barcos .glb (Vehicles/)
│   │   ├── Prefabs/          # Barcos de la Fase 3, lancha y props (salen del generador)
│   │   ├── Resources/        # Música, efectos (Sfx/), barcos de flota (Ships/), UI y materiales
│   │   ├── Scenes/           # Las 7 escenas del juego (salen del generador)
│   │   ├── Scripts/          # Código fuente en C#
│   │   │   ├── Core/         # Partida, datos del caso, sonido, música, UI y ambientación por sector
│   │   │   ├── Editor/       # Generador del juego (menú Kairos > Generar juego)
│   │   │   ├── Menu/         # Base KAIROS: misión, mejoras, opciones y códice
│   │   │   ├── Phase1/       # Rastreo satelital
│   │   │   ├── Phase2/       # Sonar, hidrófono y dron térmico
│   │   │   ├── Phase3/       # Infiltración, jugador, IA de guardias y diálogos
│   │   │   └── Phase4/       # Juicio, grafo financiero, repatriación y epílogo
│   │   ├── Shaders/          # Shaders de océano, cielo nocturno y efectos térmicos
│   │   ├── Tests/            # Pruebas EditMode de las reglas del caso y del rastreo
│   │   └── LICENCIAS.txt     # Autores y licencias de todos los recursos de terceros
│   └── Settings/             # Configuración de URP y post-procesado
├── docs/                     # Documentación técnica y diseño
│   ├── images/               # Capturas de pantalla y concept art
│   ├── GDD_RESUMEN_EJECUTIVO.md
│   ├── ROLES_Y_GUIA_DE_TRABAJO.md
│   ├── CONVENCION_DE_COMMITS_Y_RAMAS.md
│   ├── PLAN_DE_TAREAS_SPRINTS.md
│   ├── GUION_TRAILER_1080P.md
│   └── PRESENTACION_KAIROS.html
├── KAIROS_GDD_v2.docx        # Documento de diseño completo
├── .gitattributes            # Configuración de Git LFS y UnityYamlMerge
├── .gitignore                # Ignorado inteligente de Library, Temp e IDEs
└── README.md
```

---

## 🎮 Descargar y jugar

Todavía no hay una versión publicada. Cuando salga la 1.0 estará en [Releases](https://github.com/Efustion23/Kairos_Videojuego/releases): se descarga el `.zip`, se descomprime y se abre `KAIROS.exe`, sin instalación. Mientras tanto, el juego se abre desde Unity siguiendo la guía de abajo. Pulsa **F3** en cualquier momento para ver los FPS.

---

## 🚀 Guía de Inicio Rápido (Setup para el Equipo)

1. **Clonar con Git LFS:**
   ```bash
   git clone https://github.com/Efustion23/Kairos_Videojuego.git
   cd Kairos_Videojuego
   git lfs install
   git lfs pull
   ```
2. **Cambiar a tu rama de trabajo:**
   * **Alexander:** `git checkout feature/alexander-ui-narrativa`
   * **Diego:** `git checkout feature/diego-gameplay-ia`
   * **Christopher:** `git checkout feature/christopher-arte-audio`
3. **Abrir en Unity Hub:**
   * Seleccionar la versión `Unity 6 LTS (6000.3.x)`.
4. **Generar el juego:** menú **`Kairos > Generar juego`** (fuera de Play Mode).
   * ⚠️ **Las escenas, los barcos y la lancha salen del generador: no se editan a mano.** Lo que se cambie a mano en una escena se pierde la próxima vez que alguien genere. Los cambios van en `Assets/Kairos/Scripts/Editor/KairosBuilder*.cs`.
5. **Probar:** `Window > General > Test Runner > EditMode > Run All`.
