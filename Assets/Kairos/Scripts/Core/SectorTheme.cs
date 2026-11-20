using TMPro;
using UnityEngine;

namespace Kairos
{
    /// Ajusta en tiempo de ejecución el entorno, iluminación, rótulos 3D y barcos 3D reales (Resources/Ships)
    /// para que los 3 sectores oceánicos (Atlántico Sur, Golfo de Guinea, Océano Índico) se diferencien visualmente.
    public static class SectorTheme
    {
        public static void Apply(int sector, Transform shipRoot = null, Light[] deckLights = null)
        {
            var root = shipRoot != null ? shipRoot : GameObject.Find("HaiFeng77")?.transform;
            var targetName = CaseData.TargetFor(sector).name.ToUpperInvariant();
            var cam = Camera.main;

            if (sector <= 0)
            {
                // SECTOR 1: ATLÁNTICO SUR (Milla 201) — flota real de barcos pesqueros iluminados en el horizonte cercano
                SpawnRealHorizonFleet();
                return;
            }

            if (sector == 1)
            {
                // SECTOR 2: GOLFO DE GUINEA (Kestrel Explorer — arrastrero 62m, neblina cálida húmeda y transbordo con Atlantic Reef)
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.14f, .14f, .10f);
                RenderSettings.fogDensity = .0030f;
                RenderSettings.ambientLight = new Color(.22f, .20f, .14f);
                if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.06f, .07f, .05f); }
                TintDeckLights(deckLights, root, new Color(1f, .48f, .14f), 1.15f);
                TintMoon(new Color(.95f, .86f, .68f), .72f);
                TintShipPalette(root, new Color(.78f, .86f, .72f));

                // En Fase 2 (Sonar), sustituir visualmente el casco por el arrastrero 3D real KestrelExplorer_3D
                if (shipRoot != null) SwapSonarShipVisual(shipRoot, "Ships/KestrelExplorer_3D", 18.4f / 14f); // manga real del arrastrero
                SpawnHorizonReefer("Atlantic Reef", "Ships/ReeferShip_3D", new Vector3(125f, 0f, 30f), -22f, new Color(1f, .65f, .25f));
            }
            else if (sector == 2)
            {
                // SECTOR 3: OCÉANO ÍNDICO OCCIDENTAL (Oceanic Crown 9 — tormenta monzónica azul-acero, LED frío y nodriza Mombasa Reefer)
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.04f, .09f, .18f);
                RenderSettings.fogDensity = .0028f;
                RenderSettings.ambientLight = new Color(.14f, .22f, .34f);
                if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.015f, .035f, .075f); }
                TintDeckLights(deckLights, root, new Color(.65f, .90f, 1f), 1.25f);
                TintMoon(new Color(.64f, .82f, 1f), .95f);
                TintShipPalette(root, new Color(.72f, .84f, 1.0f));
                AddRedSecurityBeacons(root);

                // En Fase 2 (Sonar) se mantiene el casco del Hai Feng: falta un modelo de palangrero con proporciones reales
                SpawnHorizonReefer("Mombasa Reefer", "Ships/ReeferShip_3D", new Vector3(-135f, 0f, 45f), 28f, new Color(.65f, .90f, 1f));
            }

            if (root != null)
            {
                foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp.text != null && tmp.text.ToUpperInvariant().Contains("HAI FENG"))
                        tmp.text = targetName;
                }
            }
        }

        /// En 02_Sonar reemplaza la malla visual de HaiFeng77 por el barco 3D real del sector, conservando el HullShell para el sonar.
        static void SwapSonarShipVisual(Transform shipRoot, string resourcePath, float beamScale = 1f)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return;

            // El sonar dibuja el casco con impactos sobre «Hull» y la lancha choca con su manga: ambos siguen al barco nuevo.
            var hull = shipRoot.Find("Hull");
            if (hull != null) hull.localScale = Vector3.Scale(hull.localScale, new Vector3(beamScale, 1f, 1f));
            var sonar = Object.FindFirstObjectByType<SonarController>();
            if (sonar != null) sonar.beamScale = beamScale;

            foreach (var r in shipRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            var inst = Object.Instantiate(prefab, shipRoot, false);
            inst.name = prefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
        }

        static void SpawnHorizonReefer(string displayName, string resourcePath, Vector3 pos, float yaw, Color lightColor)
        {
            var fleet = GameObject.Find("Flota lejana");
            if (fleet != null) fleet.SetActive(false);

            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return;

            var inst = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f));
            inst.name = $"BuqueHorizonte_{displayName}";

            var lampGo = new GameObject("LuzHorizonte");
            lampGo.transform.SetParent(inst.transform, false);
            lampGo.transform.localPosition = new Vector3(0f, 16f, 0f);
            var l = lampGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = lightColor;
            l.range = 75f;
            l.intensity = 45f;
            l.shadows = LightShadows.None;
        }

        static void SpawnRealHorizonFleet()
        {
            if (GameObject.Find("FlotaRealMilla201") != null) return;
            var trawler = Resources.Load<GameObject>("Ships/KestrelExplorer_3D");
            var reefer = Resources.Load<GameObject>("Ships/ReeferShip_3D");
            if (trawler == null && reefer == null) return;

            var root = new GameObject("FlotaRealMilla201");
            // A 400-500 m del objetivo: se leen como flota en el horizonte y ninguno se cruza con la lancha.
            var placements = new (Vector3 pos, float yaw, bool useReefer)[]
            {
                (new Vector3(385f, 0f, 209f), -35f, false),
                (new Vector3(-418f, 0f, 264f), 42f, false),
                (new Vector3(462f, 0f, -242f), -125f, true),
                (new Vector3(-363f, 0f, -319f), 115f, false),
                (new Vector3(88f, 0f, 517f), 85f, true),
                (new Vector3(-121f, 0f, -495f), -70f, false),
            };

            for (int i = 0; i < placements.Length; i++)
            {
                var p = placements[i];
                var src = (p.useReefer && reefer != null) ? reefer : (trawler ?? reefer);
                var v = Object.Instantiate(src, p.pos, Quaternion.Euler(0f, p.yaw, 0f), root.transform);
                v.name = $"PesqueroMilla201_{i + 1:00}";
                foreach (var c in v.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
                var lamp = new GameObject("FocoPotero");
                lamp.transform.SetParent(v.transform, false);
                lamp.transform.localPosition = new Vector3(0f, 13f, 0f);
                var l = lamp.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, .76f, .34f);
                l.range = 60f;
                l.intensity = 35f;
                l.shadows = LightShadows.None;
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
    }
}
