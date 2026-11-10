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
  <img src="https://img.shields.io/badge/Lenguaje-C%23-blue?style=for-the-badge&logo=csharp" alt="C#" />
  <img src="https://img.shields.io/badge/Render%20Pipeline-URP-00d4aa?style=for-the-badge" alt="URP" />
  <img src="https://img.shields.io/badge/Version%20Control-Git%20LFS-orange?style=for-the-badge&logo=gitlfs" alt="Git LFS" />
  <img src="https://img.shields.io/badge/Licencias-CC0%20Assets-green?style=for-the-badge" alt="CC0 Assets" />
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

**KAIROS: Sombras en Altamar** es un videojuego de investigación y sigilo no violento inspirado en hechos reales, donde encarnas a un **inspector o inspectora de la unidad KAIROS**. Tu misión es rastrear un buque de pesca ilegal, descubrir a las personas retenidas en su casco, abordarlo de noche sin armas, sellar pruebas con hash criptográfico y seguir el dinero hasta su dueño real ante un tribunal. Esta versión incluye un caso completo, el del *Hai Feng 77*.

### 🎯 Los 4 Pilares Fundamentales
1. **La Verdad es el Arma:** No existen armas de fuego ni combate violento. El progreso se logra recopilando evidencias físicas, testimonios de tripulantes y verificando los 11 Indicadores de Trabajo Forzado de la OIT.
2. **Ver lo Invisible:** Cada fase añade un sentido: satélite, sonar y termografía, ojo humano y rastro financiero.
3. **El Océano como Antagonista Neutral:** El estado del mar (escala Douglas de 0 a 9) degrada sensores y marca las ventanas del sonar y del abordaje.
4. **Dignidad antes que Espectáculo:** Las víctimas tienen nombre, voz y decisión propia; nunca son contadores de recursos.

---

## 🔄 El Bucle de Juego (Las 4 Fases)

```mermaid
flowchart LR
    F1["🛰️ FASE 1: MACRO<br/>Rastreo Satelital & AIS"] -->|Índice de Riesgo >= 60| F2["📡 FASE 2: MID<br/>Sonar y Termografía"]
    F2 -->|Confianza >= 70%| F3["🔦 FASE 3: MICRO<br/>Infiltración y Pruebas SHA-256"]
    F3 -->|Al menos 1 prueba A| F4["⚖️ FASE 4: META<br/>Juicio y Red Financiera"]
    F4 -->|Sentencia| R["🏠 Repatriación<br/>y epílogo"]
```

1. **Fase 1 — Rastreo Satelital (Macro):** Operación en el globo 3D del Centro de Control. Cruce de datos AIS, radar SAR, luces nocturnas (VIIRS) y cálculo del Índice de Riesgo ($IR$).
2. **Fase 2 — Sonar y Termografía (Mid):** Aproximación táctica en lancha/dron. Pulso acústico sincronizado con el oleaje, lecturas de cinco compartimentos y Confianza de 70 % para abordar.
3. **Fase 3 — Infiltración y Documentación (Micro):** Abordaje nocturno con técnica de *"barco estático, mar en movimiento"*. Sigilo sin game over, personas que deciden por sí mismas y fotos selladas con hash SHA-256 en un maletín de 13 pruebas.
4. **Fase 4 — Juicio y Sala de Situación (Meta):** Grafo de pruebas desde el buque hasta Tidewater Austral, en 14 días. Orden de Retención, Congelación o Condena según los indicadores de la OIT, y 15 % del embargo para el Fondo de Decomiso.

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
│   │   ├── Generated/        # Mallas, materiales y texturas que crea el generador
│   │   ├── Models/           # Protagonista, sus animaciones y objetos CC0 de Poly Haven
│   │   ├── Prefabs/          # Buque, lancha y objetos (los crea el generador)
│   │   ├── Resources/        # Música CC0 y material base
│   │   ├── Scenes/           # Menú y 4 fases (las crea el generador; no editar a mano)
│   │   ├── Scripts/          # Código fuente en C#
│   │   │   ├── Core/         # Datos del caso, partida, mar, audio e interfaz
│   │   │   ├── Editor/       # Generador: menú Kairos > Generar juego
│   │   │   ├── Menu/         # Título, base, ajustes y créditos
│   │   │   ├── Phase1/       # Macro-gameplay y rastreo satelital
│   │   │   ├── Phase2/       # Sonar, dron e hidrófono
│   │   │   ├── Phase3/       # Infiltración, player agent, IA y maletín
│   │   │   └── Phase4/       # Juicio, grafo financiero y sentencias
│   │   ├── Shaders/          # Shaders de océano, cielo nocturno y efectos térmicos
│   │   └── Tests/            # Pruebas de editor (EditMode)
│   └── Settings/             # Configuración de URP
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
   * Usar la versión exacta de `ProjectSettings/ProjectVersion.txt`. Todo el equipo debe tener la misma.
4. **Antes de editar:**
   * Las escenas y `HaiFeng77.prefab` salen del generador, `Kairos > Generar juego`. Lee las reglas en [`docs/ROLES_Y_GUIA_DE_TRABAJO.md`](docs/ROLES_Y_GUIA_DE_TRABAJO.md).
