using TMPro;
using UnityEngine;

namespace Kairos
{
    /// Ajusta en tiempo de ejecución el entorno, fondo oceánico, buque frigorífico/nodriza en horizonte,
    /// iluminación de cubierta, paleta del casco y rótulos 3D para que los 3 sectores del GDD se diferencien de verdad.
    public static class SectorTheme
    {
        public static void Apply(int sector, Transform shipRoot = null, Light[] deckLights = null)
        {
            if (sector <= 0) return;

            var root = shipRoot != null ? shipRoot : GameObject.Find("HaiFeng77")?.transform;
            var targetName = CaseData.TargetFor(sector).name.ToUpperInvariant();
            var cam = Camera.main;

            if (sector == 1)
            {
                // SECTOR 2: GOLFO DE GUINEA (Kestrel Explorer — Arrastrero industrial, neblina cálida húmeda y transbordo)
                var fogCol = new Color(.14f, .14f, .10f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogCol;
                RenderSettings.fogDensity = .0038f;
                RenderSettings.ambientLight = new Color(.22f, .20f, .14f);
                if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.06f, .07f, .05f); }
                TintDeckLights(deckLights, root, new Color(1f, .48f, .14f), 1.15f);
                TintMoon(new Color(.95f, .86f, .68f), .72f);
                TintShipPalette(root, new Color(.78f, .86f, .72f));
                ConfigureFleetAndReefer(sector, "Atlantic Reef", new Vector3(135f, 0f, 35f), new Color(1f, .68f, .32f));
            }
            else if (sector == 2)
            {
                // SECTOR 3: OCÉANO ÍNDICO OCCIDENTAL (Oceanic Crown 9 — Palangrero de altura, tormenta monzónica azul-acero y nodriza)
                var fogCol = new Color(.04f, .09f, .18f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogCol;
                RenderSettings.fogDensity = .0034f;
                RenderSettings.ambientLight = new Color(.14f, .22f, .34f);
                if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.015f, .035f, .075f); }
                TintDeckLights(deckLights, root, new Color(.65f, .90f, 1f), 1.25f);
                TintMoon(new Color(.64f, .82f, 1f), .95f);
                TintShipPalette(root, new Color(.72f, .84f, 1.0f));
                AddRedSecurityBeacons(root);
                ConfigureFleetAndReefer(sector, "Mombasa Reefer", new Vector3(-155f, 0f, 55f), new Color(.65f, .90f, 1f));
            }

            // Actualizar rótulos 3D del casco/puente con el nombre del buque del sector
            if (root != null)
            {
                foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp.text != null && tmp.text.ToUpperInvariant().Contains("HAI FENG"))
                        tmp.text = targetName;
                }
            }
        }

        static void TintDeckLights(Light[] deckLights, Transform root, Color tint, float intensityMul)
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

        static void TintShipPalette(Transform root, Color hullTint)
        {
            if (root == null) return;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null || mr.sharedMaterial == null) continue;
                var mName = mr.sharedMaterial.name;
                if (mName.Contains("Hull") || mName.Contains("Super") || mName.Contains("Rust") || mName.Contains("Deck"))
                {
                    var mat = mr.material;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.GetColor("_BaseColor") * hullTint);
                }
            }
        }

        static void AddRedSecurityBeacons(Transform root)
        {
            if (root == null) return;
            var positions = new[] { new Vector3(0f, 13.4f, -6f), new Vector3(0f, 11.8f, -43.4f) };
            for (int i = 0; i < positions.Length; i++)
            {
                var go = new GameObject($"BalizaSeguridad_{i}");
                go.transform.SetParent(root, false);
                go.transform.localPosition = positions[i];
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, .15f, .12f);
                l.range = 14f;
                l.intensity = 6f;
                l.shadows = LightShadows.None;
            }
        }

        /// En Sector 2/3 oculta la flota lejana de poteros argentinos y coloca el frigorífico/nodriza correspondiente del GDD.
        static void ConfigureFleetAndReefer(int sector, string reeferName, Vector3 pos, Color lightCol)
        {
            var fleet = GameObject.Find("Flota lejana");
            if (fleet != null) fleet.SetActive(false);

            var reefer = new GameObject($"BuqueNodriza_{reeferName}");
            reefer.transform.position = pos;
            reefer.transform.rotation = Quaternion.Euler(0, sector == 1 ? -18f : 22f, 0);

            void Part(string n, Vector3 lp, Vector3 sc, Color c, bool glow = false)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = n;
                Object.Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(reefer.transform, false);
                g.transform.localPosition = lp;
                g.transform.localScale = sc;
                g.GetComponent<MeshRenderer>().material = glow ? Mats.Glow(c, true, true) : new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
            }

            // Silueta de gran buque frigorífico / nodriza en el horizonte
            Part("Casco", new Vector3(0, 4f, 0), new Vector3(18f, 9f, 92f), new Color(.08f, .10f, .12f));
            Part("Superestructura", new Vector3(0, 13f, -26f), new Vector3(14f, 11f, 22f), new Color(.14f, .16f, .18f));
            Part("PuenteLuz", new Vector3(0, 15.5f, -15f), new Vector3(11f, 1.4f, .5f), lightCol, true);
            for (float z = -10f; z <= 32f; z += 14f)
                Part("FocoCarga", new Vector3(0, 10.5f, z), new Vector3(12f, .8f, .8f), lightCol, true);
        }
    }
}
