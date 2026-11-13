using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Tipos de evidencia forense y coleccionables del buque pesquero (Semana 14 / S14-3).
    /// </summary>
    public enum EvidenceType
    {
        BitacoraReal,
        PasaportesRetenidos,
        RadioVHFSatelital,
        PlotterGPSNautico
    }

    /// <summary>
    /// Componente interactivo adherido a cada evidencia coleccionable.
    /// Gestiona la detección por proximidad, el efecto de resplandor (outline glow) y el registro OIT.
    /// </summary>
    public class InspectablePropItem : MonoBehaviour
    {
        [Header("Datos de la Evidencia")]
        public EvidenceType evidenceType;
        public string evidenceTitle = "Evidencia Sin Título";
        public string oitViolationCode = "OIT C188 / Trabajo Marítimo";
        [TextArea(2, 4)]
        public string description = "";

        [Header("Configuración de Interacción")]
        public float inspectionDistance = 2.5f;
        public bool isHighlighted = true;
        public bool isCollected = false;
        public Color highlightGlowColor = new Color(0.18f, 0.96f, 1.0f, 1.0f); // Cian forense

        [Header("Renderers y Materiales")]
        public List<Renderer> targetRenderers = new List<Renderer>();

        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int ActiveGlowId = Shader.PropertyToID("_ActiveGlow");
        private static readonly int HighlightStrengthId = Shader.PropertyToID("_HighlightStrength");

        private void Start()
        {
            if (targetRenderers == null || targetRenderers.Count == 0)
            {
                GetComponentsInChildren(true, targetRenderers);
            }
            ApplyHighlightState();
        }

        private void Update()
        {
            if (isCollected) return;

            Transform playerOrCam = null;
            if (Camera.main != null)
            {
                playerOrCam = Camera.main.transform;
            }

            if (playerOrCam != null)
            {
                float dist = Vector3.Distance(transform.position, playerOrCam.position);
                bool shouldHighlight = dist <= inspectionDistance;
                if (shouldHighlight != isHighlighted)
                {
                    SetHighlight(shouldHighlight);
                }
            }
        }

        public void SetHighlight(bool active)
        {
            isHighlighted = active;
            ApplyHighlightState();
        }

        public void ApplyHighlightState()
        {
            float activeValue = (isHighlighted && !isCollected) ? 1.0f : 0.0f;
            for (int i = 0; i < targetRenderers.Count; i++)
            {
                if (targetRenderers[i] == null) continue;
                var mats = targetRenderers[i].materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;
                    if (mat.HasProperty(ActiveGlowId))
                    {
                        mat.SetFloat(ActiveGlowId, activeValue);
                    }
                    if (mat.HasProperty(GlowColorId))
                    {
                        mat.SetColor(GlowColorId, highlightGlowColor);
                    }
                }
            }
        }

        public bool CollectEvidence()
        {
            if (isCollected) return false;
            isCollected = true;
            SetHighlight(false);
            return true;
        }

        public string GetEvidenceSummary()
        {
            return $"[{oitViolationCode}] {evidenceTitle}: {description}";
        }
    }

    /// <summary>
    /// Fábrica y generador procedimental de modelos 3D y prefabs de evidencias coleccionables (Semana 14 / S14-3).
    /// </summary>
    public static class InspectableEvidenceProps
    {
        private const string ShaderName = "Kairos/Pruebas/InspectableOutlineGlow";

        public static Material CreateGlowMaterial(Color baseColor, Color glowColor, float highlightStrength = 1.6f)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var mat = new Material(shader)
            {
                name = $"Mat_Evidence_{baseColor.GetHashCode()}"
            };

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_GlowColor")) mat.SetColor("_GlowColor", glowColor);
            if (mat.HasProperty("_HighlightStrength")) mat.SetFloat("_HighlightStrength", highlightStrength);
            if (mat.HasProperty("_ActiveGlow")) mat.SetFloat("_ActiveGlow", 1.0f);

            return mat;
        }

        /// <summary>
        /// Genera el cuaderno de Bitácora Real con cubierta de cuero y hojas intercaladas.
        /// </summary>
        public static GameObject CreateBitacora(Vector3 position, Transform parent = null)
        {
            var root = new GameObject("Evidencia_BitacoraReal");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;

            Color leatherColor = new Color(0.22f, 0.14f, 0.08f);
            Color paperColor = new Color(0.92f, 0.88f, 0.78f);
            Color goldGlow = new Color(1.0f, 0.75f, 0.2f, 1.0f);

            Material matCover = CreateGlowMaterial(leatherColor, goldGlow, 2.0f);
            Material matPages = CreateGlowMaterial(paperColor, goldGlow, 1.4f);

            // Cubierta del cuaderno
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = "CubiertaCuero";
            cover.transform.SetParent(root.transform, false);
            cover.transform.localScale = new Vector3(0.26f, 0.035f, 0.36f);
            cover.GetComponent<Renderer>().sharedMaterial = matCover;

            // Bloque de páginas interiores
            var pages = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pages.name = "HojasManuscritas";
            pages.transform.SetParent(root.transform, false);
            pages.transform.localPosition = new Vector3(0.01f, 0.005f, 0f);
            pages.transform.localScale = new Vector3(0.24f, 0.03f, 0.34f);
            pages.GetComponent<Renderer>().sharedMaterial = matPages;

            // Colisionador principal
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.08f, 0.4f);

            var item = root.AddComponent<InspectablePropItem>();
            item.evidenceType = EvidenceType.BitacoraReal;
            item.evidenceTitle = "Bitácora Secreta de Navegación";
            item.oitViolationCode = "OIT C188 - Art. 8 (Falsificación de Registros Náuticos)";
            item.description = "Cuaderno oculto con coordenadas reales de calado en la reserva marina protegida y registros de jornadas laborales de 18 horas.";
            item.highlightGlowColor = goldGlow;
            item.targetRenderers.Add(cover.GetComponent<Renderer>());
            item.targetRenderers.Add(pages.GetComponent<Renderer>());

            return root;
        }

        /// <summary>
        /// Genera el lote de Pasaportes Retenidos de los tripulantes atados con cincha.
        /// </summary>
        public static GameObject CreatePasaportes(Vector3 position, Transform parent = null)
        {
            var root = new GameObject("Evidencia_PasaportesRetenidos");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;

            Color passportBurgundy = new Color(0.48f, 0.08f, 0.12f);
            Color passportBlue = new Color(0.08f, 0.16f, 0.38f);
            Color cyanGlow = new Color(0.18f, 0.96f, 1.0f, 1.0f);

            Material matBurgundy = CreateGlowMaterial(passportBurgundy, cyanGlow, 2.2f);
            Material matBlue = CreateGlowMaterial(passportBlue, cyanGlow, 2.2f);

            // Pila de tres pasaportes intercalados
            var p1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p1.name = "Pasaporte_01";
            p1.transform.SetParent(root.transform, false);
            p1.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            p1.transform.localRotation = Quaternion.Euler(0, 4f, 0);
            p1.transform.localScale = new Vector3(0.125f, 0.01f, 0.18f);
            p1.GetComponent<Renderer>().sharedMaterial = matBurgundy;

            var p2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p2.name = "Pasaporte_02";
            p2.transform.SetParent(root.transform, false);
            p2.transform.localPosition = new Vector3(0.005f, 0.015f, 0.002f);
            p2.transform.localRotation = Quaternion.Euler(0, -3f, 0);
            p2.transform.localScale = new Vector3(0.125f, 0.01f, 0.18f);
            p2.GetComponent<Renderer>().sharedMaterial = matBlue;

            var p3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p3.name = "Pasaporte_03";
            p3.transform.SetParent(root.transform, false);
            p3.transform.localPosition = new Vector3(-0.003f, 0.025f, -0.001f);
            p3.transform.localRotation = Quaternion.Euler(0, 8f, 0);
            p3.transform.localScale = new Vector3(0.125f, 0.01f, 0.18f);
            p3.GetComponent<Renderer>().sharedMaterial = matBurgundy;

            // Colisionador
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.18f, 0.06f, 0.22f);

            var item = root.AddComponent<InspectablePropItem>();
            item.evidenceType = EvidenceType.PasaportesRetenidos;
            item.evidenceTitle = "Lote de Pasaportes Confiscados";
            item.oitViolationCode = "OIT C029 - Convenio Trabajo Forzoso (Retención de Identidad)";
            item.description = "Documentos originales retenidos bajo llave por el armador para impedir que los pescadores desembarquen o soliciten auxilio consular.";
            item.highlightGlowColor = cyanGlow;
            item.targetRenderers.Add(p1.GetComponent<Renderer>());
            item.targetRenderers.Add(p2.GetComponent<Renderer>());
            item.targetRenderers.Add(p3.GetComponent<Renderer>());

            return root;
        }

        /// <summary>
        /// Genera la radio emisora marina satelital VHF con microteléfono y antena.
        /// </summary>
        public static GameObject CreateRadioVHF(Vector3 position, Transform parent = null)
        {
            var root = new GameObject("Evidencia_RadioVHFSatelital");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;

            Color chassisColor = new Color(0.14f, 0.15f, 0.16f);
            Color dialColor = new Color(0.35f, 0.38f, 0.40f);
            Color amberAlert = new Color(1.0f, 0.5f, 0.1f, 1.0f);

            Material matChassis = CreateGlowMaterial(chassisColor, amberAlert, 1.8f);
            Material matDial = CreateGlowMaterial(dialColor, amberAlert, 1.5f);

            // Chasis principal de la emisora
            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "ChasisRadio";
            chassis.transform.SetParent(root.transform, false);
            chassis.transform.localScale = new Vector3(0.28f, 0.14f, 0.22f);
            chassis.GetComponent<Renderer>().sharedMaterial = matChassis;

            // Dial de frecuencia
            var dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dial.name = "DialFrecuenciaCifrada";
            dial.transform.SetParent(chassis.transform, false);
            dial.transform.localPosition = new Vector3(0.25f, 0.1f, 0.52f);
            dial.transform.localRotation = Quaternion.Euler(90, 0, 0);
            dial.transform.localScale = new Vector3(0.18f, 0.05f, 0.18f);
            dial.GetComponent<Renderer>().sharedMaterial = matDial;

            // Antena rígida
            var antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            antenna.name = "AntenaFlexible";
            antenna.transform.SetParent(chassis.transform, false);
            antenna.transform.localPosition = new Vector3(-0.35f, 0.8f, -0.3f);
            antenna.transform.localScale = new Vector3(0.04f, 0.8f, 0.04f);
            antenna.GetComponent<Renderer>().sharedMaterial = matDial;

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.32f, 0.3f, 0.26f);

            var item = root.AddComponent<InspectablePropItem>();
            item.evidenceType = EvidenceType.RadioVHFSatelital;
            item.evidenceTitle = "Emisora Satelital de Banda Oculta";
            item.oitViolationCode = "OMI/OIT Directrices de Inspección Marítima - Canal Clandestino";
            item.description = "Transceptor sintonizado en canal privado no registrado utilizado para coordinar trasbordos ilegales en alta mar con buques nodriza.";
            item.highlightGlowColor = amberAlert;
            item.targetRenderers.Add(chassis.GetComponent<Renderer>());
            item.targetRenderers.Add(dial.GetComponent<Renderer>());
            item.targetRenderers.Add(antenna.GetComponent<Renderer>());

            return root;
        }

        /// <summary>
        /// Genera la consola de navegación GPS Plotter con soporte y pantalla LCD.
        /// </summary>
        public static GameObject CreatePlotterGPS(Vector3 position, Transform parent = null)
        {
            var root = new GameObject("Evidencia_PlotterGPSNautico");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;

            Color bodyColor = new Color(0.18f, 0.20f, 0.22f);
            Color screenColor = new Color(0.05f, 0.35f, 0.32f); // Pantalla LCD náutica
            Color glowColor = new Color(0.2f, 0.98f, 0.8f, 1.0f);

            Material matBody = CreateGlowMaterial(bodyColor, glowColor, 1.8f);
            Material matScreen = CreateGlowMaterial(screenColor, glowColor, 2.5f);

            // Carcasa del terminal
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CarcasaPlotter";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.32f, 0.22f, 0.08f);
            body.transform.localRotation = Quaternion.Euler(-15f, 0, 0);
            body.GetComponent<Renderer>().sharedMaterial = matBody;

            // Pantalla táctica con derrota GPS
            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "PantallaLCD_RutaClandestina";
            screen.transform.SetParent(body.transform, false);
            screen.transform.localPosition = new Vector3(-0.06f, 0.02f, 0.52f);
            screen.transform.localScale = new Vector3(0.72f, 0.78f, 0.08f);
            screen.GetComponent<Renderer>().sharedMaterial = matScreen;

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.36f, 0.28f, 0.16f);

            var item = root.AddComponent<InspectablePropItem>();
            item.evidenceType = EvidenceType.PlotterGPSNautico;
            item.evidenceTitle = "Terminal Plotter GPS y Rutas Guardadas";
            item.oitViolationCode = "FAO/OIT Acuerdo INDNR - Apagado Ilegal de AIS";
            item.description = "Historial de trayectorias que demuestra el apagado intencional de la señal AIS durante 72 horas para faenar en zona de exclusión.";
            item.highlightGlowColor = glowColor;
            item.targetRenderers.Add(body.GetComponent<Renderer>());
            item.targetRenderers.Add(screen.GetComponent<Renderer>());

            return root;
        }

        public static GameObject CreateEvidence(EvidenceType type, Vector3 position, Transform parent = null)
        {
            switch (type)
            {
                case EvidenceType.BitacoraReal:
                    return CreateBitacora(position, parent);
                case EvidenceType.PasaportesRetenidos:
                    return CreatePasaportes(position, parent);
                case EvidenceType.RadioVHFSatelital:
                    return CreateRadioVHF(position, parent);
                case EvidenceType.PlotterGPSNautico:
                    return CreatePlotterGPS(position, parent);
                default:
                    return CreateBitacora(position, parent);
            }
        }

        public static List<GameObject> CreateAllEvidenceCollection(Vector3 basePosition, Transform parent = null)
        {
            var list = new List<GameObject>
            {
                CreateBitacora(basePosition + new Vector3(0, 0, 0), parent),
                CreatePasaportes(basePosition + new Vector3(0.6f, 0, 0), parent),
                CreateRadioVHF(basePosition + new Vector3(1.2f, 0, 0), parent),
                CreatePlotterGPS(basePosition + new Vector3(1.8f, 0, 0), parent)
            };
            return list;
        }
    }
}
