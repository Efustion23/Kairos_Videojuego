using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kairos.Pruebas
{
    [Serializable]
    public struct CinematicShot
    {
        public string shotName;
        public Vector3 cameraPosition;
        public Vector3 cameraRotationEuler;
        public float fieldOfView;
        public float duration;
        public string captionTitle;

        public CinematicShot(string name, Vector3 pos, Vector3 rot, float fov, float dur, string caption)
        {
            shotName = name;
            cameraPosition = pos;
            cameraRotationEuler = rot;
            fieldOfView = fov;
            duration = dur;
            captionTitle = caption;
        }
    }

    /// <summary>
    /// Secuenciador de tomas cinemáticas para tráiler, cinemática de introducción y menú principal (Semana 15 / S15-3).
    /// Controla la transición suave de cámara, interpolación de campo visual (FOV) y subtitulado diegético.
    /// </summary>
    public class TrailerCinematicSequencer : MonoBehaviour
    {
        [Header("Cámara Objetivo")]
        public Camera targetCamera;

        [Header("Secuencia de Tomas")]
        public List<CinematicShot> sequenceShots = new List<CinematicShot>();
        public int currentShotIndex = 0;
        public bool isPlaying = false;
        public bool loopSequence = true;

        [Header("Velocidad de Suavizado")]
        public float cameraMoveDamping = 2.5f;

        private float shotTimer = 0f;

        private void Awake()
        {
            if (sequenceShots == null || sequenceShots.Count == 0)
            {
                LoadDefaultTrailerShots();
            }
        }

        private void Update()
        {
            if (!isPlaying || sequenceShots.Count == 0 || targetCamera == null) return;

            shotTimer += Time.deltaTime;
            var currentShot = sequenceShots[currentShotIndex];

            // Interpolación de posición y rotación hacia la toma activa
            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                currentShot.cameraPosition,
                Time.deltaTime * cameraMoveDamping
            );

            Quaternion targetRot = Quaternion.Euler(currentShot.cameraRotationEuler);
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRot,
                Time.deltaTime * cameraMoveDamping
            );

            targetCamera.fieldOfView = Mathf.Lerp(
                targetCamera.fieldOfView,
                currentShot.fieldOfView,
                Time.deltaTime * cameraMoveDamping
            );

            // Avance a la siguiente toma al expirar el temporizador
            if (shotTimer >= currentShot.duration)
            {
                shotTimer = 0f;
                currentShotIndex++;
                if (currentShotIndex >= sequenceShots.Count)
                {
                    if (loopSequence)
                    {
                        currentShotIndex = 0;
                    }
                    else
                    {
                        isPlaying = false;
                    }
                }
            }
        }

        public void PlaySequence()
        {
            isPlaying = true;
            shotTimer = 0f;
            currentShotIndex = 0;
            if (sequenceShots.Count > 0 && targetCamera != null)
            {
                // Posicionamiento instantáneo en la primera toma
                targetCamera.transform.position = sequenceShots[0].cameraPosition;
                targetCamera.transform.rotation = Quaternion.Euler(sequenceShots[0].cameraRotationEuler);
                targetCamera.fieldOfView = sequenceShots[0].fieldOfView;
            }
        }

        public void StopSequence()
        {
            isPlaying = false;
        }

        public void LoadDefaultTrailerShots()
        {
            sequenceShots.Clear();

            // Toma 1: Plano general exterior del buque fantasma Hai Feng 77 entre la niebla nocturna
            sequenceShots.Add(new CinematicShot(
                "Toma1_BuqueEntreNiebla",
                new Vector3(-45f, 18f, -40f),
                new Vector3(15f, 45f, 0f),
                65f,
                4.5f,
                "KAIROS: Sombras en Altamar - Misión de Inspección"
            ));

            // Toma 2: Travelling rasante lateral siguiendo la estela de la lancha de asalto RHIB
            sequenceShots.Add(new CinematicShot(
                "Toma2_AproximacionLancha",
                new Vector3(12f, 2.5f, -28f),
                new Vector3(4f, -35f, 0f),
                50f,
                4.0f,
                "Aproximación encubierta en mar territorial"
            ));

            // Toma 3: Enfoque vertical a la escala de gato y conos de reflectores en popa
            sequenceShots.Add(new CinematicShot(
                "Toma3_EscalaAbordaje",
                new Vector3(3.5f, 4.0f, -14f),
                new Vector3(-25f, 180f, 0f),
                55f,
                3.8f,
                "Ventana de abordaje sincronizada con la cresta de la ola"
            ));

            // Toma 4: Plano táctico en el interior del puente de mando con el sonar y bitácora
            sequenceShots.Add(new CinematicShot(
                "Toma4_PuenteYEvidencias",
                new Vector3(-1.8f, 2.2f, 2.8f),
                new Vector3(18f, 135f, 0f),
                45f,
                4.5f,
                "Inspección forense de evidencias OIT y rescate"
            ));
        }

        /// <summary>
        /// Fábrica que construye el secuenciador y opcionalmente una cámara virtual cinemática.
        /// </summary>
        public static GameObject CreateCinematicSequencer(Camera cam = null, Transform parent = null)
        {
            var go = new GameObject("Secuenciador_Trailer_Cinematico");
            if (parent != null) go.transform.SetParent(parent, false);

            var seq = go.AddComponent<TrailerCinematicSequencer>();

            if (cam == null)
            {
                var camGo = new GameObject("Camara_Cinematica_Trailer");
                camGo.transform.SetParent(go.transform, false);
                cam = camGo.AddComponent<Camera>();
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
            }

            seq.targetCamera = cam;
            seq.LoadDefaultTrailerShots();

            return go;
        }
    }
}
