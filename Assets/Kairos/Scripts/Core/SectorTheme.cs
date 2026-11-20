using TMPro;
using UnityEngine;

namespace Kairos
{
    /// Ajusta en tiempo de ejecución cielo (M_NightSky), mar (M_Ocean), niebla, iluminación, clima (relámpagos/balizas)
    /// y barcos 3D reales (Resources/Ships) para que los 3 sectores oceánicos sean visualmente inconfundibles
    /// tanto en 02_Sonar como en 03_Infiltracion.
    public static class SectorTheme
    {
        public static void Apply(int sector, Transform shipRoot = null, Light[] deckLights = null)
        {
            var root = shipRoot != null ? shipRoot : GameObject.Find("HaiFeng77")?.transform;
            var targetName = CaseData.TargetFor(sector).name.ToUpperInvariant();
            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;

            if (sector <= 0)
            {
                // ═══════════════════════════════════════════════════════════════════════
                // SECTOR 1: ATLÁNTICO SUR — MILLA 201 (Noche polar azul + ciudad flotante dorada)
                // ═══════════════════════════════════════════════════════════════════════
                ApplySky(new Color(.015f, .035f, .075f), new Color(.14f, .22f, .32f), .45f);
                ApplyOcean(new Color(.030f, .075f, .135f), new Color(.055f, .165f, .240f), new Color(.14f, .22f, .32f), new Color(.62f, .74f, .84f));
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.055f, .095f, .145f);
                RenderSettings.fogDensity = .0020f;
                RenderSettings.ambientLight = new Color(.20f, .26f, .34f);
                TintMoon(new Color(.82f, .90f, 1f), .85f);
                TintDeckLights(deckLights, root, new Color(1f, .88f, .68f), 1.08f);

                // El objetivo del Sector 1 es el Hai Feng 77 del generador: su casco coincide con el sonar y con el interior de la Fase 3.
                SpawnSector1Fleet();
            }
            else if (sector == 1)
            {
                // ═══════════════════════════════════════════════════════════════════════
                // SECTOR 2: GOLFO DE GUINEA (Crepúsculo tropical cálido + mar verde-petróleo + transbordo)
                // ═══════════════════════════════════════════════════════════════════════
                ApplySky(new Color(.11f, .065f, .045f), new Color(.52f, .28f, .11f), .78f);
                ApplyOcean(new Color(.025f, .105f, .095f), new Color(.065f, .225f, .185f), new Color(.48f, .28f, .14f), new Color(.78f, .70f, .52f));
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.22f, .15f, .085f);
                RenderSettings.fogDensity = .0028f;
                RenderSettings.ambientLight = new Color(.30f, .23f, .15f);
                TintMoon(new Color(1f, .76f, .46f), .95f);
                TintDeckLights(deckLights, root, new Color(1f, .52f, .14f), 1.25f);
                TintShipPalette(root, new Color(.92f, .86f, .70f));

                if (shipRoot != null) SwapSonarShipVisual(shipRoot, "Ships/KestrelExplorer_3D");
                SpawnSector2Fleet();
            }
            else
            {
                // ═══════════════════════════════════════════════════════════════════════
                // SECTOR 3: OCÉANO ÍNDICO (Tormenta monzónica eléctrica + LED cian + relámpagos + balizas rojas)
                // ═══════════════════════════════════════════════════════════════════════
                ApplySky(new Color(.010f, .025f, .065f), new Color(.08f, .20f, .34f), .95f);
                ApplyOcean(new Color(.015f, .045f, .095f), new Color(.040f, .145f, .240f), new Color(.12f, .26f, .42f), new Color(.84f, .92f, 1f));
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.035f, .085f, .165f);
                RenderSettings.fogDensity = .0030f;
                RenderSettings.ambientLight = new Color(.15f, .24f, .38f);
                TintMoon(new Color(.58f, .82f, 1f), 1.05f);
                TintDeckLights(deckLights, root, new Color(.55f, .92f, 1f), 1.30f);
                TintShipPalette(root, new Color(.68f, .82f, 1.0f));
                var beacons = AddRedSecurityBeacons(root);
                SpawnStormController(beacons);

                if (shipRoot != null) SwapSonarShipVisual(shipRoot, "Ships/CargoShip_3D");
                SpawnSector3Fleet();
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

