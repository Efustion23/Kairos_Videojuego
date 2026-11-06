# 📋 Tablero de Tareas y Plan de Producción (Semanas 10–15)

> **KAIROS: Sombras en Altamar — Desarrollo Unity 6**  
> *Estructura idéntica al Tablero de Trello para seguimiento académico y control del equipo.*

---

## 📌 Configuración del Tablero en Trello

* **Nombre del Tablero:** `KAIROS: Sombras en Altamar — Desarrollo Unity 6 (Semanas 10–15)`
* **Visibilidad:** Espacio de Trabajo / Público
* **Miembros:**
  * **AB** — Benites Fierro Alexander Aiken (Dirección de Diseño, UI/UX, Narrativa y Gestión de Escenas)
  * **DT** — Tornero Bermudez Diego Ali Gadiel (Programación Principal en Unity, Mecánicas, Sonar e IA)
  * **CV** — Veliz Cochachi Christopher Jesus (Dirección de Arte 3D, Entorno Marítimo, Iluminación y Audio)
* **Etiquetas (Labels):**
  * 🟦 **Azul Cian:** `Diseño y UI · feature/alexander-ui-narrativa`
  * 🟧 **Ámbar / Naranja:** `Programación Unity · feature/diego-gameplay-ia`
  * 🟩 **Verde Esmeralda:** `Arte 3D y Audio · feature/christopher-arte-audio`
  * ⬛ **Gris Oscuro (Semanas):** `Semana 10`, `Semana 11`, `Semana 12`, `Semana 13`, `Semana 14`, `Semana 15`

---

## 📂 LISTA 1: Completado (Semanas 10–11) — 6 Tarjetas (100% Finalizadas)

### 1. `[S10-1]` Diseño del nivel del buque «Hai Feng 77» y bocetos de interfaz
* **Miembro:** Alexander Benites (AB)
* **Etiquetas:** `Semana 10` + `Diseño y UI · feature/alexander-ui-narrativa`
* **Horario:** Lun y Mié · 18:00 – 21:00
* **Rama:** `feature/alexander-ui-narrativa`
* **Commit:** `docs(level): definir plano de cubierta y puntos de rescate del buque`
* **Descripción:** Definir planos de cubierta, pasillos interiores, ubicación de las 5 pruebas clave y diseño visual del radar en pantalla.
* **Checklist (Subtareas):**
  - [x] Trazar mapa 2D de cubierta principal y puente de mando *(Completado por Alexander Benites)*
  - [x] Definir ubicación de las 5 evidencias documentales clave *(Completado por Alexander Benites)*
  - [x] Diseñar wireframe del HUD táctico y radar en Figma *(Completado por Alexander Benites)*

### 2. `[S10-2]` Configuración del repositorio Unity y movimiento del personaje
* **Miembro:** Diego Tornero (DT)
* **Etiquetas:** `Semana 10` + `Programación Unity · feature/diego-gameplay-ia`
* **Horario:** Mar y Jue · 18:00 – 21:00
* **Rama:** `feature/diego-gameplay-ia`
* **Commit:** `feat(player): implementar movimiento en tercera persona y modo sigilo`
* **Descripción:** Crear proyecto en Unity, configurar cámara en tercera persona, desplazamiento sigiloso agachado y subida por escaleras.
* **Checklist (Subtareas):**
  - [x] Inicializar repositorio Git con .gitignore de Unity y Git LFS *(Completado por Diego Tornero)*
  - [x] Programar CharacterController en tercera persona y cámara orbital *(Completado por Diego Tornero)*
  - [x] Implementar postura agachado de sigilo y detección de escaleras *(Completado por Diego Tornero)*

### 3. `[S10-3]` Bloqueo 3D del barco pesquero y simulación inicial del mar
* **Miembro:** Christopher Veliz (CV)
* **Etiquetas:** `Semana 10` + `Arte 3D y Audio · feature/christopher-arte-audio`
* **Horario:** Lun y Jue · 18:00 – 21:00
* **Rama:** `feature/christopher-arte-audio`
* **Commit:** `art(ship): importar estructura 3D modular del barco y superficie del mar`
* **Descripción:** Armar la estructura modular del casco, cubierta principal, puente de mando y configurar el oleaje nocturno.
* **Checklist (Subtareas):**
  - [x] Modelar greybox modular del casco y superestructura del pesquero *(Completado por Christopher Veliz)*
  - [x] Configurar plano oceánico con shader de olas nocturnas *(Completado por Christopher Veliz)*
  - [x] Exportar prefabs con colisionadores optimizados a Unity *(Completado por Christopher Veliz)*

