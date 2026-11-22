using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Controlador de post-procesado térmico alternable con tecla rápida en la lancha.
    /// Aplica el Volume Profile térmico (azul cobalto a ámbar brillante) a la cámara
    /// y despliega el HUD FLIR diegético.
    /// </summary>
    public class ThermalVisorBoatController : MonoBehaviour
    {
        public Camera targetCamera;
        public Volume postProcessVolume;
        public VolumeProfile normalProfile;
        public VolumeProfile thermalProfile;

        [Header("Controles")]
        public Key toggleKey = Key.V;
        public bool isThermalActive;

        [Header("Capas")]
        public int thermalLayerIndex = 10;
        int savedCullingMask;

        GameObject visorCanvas;
        public GameObject VisorCanvas => visorCanvas;

        void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (postProcessVolume == null) postProcessVolume = FindAnyObjectByType<Volume>();

            if (thermalProfile == null)
            {
                thermalProfile = Resources.Load<VolumeProfile>("ThermalProfile");
#if UNITY_EDITOR
                if (thermalProfile == null)
                    thermalProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Kairos/Arte y audio/Profiles/ThermalProfile.asset");
#endif
            }

            if (postProcessVolume != null && normalProfile == null)
                normalProfile = postProcessVolume.sharedProfile;

            if (targetCamera != null) savedCullingMask = targetCamera.cullingMask;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                var keyControl = kb[toggleKey];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    ToggleThermalVisor();
                }
            }
        }

        public void ToggleThermalVisor()
        {
            SetThermalActive(!isThermalActive);
        }

        public void SetThermalActive(bool active)
        {
            isThermalActive = active;

            if (postProcessVolume != null)
            {
                postProcessVolume.sharedProfile = isThermalActive
                    ? (thermalProfile != null ? thermalProfile : normalProfile)
                    : normalProfile;
            }

            if (targetCamera != null)
            {
                if (isThermalActive)
                    targetCamera.cullingMask = savedCullingMask | (1 << thermalLayerIndex);
                else
                    targetCamera.cullingMask = savedCullingMask;
            }

            if (isThermalActive)
            {
                BuildVisorUI();
                SubmarineAudioSuite.Play(SubmarineAudioSuite.PingAcoustic, 0.4f);
            }
            else
            {
                if (visorCanvas != null)
                {
                    if (Application.isPlaying) Destroy(visorCanvas); else DestroyImmediate(visorCanvas);
                    visorCanvas = null;
                }
            }
        }

        void BuildVisorUI()
        {
            if (visorCanvas != null)
            {
                if (Application.isPlaying) Destroy(visorCanvas); else DestroyImmediate(visorCanvas);
            }

            visorCanvas = new GameObject("Prueba_ThermalVisorUI");
            var canvas = visorCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = visorCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel superior FLIR bajo la brújula (sin tapar la brújula ni los paneles izquierdo/derecho)
            var topGo = new GameObject("TopBar");
            topGo.transform.SetParent(visorCanvas.transform, false);
            var topImg = topGo.AddComponent<Image>();
            topImg.color = new Color(0.01f, 0.03f, 0.08f, 0.88f);
            topImg.raycastTarget = false;
            var topRt = topGo.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0.278f, 0.870f);
            topRt.anchorMax = new Vector2(0.645f, 0.922f);
            topRt.offsetMin = topRt.offsetMax = Vector2.zero;

            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(topGo.transform, false);
            var titleTxt = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
            titleTxt.text = "● ÓPTICA TÉRMICA FLIR LANCHA · AZUL / ÁMBAR [V]";
            titleTxt.fontSize = 15;
            titleTxt.enableAutoSizing = true;
            titleTxt.fontSizeMin = 11;
            titleTxt.fontSizeMax = 15;
            titleTxt.alignment = TMPro.TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.824f, 0.478f);
            titleTxt.raycastTarget = false;
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(8, 2); titleRt.offsetMax = new Vector2(-8, -2);

            // Escala calorimétrica bajo el panel izquierdo (sin tapar la barra de pistas ni toasts)
            var scaleGo = new GameObject("ScaleBar");
            scaleGo.transform.SetParent(visorCanvas.transform, false);
            var scaleImg = scaleGo.AddComponent<Image>();
            scaleImg.color = new Color(0.01f, 0.04f, 0.09f, 0.88f);
            scaleImg.raycastTarget = false;
            var scaleRt = scaleGo.GetComponent<RectTransform>();
            scaleRt.anchorMin = new Vector2(0.008f, 0.535f);
            scaleRt.anchorMax = new Vector2(0.268f, 0.616f);
            scaleRt.offsetMin = scaleRt.offsetMax = Vector2.zero;

            var scaleTxtGo = new GameObject("ScaleText");
            scaleTxtGo.transform.SetParent(scaleGo.transform, false);
            var scaleTxt = scaleTxtGo.AddComponent<TMPro.TextMeshProUGUI>();
            scaleTxt.text = "<color=#2050FF>■ Frío (-18°C)</color>  |  <color=#FFD27A>■ Vital (37°C)</color>\nBodegas refrigeradas vs. Presencia humana";
            scaleTxt.fontSize = 13;
            scaleTxt.enableAutoSizing = true;
            scaleTxt.fontSizeMin = 10;
            scaleTxt.fontSizeMax = 13;
            scaleTxt.alignment = TMPro.TextAlignmentOptions.Center;
            scaleTxt.color = new Color(0.86f, 0.92f, 0.94f);
            scaleTxt.raycastTarget = false;
            var scaleTxtRt = scaleTxtGo.GetComponent<RectTransform>();
            scaleTxtRt.anchorMin = Vector2.zero; scaleTxtRt.anchorMax = Vector2.one;
            scaleTxtRt.offsetMin = new Vector2(6, 2); scaleTxtRt.offsetMax = new Vector2(-6, -2);
        }

        void OnDestroy()
        {
            if (visorCanvas != null)
            {
                if (Application.isPlaying) Destroy(visorCanvas); else DestroyImmediate(visorCanvas);
            }
        }
    }
}