        static void ApplySky(Color zenith, Color horizon, float clouds)
        {
            if (RenderSettings.skybox == null) return;
            var mat = new Material(RenderSettings.skybox);
            if (mat.HasProperty("_Zenith")) mat.SetColor("_Zenith", zenith);
            if (mat.HasProperty("_Horizon")) mat.SetColor("_Horizon", horizon);
            if (mat.HasProperty("_Clouds")) mat.SetFloat("_Clouds", clouds);
            RenderSettings.skybox = mat;
        }

        static void ApplyOcean(Color deep, Color shallow, Color skyTint, Color foam)
        {
            var ocean = GameObject.Find("Ocean") ?? GameObject.Find("Mar") ?? GameObject.Find("Agua");
            if (ocean == null) return;
            foreach (var r in ocean.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null) continue;
                var mat = r.material;
                if (mat.HasProperty("_Deep")) mat.SetColor("_Deep", deep);
                if (mat.HasProperty("_Shallow")) mat.SetColor("_Shallow", shallow);
                if (mat.HasProperty("_SkyTint")) mat.SetColor("_SkyTint", skyTint);
                if (mat.HasProperty("_FoamColor")) mat.SetColor("_FoamColor", foam);
            }
        }

        /// En 02_Sonar reemplaza la malla visual de HaiFeng77 por el barco 3D real del sector, conservando el HullShell para el sonar.
        static void SwapSonarShipVisual(Transform shipRoot, string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return;

            foreach (var r in shipRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            var inst = Object.Instantiate(prefab, shipRoot, false);
            inst.name = prefab.name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            // Manga real del barco nuevo (su pieza de mayor planta) frente a los 14 m del Hai Feng:
            // el casco que lee el sonar y el choque de la lancha la siguen.
            float beam = 14f, best = 0f;
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var size = shipRoot.InverseTransformVector(r.bounds.size);
                float area = Mathf.Abs(size.x * size.z);
                if (area > best) { best = area; beam = Mathf.Abs(size.x); }
            }
            float beamScale = Mathf.Clamp(beam / 14f, .7f, 2f);
            var hull = shipRoot.Find("Hull");
            if (hull != null) hull.localScale = Vector3.Scale(hull.localScale, new Vector3(beamScale, 1f, 1f));
            var sonar = Object.FindFirstObjectByType<SonarController>();
            if (sonar != null) sonar.beamScale = beamScale;
        }

        static void HideLegacyFleet()
        {
            var fleet = GameObject.Find("Flota lejana");
            if (fleet != null) fleet.SetActive(false);
        }

        static void SpawnVessel(Transform parent, string name, string resourcePath, Vector3 pos, float yaw, Color lightColor, float lightIntensity = 55f, float lightHeight = 16f)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return;
            pos *= 3f; // a 270-500 m del objetivo: flota en el horizonte que no se cruza con la lancha ni tapa al blanco
            var v = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent);
            v.name = name;
            foreach (var c in v.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);

            var lamp = new GameObject("FocoBuque");
            lamp.transform.SetParent(v.transform, false);
            lamp.transform.localPosition = new Vector3(0f, lightHeight, 0f);
            var l = lamp.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = lightColor;
            l.range = 85f;
            l.intensity = lightIntensity;
            l.shadows = LightShadows.None;
        }

        // Flotas solo con los modelos livianos (Kestrel, carguero, frigorífico): Rita y el carguero pesquero pasan del millón de triángulos.
        static void SpawnSector1Fleet()
        {
            if (GameObject.Find("FlotaSector") != null) return;
            HideLegacyFleet();
            var root = new GameObject("FlotaSector").transform;
            var gold = new Color(1f, .78f, .32f);
            SpawnVessel(root, "PoteroMilla201_01", "Ships/KestrelExplorer_3D", new Vector3(95f, 0f, 55f), -35f, gold, 65f, 15f);
            SpawnVessel(root, "PoteroMilla201_02", "Ships/KestrelExplorer_3D", new Vector3(-105f, 0f, 65f), 42f, gold, 55f, 12f);
            SpawnVessel(root, "TransbordoMilla201_03", "Ships/CargoShip_3D", new Vector3(135f, 0f, -85f), -125f, gold, 65f, 14f);
            SpawnVessel(root, "ReeferMilla201_04", "Ships/ReeferShip_3D", new Vector3(-125f, 0f, -105f), 110f, gold, 70f, 18f);
            SpawnVessel(root, "PoteroMilla201_05", "Ships/KestrelExplorer_3D", new Vector3(30f, 0f, 145f), 82f, gold, 55f, 12f);
        }

        static void SpawnSector2Fleet()
        {
            if (GameObject.Find("FlotaSector") != null) return;
            HideLegacyFleet();
            var root = new GameObject("FlotaSector").transform;
            var amber = new Color(1f, .58f, .20f);
            // Nodriza Atlantic Reef muy visible haciendo transbordo cercano + pesqueros costeros
            SpawnVessel(root, "BuqueHorizonte_AtlanticReef", "Ships/CargoShip_3D", new Vector3(88f, 0f, 22f), -18f, amber, 80f, 20f);
            SpawnVessel(root, "PesqueroGuinea_Rita", "Ships/KestrelExplorer_3D", new Vector3(-82f, 0f, 48f), 32f, amber, 55f, 12f);
            SpawnVessel(root, "TransbordoGuinea_02", "Ships/ReeferShip_3D", new Vector3(-115f, 0f, -72f), 125f, amber, 60f, 14f);
        }

        static void SpawnSector3Fleet()
        {
            if (GameObject.Find("FlotaSector") != null) return;
            HideLegacyFleet();
            var root = new GameObject("FlotaSector").transform;
            var cyan = new Color(.55f, .90f, 1f);
            // Nodriza Mombasa Reefer + escoltas en tormenta monzónica
            SpawnVessel(root, "BuqueHorizonte_MombasaReefer", "Ships/ReeferShip_3D", new Vector3(-95f, 0f, 38f), 26f, cyan, 80f, 18f);
            SpawnVessel(root, "EscoltaIndico_02", "Ships/KestrelExplorer_3D", new Vector3(55f, 0f, 125f), -65f, cyan, 65f, 15f);
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

        static Light[] AddRedSecurityBeacons(Transform root)
        {
            if (root == null) return System.Array.Empty<Light>();
            var positions = new[] { new Vector3(0f, 13.4f, -6f), new Vector3(0f, 11.8f, -43.4f), new Vector3(0f, 11.0f, 34f) };
            var lights = new Light[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var go = new GameObject($"BalizaSeguridad_{i}");
                go.transform.SetParent(root, false);
                go.transform.localPosition = positions[i];
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, .12f, .10f);
                l.range = 20f;
                l.intensity = 10f;
                l.shadows = LightShadows.None;
                lights[i] = l;
            }
            return lights;
        }

        static void SpawnStormController(Light[] beacons)
        {
            if (GameObject.Find("SectorStormFX") != null) return;
            var go = new GameObject("SectorStormFX");
            var fx = go.AddComponent<SectorStormFX>();
            fx.beacons = beacons;
        }
    }

    /// Relámpagos periódicos y pulso de balizas rojas para el Sector 3 (Tormenta Monzónica en el Océano Índico).
    public class SectorStormFX : MonoBehaviour
    {
        public Light[] beacons;
        Light lightning;
        float nextFlash = 4.5f;
        float flashTimer;

        void Start()
        {
            var lGo = new GameObject("LightningFlash");
            lGo.transform.SetParent(transform, false);
            lGo.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
            lightning = lGo.AddComponent<Light>();
            lightning.type = LightType.Directional;
            lightning.color = new Color(.72f, .88f, 1f);
            lightning.intensity = 0f;
            lightning.shadows = LightShadows.None;
        }

        void Update()
        {
            float t = Time.time;
            if (beacons != null)
            {
                float pulse = 4f + 10f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * 4.2f), 3f);
                for (int i = 0; i < beacons.Length; i++)
                    if (beacons[i] != null) beacons[i].intensity = pulse;
            }

            nextFlash -= Time.deltaTime;
            if (nextFlash <= 0f)
            {
                flashTimer = 0.35f;
                nextFlash = Random.Range(6.5f, 13.5f);
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float f = flashTimer > 0.22f ? 3.2f : (flashTimer > 0.14f ? 0.4f : 2.1f * (flashTimer / 0.14f));
                if (lightning != null) lightning.intensity = f;
            }
            else if (lightning != null && lightning.intensity > 0f)
            {
                lightning.intensity = 0f;
            }
        }
    }
}