### 4. `[S11-1]` Interfaz del mapa satelital (Fase 1) y marcadores de alerta
* **Miembro:** Alexander Benites (AB)
* **Etiquetas:** `Semana 11` + `Diseño y UI · feature/alexander-ui-narrativa`
* **Horario:** Lun y Vie · 18:00 – 21:00
* **Rama:** `feature/alexander-ui-narrativa`
* **Commit:** `ui(satellite): crear panel interactivo de rastreo satelital de Fase 1`
* **Descripción:** Construir la pantalla de rastreo satelital con botones para encender capas de radar y seleccionar el barco sospechoso.
* **Checklist (Subtareas):**
  - [x] Maquetar panel de capas AIS, VIIRS y radar SAR en Canvas *(Completado por Alexander Benites)*
  - [x] Diseñar tarjeta emergente de Índice de Riesgo (IR) del buque *(Completado por Alexander Benites)*
  - [x] Conectar botón de despliegue táctico hacia la Fase 2 *(Completado por Alexander Benites)*

### 5. `[S11-2]` Conducción de la lancha de KAIROS y física de flotación
* **Miembro:** Diego Tornero (DT)
* **Etiquetas:** `Semana 11` + `Programación Unity · feature/diego-gameplay-ia`
* **Horario:** Mar y Vie · 18:00 – 21:00
* **Rama:** `feature/diego-gameplay-ia`
* **Commit:** `feat(boat): agregar control de lancha y detección de zona de abordaje`
* **Descripción:** Programar la navegación de la lancha alrededor del buque pesquero y el indicador de ventana de abordaje en la popa.
* **Checklist (Subtareas):**
  - [x] Programar propulsión, giro y flotabilidad de la lancha RHIB *(Completado por Diego Tornero)*
  - [x] Sincronizar ventana de abordaje con la cresta de la ola en popa *(Completado por Diego Tornero)*
  - [x] Integrar transición de cámara al subir por la escala del casco *(Completado por Diego Tornero)*

### 6. `[S11-3]` Texturas de metal oxidado e iluminación nocturna de cubierta
* **Miembro:** Christopher Veliz (CV)
* **Etiquetas:** `Semana 11` + `Arte 3D y Audio · feature/christopher-arte-audio`
* **Horario:** Mié y Jue · 18:00 – 21:00
* **Rama:** `feature/christopher-arte-audio`
* **Commit:** `art(lighting): configurar luces de cubierta, niebla y texturas de óxido`
* **Descripción:** Aplicar materiales de acero corroído por la sal, colocar los reflectores amarillos de peligro y la niebla marina.
* **Checklist (Subtareas):**
  - [x] Texturizar el casco con materiales PBR de óxido ferroso y salitre *(Completado por Christopher Veliz)*
  - [x] Posicionar focos halógenos amarillos en cubierta principal *(Completado por Christopher Veliz)*
  - [x] Ajustar niebla volumétrica nocturna y partículas de lluvia *(Completado por Christopher Veliz)*

---

## 📂 LISTA 2: En Revisión / PR (Semana 12) — 3 Tarjetas (2/3 Subtareas)

### 7. `[S12-1]` Diseño del visor de sonar, brújula táctica y barra de sospecha
* **Miembro:** Alexander Benites (AB)
* **Etiquetas:** `Semana 12` + `Diseño y UI · feature/alexander-ui-narrativa`
* **Horario:** Lun y Mié · 18:00 – 21:00
* **Rama:** `feature/alexander-ui-narrativa`
* **Commit:** `ui(sonar): integrar visor de escaneo acústico y medidor de sospecha`
* **Checklist (Subtareas):**
  - [x] Diseñar retícula cian de sintonización de frecuencia de sonar *(Completado por Alexander Benites)*
  - [x] Implementar medidor dinámico de sospecha del barco (0 a 100) *(Completado por Alexander Benites)*
  - [ ] Validar legibilidad de iconos en Pull Request con el equipo

