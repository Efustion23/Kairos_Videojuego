using System.Collections.Generic;
using UnityEngine;

namespace Kairos.Pruebas
{
    // En su propio archivo: Unity solo enlaza un MonoBehaviour guardado en escena si la clase se llama como el archivo.
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
                var mats = Application.isPlaying ? targetRenderers[i].materials : targetRenderers[i].sharedMaterials;
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
}
