# 🎬 Guion Oficial y Storyboard del Tráiler (1080p · 60 FPS)

> **KAIROS: Sombras en Altamar — Unidad de Inspección Marítima**  
> *Duración estimada: 45–60 segundos · Sin violencia gráfica · Enfoque en investigación forense, sigilo y derechos humanos.*

---

## 1. Estructura de Tomas (Sincronizada con `TrailerCinematicSequencer.cs`)

| Toma | Duración | Escena / Cámara | Audio / Locución / Música | Texto en Pantalla (Subtítulo Diegético) |
| :---: | :---: | :--- | :--- | :--- |
| **Toma 1**<br>*El Horizonte* | `00:00 – 00:10`<br>(4.5 s en bucle) | **`01_Rastreo` / `00_Menu`**<br>Plano orbital del globo 3D en la milla 201 del Atlántico Sur; corte al buque *Hai Feng 77* entre la niebla nocturna (`(-45, 18, -40)`). | Pulso satelital grave + lluvia marina + tema *Title / Airy* (`-6 dB`). | *"Más allá de la milla 200, donde se apagan los radares, la verdad sigue dejando rastro."* |
| **Toma 2**<br>*Aproximación* | `00:10 – 00:22`<br>(4.0 s) | **`02_Sonar`**<br>Travelling rasante lateral siguiendo la estela de espuma (`BoatWakeFoam`) de la lancha RHIB y el barrido radial cian (`#3FE0D0`) sobre el casco (`(12, 2.5, -28)`). | Ping acústico chirp (`1220 Hz → 720 Hz`) + eco metálico submarino + radio VHF con pitido Roger. | *"Rastrea la flota fantasma. Escanea el casco entre la tormenta."* |
| **Toma 3**<br>*Infiltración* | `00:22 – 00:36`<br>(3.8 s) | **`03_Infiltracion`**<br>Enfoque a la escala de gato en popa bajo los reflectores halógenos con salitre (`(3.5, 4.0, -14)`). La inspectora avanza agachada evitando los conos de visión de los guardias con linterna. | Pasos metálicos 3D + compresores frigoríficos + transición a *Pulse* en música adaptativa. | *"Infiltración sin armas: cada fotografía sellada con SHA-256 es una prueba ante la ley."* |
| **Toma 4**<br>*Verdad y Justicia* | `00:36 – 00:50`<br>(4.5 s) | **`03_Infiltracion` → `04_Juicio`**<br>Plano interior del puente de mando (`(-1.8, 2.2, 2.8)`) enfocando la bitácora, pasaportes, radio VHF y plotter GPS con resaltado forense; corte al grafo financiero conectando hasta *Tidewater Austral*. | Obturador de cámara forense + sello criptográfico + tema *Victory*. | *"11 indicadores OIT. 4 sobrevivientes con voz propia. Lleva la cadena de mando ante la justicia."* |
| **Cierre**<br>*Cartela Final* | `00:50 – 00:60` | **`00_Menu`**<br>Logotipo **KAIROS: SOMBRAS EN ALTAMAR** sobre el océano nocturno iluminado por la luna. | Acorde final + sonido de oleaje nocturno. | **KAIROS: SOMBRAS EN ALTAMAR**<br>*Alexander Benites · Diego Tornero · Christopher Veliz* |

---

## 2. Procedimiento de Grabación a 1080p

1. Abrir la escena `00_Menu` o `03_Infiltracion` en Unity 6 (`6000.3.24f1`) con resolución del Game View fijada en **Full HD (`1920 × 1080`)**.
2. El componente `TrailerCinematicSequencer` ya se encuentra integrado en la escena principal y configurado con las 4 tomas suaves interpoladas (`Vector3.Lerp` / `Quaternion.Slerp`).
3. Grabar en modo Maximized (sin bordes del editor) a 60 FPS estables verificados con el monitor `F3`.