### 8. `[S12-2]` Mecánica de pulso de sonar y detección de compartimentos ocultos
* **Miembro:** Diego Tornero (DT)
* **Etiquetas:** `Semana 12` + `Programación Unity · feature/diego-gameplay-ia`
* **Horario:** Mar y Jue · 18:00 – 21:00
* **Rama:** `feature/diego-gameplay-ia`
* **Commit:** `feat(sonar): programar escaneo a través del casco y marcado de bodegas`
* **Checklist (Subtareas):**
  - [x] Programar emisor de pulso esférico y detección por capas (LayerMask) *(Completado por Diego Tornero)*
  - [x] Permitir fijar marcadores 3D sobre bodegas ocultas y motores *(Completado por Diego Tornero)*
  - [ ] Revisar fusión de rama en develop sin conflictos de escena

### 9. `[S12-3]` Efecto visual de onda cian, vista térmica y sonidos del sonar
* **Miembro:** Christopher Veliz (CV)
* **Etiquetas:** `Semana 12` + `Arte 3D y Audio · feature/christopher-arte-audio`
* **Horario:** Lun y Jue · 18:00 – 21:00
* **Rama:** `feature/christopher-arte-audio`
* **Commit:** `audio(sonar): añadir efecto visual de barrido cian y sonidos de eco`
* **Checklist (Subtareas):**
  - [x] Crear Shader Graph de barrido volumétrico cian (#3FE0D0) *(Completado por Christopher Veliz)*
  - [x] Configurar filtro de visión térmica fría/caliente para el dron *(Completado por Christopher Veliz)*
  - [ ] Integrar bancos de sonido de ping acústico y eco metálico

---

## 📂 LISTA 3: En Proceso (Semana 13) — 3 Tarjetas (1/3 Subtareas)

### 10. `[S13-1]` Guion de diálogos con los sobrevivientes y rutas de patrulla
* **Miembro:** Alexander Benites (AB)
* **Etiquetas:** `Semana 13` + `Diseño y UI · feature/alexander-ui-narrativa`
* **Horario:** Mié y Vie · 18:00 – 21:00
* **Rama:** `feature/alexander-ui-narrativa`
* **Commit:** `feat(dialogue): integrar textos de conversación y medidor de confianza`
* **Checklist (Subtareas):**
  - [x] Redactar árbol de diálogo con víctimas y opciones de asistencia *(Completado por Alexander Benites)*
  - [ ] Diseñar interfaz de nivel de confianza y consentimiento de testigo
  - [ ] Ajustar waypoints y tiempos de parada de los guardias en cubierta

### 11. `[S13-2]` Inteligencia Artificial de guardias y conos de visión
* **Miembro:** Diego Tornero (DT)
* **Etiquetas:** `Semana 13` + `Programación Unity · feature/diego-gameplay-ia`
* **Horario:** Mar y Jue · 18:00 – 21:00
* **Rama:** `feature/diego-gameplay-ia`
* **Commit:** `feat(ai): implementar patrullaje de guardias y detección por ruido`
* **Checklist (Subtareas):**
  - [x] Configurar NavMeshAgent y máquina de estados (Patrulla, Sospecha, Alerta) *(Completado por Diego Tornero)*
  - [ ] Programar cono de visión dinámico afectado por lluvia y postura
  - [ ] Añadir reacción de guardias ante escora del barco (>15°) y ruido

### 12. `[S13-3]` Modelado de camarotes internos, personajes y pasos sobre metal
* **Miembro:** Christopher Veliz (CV)
* **Etiquetas:** `Semana 13` + `Arte 3D y Audio · feature/christopher-arte-audio`
* **Horario:** Lun y Mié · 18:00 – 21:00
* **Rama:** `feature/christopher-arte-audio`
* **Commit:** `art(interior): decorar puente de mando, bodegas y audio de tormenta`
* **Checklist (Subtareas):**
  - [x] Ambientar interior del puente de mando y camarote del contramaestre *(Completado por Christopher Veliz)*
  - [ ] Integrar animaciones de patrullaje con linterna para los guardias
  - [ ] Configurar audio espacial 3D de pasos metálicos y compresores

---

## 📂 LISTA 4: Por Hacer (Semanas 14–15) — 6 Tarjetas (Pendientes)

### 13. `[S14-1]` Interfaz del maletín de evidencias y tablero de resolución final
* **Miembro:** Alexander Benites (AB)
* **Etiquetas:** `Semana 14` + `Diseño y UI · feature/alexander-ui-narrativa`
* **Horario:** Lun y Vie · 18:00 – 21:00
* **Rama:** `feature/alexander-ui-narrativa`
* **Commit:** `ui(caseboard): diseñar maletín de pruebas y pantalla de victoria`
* **Checklist (Subtareas):**
  - [ ] Diseñar cuadrícula del maletín estanco con sello criptográfico
  - [ ] Construir pizarra de conexión de evidencias e indicadores OIT
  - [ ] Maquetar pantalla de sentencia judicial y repatriación digna

### 14. `[S14-2]` Cámara fotográfica de pruebas y transición completa de las 4 fases
* **Miembro:** Diego Tornero (DT)
* **Etiquetas:** `Semana 14` + `Programación Unity · feature/diego-gameplay-ia`
* **Horario:** Mar y Vie · 18:00 – 21:00
* **Rama:** `feature/diego-gameplay-ia`
* **Commit:** `feat(evidence): programar captura fotográfica de pistas y cierre de misión`
* **Checklist (Subtareas):**
  - [ ] Programar raycast de validación de encuadre y enfoque fotográfico
  - [ ] Implementar sistema de guardado de pruebas recolectadas entre escenas
  - [ ] Conectar bucle completo de las 4 fases y zona de extracción

### 15. `[S14-3]` Objetos coleccionables 3D (bitácora, radio, pasaportes) y música
* **Miembro:** Christopher Veliz (CV)
* **Etiquetas:** `Semana 14` + `Arte 3D y Audio · feature/christopher-arte-audio`
* **Horario:** Mié y Jue · 18:00 – 21:00
* **Rama:** `feature/christopher-arte-audio`
* **Commit:** `art(props): añadir modelos 3D de evidencias y música de infiltración`
* **Checklist (Subtareas):**
  - [ ] Modelar y texturizar bitácora real, pasaportes, radio y plotter GPS
  - [ ] Añadir shader sutil de resaltado para objetos inspeccionables
  - [ ] Integrar música adaptativa de tensión e infiltración nocturna

### 16. `[S15-1]` Pruebas de usabilidad, ajuste de textos y menú de pausa/inicio
* **Miembro:** Alexander Benites (AB)
* **Etiquetas:** `Semana 15` + `Diseño y UI · feature/alexander-ui-narrativa`
* **Horario:** Lun, Mié y Sáb · 18:00 – 21:00
* **Rama:** `feature/alexander-ui-narrativa`
* **Commit:** `ui(polish): pulir menú de inicio, créditos del equipo y textos guía`
* **Checklist (Subtareas):**
  - [ ] Pulir pantalla de título, selector de casos y créditos del equipo
  - [ ] Realizar sesión de playtesting de claridad de objetivos y HUD
  - [ ] Verificar coherencia final del GDD web y material de exposición

### 17. `[S15-2]` Corrección de bugs, optimización de rendimiento y Build Final
* **Miembro:** Diego Tornero (DT)
* **Etiquetas:** `Semana 15` + `Programación Unity · feature/diego-gameplay-ia`
* **Horario:** Mar, Jue y Sáb · 18:00 – 21:00
* **Rama:** `feature/diego-gameplay-ia`
* **Commit:** `fix(release): optimizar rendimiento a 60fps y generar versión final`
* **Checklist (Subtareas):**
  - [ ] Auditar tiempos de CPU/GPU con Unity Profiler para asegurar 60 FPS
  - [ ] Aprobar Pull Request final de develop hacia la rama main
  - [ ] Compilar y verificar el ejecutable final de Windows (.exe)

### 18. `[S15-3]` Pulido de iluminación nocturna, mezcla de audio y video tráiler
* **Miembro:** Christopher Veliz (CV)
* **Etiquetas:** `Semana 15` + `Arte 3D y Audio · feature/christopher-arte-audio`
* **Horario:** Lun, Jue y Sáb · 18:00 – 21:00
* **Rama:** `feature/christopher-arte-audio`
* **Commit:** `art(final): ajustar post-procesado nocturno y capturar video de inicio`
* **Checklist (Subtareas):**
  - [ ] Calibrar corrección de color y bloom en perfiles de Post-Processing
  - [ ] Nivelar mezcla maestra de audio (voces, sonar, tormenta y música)
  - [ ] Capturar secuencias cinemáticas en alta definición para la portada
