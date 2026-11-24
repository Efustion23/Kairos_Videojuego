using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kairos.Pruebas
{
    /// <summary>
    /// Controlador de atmósfera marítima nocturna, niebla volumétrica e iluminación halógena de cubierta (Semana 15 / S15-3).
    /// Calibra la atenuación lumínica sobre el océano, niebla marina y parpadeo realista de reflectores por salitre.
    /// </summary>
    public class MaritimeFogAtmosphereController : MonoBehaviour
    {
        [Header("Configuración de Niebla Marina")]
        public bool enableMarineFog = true;
        public Color marineFogColor = new Color(0.035f, 0.065f, 0.10f, 1.0f); // Azul petróleo oscuro
        [Range(0.001f, 0.1f)] public float fogDensity = 0.022f;
        public FogMode fogMode = FogMode.ExponentialSquared;

        [Header("Iluminación de Cubierta y Mástil")]
        public Color halogenColor = new Color(1.0f, 0.72f, 0.42f, 1.0f); // 3200K Halógeno
        public Color mastBeaconColor = new Color(1.0f, 0.15f, 0.10f, 1.0f); // Baliza roja de peligro
        public List<Light> deckFloodlights = new List<Light>();
        public Light mastBeaconLight;

        [Header("Simulación de Salitre y Parpadeo Eléctrico")]
        public bool enableSaltyFlicker = true;
        public float flickerSpeed = 14f;
        public float flickerIntensity = 0.08f;
        public float beaconPulseSpeed = 2.5f;

        [Header("Volumen de Post-Procesado")]
        public Volume postProcessVolume;

        [Header("Integración por Sector")]
        public bool preserveActiveSectorFog = true;

        private float[] baseLightIntensities;

        private void Start()
        {
            // En escenas con SectorTheme (Fase 2 / Fase 3), respetar la niebla configurada para el sector activo
            if (preserveActiveSectorFog && RenderSettings.fog &&
                (InfiltrationController.I != null || FindFirstObjectByType<SonarController>() != null))
            {
                SyncFromRenderSettings();
            }
            else
            {
                ApplyAtmosphericSettings();
            }
            CacheLightIntensities();
        }

        public void SyncFromRenderSettings()
        {
            enableMarineFog = RenderSettings.fog;
            marineFogColor = RenderSettings.fogColor;
            fogDensity = RenderSettings.fogDensity;
            fogMode = RenderSettings.fogMode;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;

            if (enableSaltyFlicker && deckFloodlights != null)
            {
                for (int i = 0; i < deckFloodlights.Count; i++)
                {
                    if (deckFloodlights[i] == null) continue;
                    float baseIntensity = (baseLightIntensities != null && i < baseLightIntensities.Length)
                        ? baseLightIntensities[i]
                        : 2.0f;

                    // Ruido Perlin suave para variación de voltaje marino
                    float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, (float)i * 17.13f);
                    deckFloodlights[i].intensity = baseIntensity * (1.0f + (noise - 0.5f) * flickerIntensity);
                }
            }

            if (mastBeaconLight != null)
            {
                // Pulsación rítmica de advertencia náutica (estroboscópico de tope de palo)
                float pulse = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * beaconPulseSpeed));
                mastBeaconLight.intensity = pulse * 3.5f;
            }
        }

        public void ApplyAtmosphericSettings()
        {
            RenderSettings.fog = enableMarineFog;
            RenderSettings.fogColor = marineFogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogMode = fogMode;
        }

        private void CacheLightIntensities()
        {
            if (deckFloodlights == null || deckFloodlights.Count == 0) return;
            baseLightIntensities = new float[deckFloodlights.Count];
            for (int i = 0; i < deckFloodlights.Count; i++)
            {
                if (deckFloodlights[i] != null)
                {
                    baseLightIntensities[i] = deckFloodlights[i].intensity;
                }
            }
        }

        /// <summary>
        /// Crea e inicializa el rig de iluminación nocturna de cubierta con reflectores y baliza de mástil.
        /// </summary>
        public static GameObject CreateMaritimeAtmosphere(Transform parent = null)
        {
            var root = new GameObject("Atmosfera_Maritima_Nocturna");
            if (parent != null) root.transform.SetParent(parent, false);

            var ctrl = root.AddComponent<MaritimeFogAtmosphereController>();

            // 1. Reflector halógeno de Proa
            var lightBowGo = new GameObject("Reflector_Cubierta_Proa");
            lightBowGo.transform.SetParent(root.transform, false);
            lightBowGo.transform.localPosition = new Vector3(0f, 6.5f, 14f);
            lightBowGo.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            var lightBow = lightBowGo.AddComponent<Light>();
            lightBow.type = LightType.Spot;
            lightBow.color = ctrl.halogenColor;
            lightBow.intensity = 2.8f;
            lightBow.range = 28f;
            lightBow.spotAngle = 70f;
            lightBow.shadows = LightShadows.None;
            ctrl.deckFloodlights.Add(lightBow);

            // 2. Reflector halógeno de Popa (maniobra de redes)
            var lightSternGo = new GameObject("Reflector_Cubierta_Popa");
            lightSternGo.transform.SetParent(root.transform, false);
            lightSternGo.transform.localPosition = new Vector3(0f, 6.0f, -12f);
            lightSternGo.transform.localRotation = Quaternion.Euler(50f, 180f, 0f);
            var lightStern = lightSternGo.AddComponent<Light>();
            lightStern.type = LightType.Spot;
            lightStern.color = ctrl.halogenColor;
            lightStern.intensity = 2.4f;
            lightStern.range = 24f;
            lightStern.spotAngle = 75f;
            lightStern.shadows = LightShadows.None;
            ctrl.deckFloodlights.Add(lightStern);

            // 3. Baliza estroboscópica de peligro en el mástil
            var beaconGo = new GameObject("Baliza_Tope_Mastil");
            beaconGo.transform.SetParent(root.transform, false);
            beaconGo.transform.localPosition = new Vector3(0f, 12f, 2f);
            var beacon = beaconGo.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.color = ctrl.mastBeaconColor;
            beacon.intensity = 3.5f;
            beacon.range = 40f;
            beacon.shadows = LightShadows.None;
            ctrl.mastBeaconLight = beacon;

            // 4. Volume Profile de Post-Processing
            var volumeGo = new GameObject("Volume_PostProcess_Nocturno");
            volumeGo.transform.SetParent(root.transform, false);
            var vol = volumeGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.weight = 1.0f;
            ctrl.postProcessVolume = vol;

            ctrl.ApplyAtmosphericSettings();
            ctrl.CacheLightIntensities();

            return root;
        }
    }
}
