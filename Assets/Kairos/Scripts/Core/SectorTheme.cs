using TMPro;
using UnityEngine;

namespace Kairos
{
    /// Ajusta en tiempo de ejecución la atmósfera, iluminación de cubierta, rótulos del buque y exigencia táctica
    /// para que cada uno de los 3 sectores oceánicos (Atlántico Sur, Golfo de Guinea, Océano Índico) se sienta único.
    public static class SectorTheme
    {
        public static void Apply(int sector, Transform shipRoot = null, Light[] deckLights = null)
        {
            if (sector <= 0) return;

            var targetName = CaseData.TargetFor(sector).name.ToUpperInvariant();

            // 1. Atmósfera marítima y niebla propia del sector
            if (sector == 1)
            {
                // Sector 2: Golfo de Guinea — neblina cálida húmeda, foco sodio industrial y mar picado
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.14f, .16f, .13f);
                RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, .0034f);
                RenderSettings.ambientLight = new Color(.18f, .20f, .16f);
                TintDeckLights(deckLights, shipRoot, new Color(1f, .52f, .18f), 1.15f);
                TintMoon(new Color(.85f, .88f, .76f), .68f);
            }
            else if (sector == 2)
            {
                // Sector 3: Océano Índico Occidental — tormenta monzónica azul-acero, LED frío de alta potencia y máxima alerta
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.06f, .11f, .19f);
                RenderSettings.fogDensity = Mathf.Max(RenderSettings.fogDensity, .0030f);
                RenderSettings.ambientLight = new Color(.14f, .20f, .30f);
                TintDeckLights(deckLights, shipRoot, new Color(.68f, .90f, 1f), 1.20f);
                TintMoon(new Color(.68f, .84f, 1f), .85f);
            }

            // 2. Actualizar rótulos 3D del casco/puente con el nombre real del buque del sector
            var root = shipRoot != null ? shipRoot : GameObject.Find("HaiFeng77")?.transform;
            if (root != null)
            {
                foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp.text != null && tmp.text.ToUpperInvariant().Contains("HAI FENG"))
                        tmp.text = targetName;
                }
            }
        }

        static void TintDeckLights(Light[] deckLights, Transform shipRoot, Color tint, float intensityMul)
        {
            if (deckLights != null && deckLights.Length > 0)
            {
                foreach (var l in deckLights)
                {
                    if (l == null) continue;
                    l.color = tint;
                    l.intensity *= intensityMul;
                }
                return;
            }
            var root = shipRoot != null ? shipRoot : GameObject.Find("HaiFeng77")?.transform;
            if (root == null) return;
            foreach (var l in root.GetComponentsInChildren<Light>(true))
            {
                if (l.name.StartsWith("Foco") || l.name == "Luz Tejado")
                {
                    l.color = tint;
                    l.intensity *= intensityMul;
                }
            }
        }

        static void TintMoon(Color color, float intensity)
        {
            var moon = GameObject.Find("Luna")?.GetComponent<Light>();
            if (moon != null) { moon.color = color; moon.intensity = intensity; }
        }
    }
}
