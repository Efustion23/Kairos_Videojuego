# GUION DE EXPOSICIÓN Y PITCH (3 MINUTOS) &bull; KAIROS: SOMBRAS EN ALTAMAR
**Curso:** Investigación Académica / Diseño de Videojuegos  
**Presentación Oficial:** [PRESENTACION_KAIROS_SLIDES.pdf](file:///D:/Kairos_Videojuego/docs/PRESENTACION_KAIROS_SLIDES.pdf) (12 Diapositivas)  
**Versión Web Interactiva:** [PRESENTACION_OFICIAL_SLIDES.html](file:///D:/Kairos_Videojuego/docs/PRESENTACION_OFICIAL_SLIDES.html)  

---

## ⏱️ DISTRIBUCIÓN GENERAL DEL TIEMPO (3:00 MINUTOS TOTALES)
* **0:00 - 1:00 min (Diapositivas 1 a 4):** **Alexander Benites** (Problemática, Concepto, Fusión Obra Dinn/MGS, Bucle Jugable 4 Fases y Maletín Forense con Indicadores OIT).
* **1:00 - 2:00 min (Diapositivas 5 a 7):** **Diego Tornero** (IA de Patrulla con Conos de Visión/Escucha, Sectores Oceánicos, Físicas Hidrodinámicas en Unity 6 y 99/99 Tests Unitarios).
* **2:00 - 3:00 min (Diapositivas 8 a 12):** **Christopher Veliz** (GDD y Rigor Jurídico AMERP, Gestión de Tareas y Commits en GitHub, Video Demo / Trailer, Enlaces y Conclusión).

---

## 🎙️ GUION EXACTO DIAPOSITIVA POR DIAPOSITIVA

### 🟦 BLOQUE 1: ALEXANDER BENITES (0:00 - 1:00 MIN)

#### 📄 DIAPOSITIVA 01: PORTADA & TÍTULO
* **En pantalla:** *KAIROS: Sombras en Altamar - ODS 14 y ODS 8 - Nombres de los 3 integrantes.*
* **Palabras del Expositor (Alexander):**
  > *"Buenas noches miembros del jurado. Hoy presentamos **KAIROS: Sombras en Altamar**, un videojuego en Unity 6 que fusiona la investigación forense y la infiltración táctica para abordar una de las mayores crisis invisibles de nuestros océanos: la pesca ilegal no declarada y el trabajo forzoso en aguas internacionales."*

#### 📄 DIAPOSITIVA 02: CONCEPTO DEL VIDEOJUEGO & REFERENCIAS
* **En pantalla:** *Imágenes de problemática global, Inspectora Elena Ruiz (DICAPI) y referentes: Return of the Obra Dinn + Metal Gear Solid.*
* **Palabras del Expositor (Alexander):**
  > *"Cada año, las flotas ilegales mueven más de 20 mil millones de dólares apagando sus transpondedores satelitales. En KAIROS encarnamos a la inspectora Elena Ruiz. Nuestro objetivo no es hundir buques ni combatir con armas letales, sino reunir pruebas documentales irrebatibles. Tomamos la meticulosidad deductiva de 'Return of the Obra Dinn' y la combinamos con la infiltración táctica en buques industriales inspirada en 'Metal Gear Solid'."*

#### 📄 DIAPOSITIVA 03: BUCLE JUGABLE (LAS 4 FASES TÁCTICAS)
* **En pantalla:** *Esquema visual de las 4 fases: 1. Satelital, 2. Sonar/Navegación, 3. Infiltración Forense, 4. Tribunal AMERP.*
* **Palabras del Expositor (Alexander):**
  > *"El bucle jugable se estructura en cuatro etapas progresivas: primero, la triangulación macro-satelital de naves sospechosas; segundo, la navegación marítima guiada por sonar acústico; tercero, el abordaje clandestino para recolectar bitácoras y rescatar pasaportes de marineros retenidos; y cuarto, el dictamen judicial bajo los estándares reales del Acuerdo de la FAO sobre Medidas del Estado Rector del Puerto."*

#### 📄 DIAPOSITIVA 04: MECÁNICAS & MALETÍN FORENSE DIEGÉTICO
* **En pantalla:** *Render del Maletín Forense 3D, esquema de controles (WASD, TAB, E, Visor FLIR).*
* **Palabras del Expositor (Alexander):**
  > *"Para garantizar una inmersión absoluta eliminamos el HUD artificial: toda la interfaz es diegética. El jugador manipula en 3D un Maletín Forense donde coteja contratos clonados y analiza pasaportes con indicadores de trabajo forzoso de la OIT. Un falso arresto arruina el operativo; solo el rigor legal asegura la sanción. Doy paso a Diego Tornero para los aspectos de ingeniería y sistemas."*

---

### 🟨 BLOQUE 2: DIEGO TORNERO (1:00 - 2:00 MIN)

#### 📄 DIAPOSITIVA 05: PATRONES DE AMENAZA & SISTEMA DE IA
* **En pantalla:** *Guardias de cubierta con linterna táctica, oficiales de puente y máquina de estados (Patrulla, Alerta, Alarma).*
* **Palabras del Expositor (Diego):**
  > *"Gracias Alexander. Para lograr que la infiltración a bordo fuera tensa y desafiante, programamos una Inteligencia Artificial mediante máquinas de estados finitos. Los guardias no tienen visión omnisciente: proyectan conos de visión dinámicos y cuentan con sensores de escucha. Si el jugador corre sobre chapa metálica mojada, los guardias acudirán a investigar la fuente sonora, obligando a estudiar rutas de patrulla y usar coberturas oscuras."*

#### 📄 DIAPOSITIVA 06: AMBIENTACIÓN & SECTORES OCEANOGRÁFICOS
* **En pantalla:** *Las 3 zonas: Atlántico Sur (Milla 201), Pacífico Ecuatorial (Galápagos) e Índico Neutral.*
* **Palabras del Expositor (Diego):**
  > *"Diseñamos tres biomas oceánicos distintos con identidades mecánicas únicas: el Atlántico Sur, caracterizado por tormentas polares y olas de hasta 6 metros según la escala Douglas donde la navegación es prácticamente a ciegas; el Pacífico Ecuatorial, una zona protegida colindante a Galápagos con alta densidad de barcos nodriza; y el Índico Neutral, con factorías flotantes de alta tecnología y candados biométricos."*

#### 📄 DIAPOSITIVA 07: INGENIERÍA EN UNITY 6 & RETOS TÉCNICOS
* **En pantalla:** *Métricas de depuración, físicas hidrodinámicas, 99/99 Tests EditMode en verde y 60 FPS estables.*
* **Palabras del Expositor (Diego):**
  > *"El principal desafío técnico fue la simulación hidrodinámica: calculamos fuerzas de sustentación multi-punto sobre el casco de la lancha para que responda con inercia y balanceo real al oleaje. Además, optimizamos el sonar mediante nubes de puntos por GPU. Para garantizar cero regresiones y máxima estabilidad, implementamos un conjunto de 99 pruebas automatizadas con el Unity Test Runner, logrando 99 de 99 tests en verde y un rendimiento estable de 60 cuadros por segundo en 1080p. Ahora, Christopher Veliz expondrá el arte, DevOps y la demostración."*

---

### 🟩 BLOQUE 3: CHRISTOPHER VELIZ (2:00 - 3:00 MIN)

#### 📄 DIAPOSITIVA 08: GAME DESIGN DOCUMENT (GDD) & METODOLOGÍA
* **En pantalla:** *Captura de menús tácticos, manual operativo y resumen del GDD.*
* **Palabras del Expositor (Christopher):**
  > *"Gracias Diego. Toda esta arquitectura se sustenta en un Game Design Document formal que articula la curva de dificultad con la investigación académica. KAIROS no busca satanizar a la tripulación extranjera, sino evidenciar cómo el crimen organizado y las corporaciones armadoras explotan vacíos jurídicos en altamar, convirtiendo el videojuego en una herramienta de divulgación ética alineada al ODS 14 de vida submarina y ODS 8 de trabajo digno."*

#### 📄 DIAPOSITIVA 09: GESTIÓN DE TAREAS, COMMITS & GITHUB
* **En pantalla:** *Estructura de ramas GitFlow, estándar Conventional Commits y trazabilidad de los últimos commits.*
* **Palabras del Expositor (Christopher):**
  > *"En el apartado de desarrollo colaborativo, aplicamos un flujo estricto de GitFlow en nuestro repositorio de GitHub. Segmentamos las responsabilidades en ramas específicas de UI-Narrativa, Gameplay-IA y Arte-Audio para evitar conflictos en binarios de Unity. Cada avance fue documentado con la convención estándar de Conventional Commits, manteniendo trazabilidad completa y una rama principal limpia de archivos basura."*

#### 📄 DIAPOSITIVA 10: VIDEO DEMO / TRAILER CINEMÁTICO 1080P
* **En pantalla:** *Espacio reservado con ícono de reproducción para video de YouTube y escaleta del trailer.*
* **Palabras del Expositor (Christopher):**
  > *"Para la presentación audiovisual, diseñamos el componente 'TrailerCinematicSequencer' dentro de Unity, permitiéndonos coreografiar y renderizar en 1080p la persecución naval con audio posicional 3D, el abordaje táctico bajo lluvia torrencial y la apertura de las bodegas refrigeradas donde se encuentran las evidencias clave de la infracción."*

#### 📄 DIAPOSITIVA 11: ENLACES OFICIALES & REPOSITORIOS
* **En pantalla:** *Enlaces a GitHub (Efustion23/Kairos_Videojuego), rama activa, versión de Unity 6 y documentación.*
* **Palabras del Expositor (Christopher):**
  > *"Ponemos a disposición del jurado el enlace completo a nuestro repositorio en GitHub, la documentación técnica del GDD y los ejecutables de prueba generados en el motor Unity 6. Cada línea de código, shader PBR y prueba de estrés está disponible de manera transparente."*

#### 📄 DIAPOSITIVA 12: CIERRE & PREGUNTAS DEL JURADO
* **En pantalla:** *Diapositiva final de agradecimiento con los 3 expositores y áreas de especialidad.*
* **Palabras del Expositor (Christopher):**
  > *"KAIROS demuestra cómo la ingeniería y el arte de los videojuegos pueden trascender el entretenimiento puro para convertirse en un instrumento pericial y de toma de conciencia internacional. Agradecemos su atención y quedamos a su entera disposición para responder las preguntas del jurado."*

---

## 💡 RECOMENDACIONES CLAVE PARA LA DEFENSA ANTE EL JURADO
1. **Seguridad y Tono:** Cada expositor debe hablar con tono formal y firme (propio de una sustentación de investigación aplicada).
2. **Si el jurado pregunta por las complicaciones técnicas:**
   * **Diego** debe mencionar: *"La principal complicación fue la física del oleaje con la física de la lancha rápida; se producían rebotes indeseados, por lo que tuvimos que calibrar amortiguadores angulares e implementar pruebas unitarias automatizadas para los puntos del sonar."*
   * **Christopher** debe mencionar: *"En el aspecto gráfico, el balance entre iluminación volumétrica bajo lluvia y rendimiento a 60 FPS nos exigió optimizar shaders de agua e instanciar mallas modulares para los tres sectores navales."*
   * **Alexander** debe mencionar: *"En UI, el reto fue diseñar un maletín forense 100% interactivo dentro del mundo sin romper la inmersión con pantallas 2D genéricas."*
