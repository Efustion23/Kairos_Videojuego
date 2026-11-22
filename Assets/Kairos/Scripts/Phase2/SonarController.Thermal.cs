using UnityEngine;

namespace Kairos
{
    public partial class SonarController
    {
        void BuildThermal()
        {
            thermalRoot = new GameObject("VistaTermica");
            thermalRoot.SetActive(false);
            var hull = ship.Find("Hull");
            if (hull != null)
            {
                var ghost = new GameObject("CascoFantasma", typeof(MeshFilter), typeof(MeshRenderer));
                ghost.transform.SetParent(thermalRoot.transform, false);
                ghost.transform.SetPositionAndRotation(hull.position, hull.rotation);
                ghost.transform.localScale = hull.localScale;
                ghost.GetComponent<MeshFilter>().sharedMesh = hull.GetComponent<MeshFilter>().sharedMesh;
                ghost.GetComponent<MeshRenderer>().material = Mats.Glow(new Color(.1f, .2f, .35f, .16f));
                ghost.layer = ThermalLayer;
            }
            foreach (var k in CaseData.Compartments)
            {
                MapCompartment(k, out var mc, out var ms);
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(b.GetComponent<Collider>());
                b.layer = ThermalLayer;
                b.transform.SetParent(thermalRoot.transform, false);
                b.transform.position = ship.TransformPoint(mc);
                b.transform.localScale = ms;
                Color c = k.kind == 0 ? Pal.ThermCold : k.kind == 2 ? new Color(1f, .55f, .2f) : new Color(Pal.ThermHot.r, Pal.ThermHot.g * .8f, Pal.ThermHot.b * .5f);
                c.a = k.kind == 1 ? .28f : .42f;
                b.GetComponent<MeshRenderer>().material = Mats.Glow(c);
            }
            // Siluetas humanas térmicas (36-37 °C) sentadas/confinadas en K4 (3 sobrevivientes; el cuarto trabaja fuera)
            personFoci.Clear();
            MapCompartment(CaseData.Compartments[3], out var k4Center, out var k4Size);
            var rnd = new System.Random(4);
            var hotMat = Mats.Glow(Pal.ThermHot, true, true);
            for (int i = 0; i < 3; i++)
            {
                var rawFocus = k4Center + new Vector3(
                    ((float)rnd.NextDouble() - .5f) * Mathf.Min(2.4f, k4Size.x * .55f),
                    0.15f,
                    ((float)rnd.NextDouble() - .5f) * Mathf.Min(9.5f, k4Size.z * .72f));
                var f = ClampLocalPoint(rawFocus, 0.95f);
                personFoci.Add(f);
                var personGo = new GameObject($"SiluetaTermica_{i + 1}");
                personGo.transform.SetParent(thermalRoot.transform, false);
                personGo.transform.position = ship.TransformPoint(f);
                personGo.transform.rotation = Quaternion.Euler(0f, i * 110f + 25f, 0f);

                void BodyPart(PrimitiveType pt, Vector3 lp, Vector3 sc, Vector3 rot)
                {
                    var part = GameObject.CreatePrimitive(pt);
                    Destroy(part.GetComponent<Collider>());
                    part.layer = ThermalLayer;
                    part.transform.SetParent(personGo.transform, false);
                    part.transform.localPosition = lp;
                    part.transform.localRotation = Quaternion.Euler(rot);
                    part.transform.localScale = sc;
                    part.GetComponent<MeshRenderer>().sharedMaterial = hotMat;
                }

                BodyPart(PrimitiveType.Sphere, new Vector3(0f, .62f, .02f), new Vector3(.28f, .30f, .28f), Vector3.zero);         // cabeza
                BodyPart(PrimitiveType.Capsule, new Vector3(0f, .18f, 0f), new Vector3(.42f, .36f, .26f), new Vector3(8f, 0, 0));   // torso
                BodyPart(PrimitiveType.Capsule, new Vector3(-.22f, .14f, .12f), new Vector3(.13f, .24f, .13f), new Vector3(52f, 0, 14f)); // brazo izq recogido
                BodyPart(PrimitiveType.Capsule, new Vector3(.22f, .14f, .12f), new Vector3(.13f, .24f, .13f), new Vector3(52f, 0, -14f)); // brazo der recogido
                BodyPart(PrimitiveType.Capsule, new Vector3(-.13f, -.26f, .24f), new Vector3(.16f, .30f, .16f), new Vector3(68f, 0, 0));  // pierna izq sentada
                BodyPart(PrimitiveType.Capsule, new Vector3(.13f, -.26f, .24f), new Vector3(.16f, .30f, .16f), new Vector3(68f, 0, 0));   // pierna der sentada
            }
            // Tubería de agua dulce hasta K4, solo visible en térmica.
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(pipe.GetComponent<Collider>());
            pipe.layer = ThermalLayer;
            pipe.transform.SetParent(thermalRoot.transform, false);
            pipe.transform.position = ship.TransformPoint(new Vector3(-1.2f, 5f, -10f));
            pipe.transform.localScale = new Vector3(.25f, .25f, 26f);
            pipe.GetComponent<MeshRenderer>().material = Mats.Glow(Pal.ThermHot, true, true);

            drone = new GameObject("Dron").transform;
            void DronePart(PrimitiveType pt, Vector3 pos, Vector3 scale, Color col, int layer)
            {
                var gp = GameObject.CreatePrimitive(pt);
                Destroy(gp.GetComponent<Collider>());
                gp.layer = layer;
                gp.transform.SetParent(drone, false);
                gp.transform.localPosition = pos;
                gp.transform.localScale = scale;
                gp.GetComponent<MeshRenderer>().material = Mats.Glow(col, true, true);
            }
            DronePart(PrimitiveType.Cube, Vector3.zero, new Vector3(.6f, .18f, .9f), Pal.Sonar, ThermalLayer);
            DronePart(PrimitiveType.Cube, Vector3.zero, new Vector3(1.8f, .06f, .12f), Pal.DeckLight, ThermalLayer);
            DronePart(PrimitiveType.Cube, Vector3.zero, new Vector3(.12f, .06f, 1.8f), Pal.DeckLight, ThermalLayer);
            DronePart(PrimitiveType.Sphere, new Vector3(0, .14f, .35f), Vector3.one * .18f, Pal.Sealed, ThermalLayer);
        }

        GameObject thermalVisorOverlay;

        void ToggleDrone()
        {
            if (droneActive) { EndDrone(false); return; }
            if (Ocean.Douglas >= 6 && !Game.Has(1, 3)) { toast.Show("El dron no vuela con Douglas 6 o más (requiere Estabilizadores activos).", Pal.DeckLight); return; }
            if (Game.S.droneParts <= 0) { toast.Show("Sin repuestos de dron: solo sonar e hidrófono desde la lancha.", Pal.DeckLight); return; }
            droneActive = true;
            droneBattery = 45f;
            drone.position = boat.position + Vector3.up * 30;
            drone.rotation = Quaternion.Euler(0, yaw, 0);
            thermalRoot.SetActive(true);
            savedBg = cam.backgroundColor; savedClear = cam.clearFlags; savedMask = cam.cullingMask;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.01f, .02f, .05f);
            cam.cullingMask = 1 << ThermalLayer;

            BuildThermalVisor();
        }

        void BuildThermalVisor()
        {
            if (thermalVisorOverlay != null) Destroy(thermalVisorOverlay);
            if (canvasRoot == null) return;
            if (compassPanelObj != null) compassPanelObj.SetActive(false);

            thermalVisorOverlay = new GameObject("ThermalVisorOverlay");
            thermalVisorOverlay.transform.SetParent(canvasRoot, false);

            var topPanel = UI.Panel(thermalVisorOverlay.transform, new Vector2(.278f, .928f), new Vector2(rightCollapsed ? .715f : .645f, .988f), default, default, new Color(.01f, .03f, .08f, .90f));
            var topText = UI.Text(topPanel.transform, "ÓPTICA TÉRMICA FLIR DRON  ·  AZUL (-18°C) / ÁMBAR (37°C)", 15, Pal.ThermHot, TMPro.TextAlignmentOptions.Center, true);
            topText.margin = new Vector4(8, 2, 8, 2);
            topText.enableAutoSizing = true; topText.fontSizeMin = 11; topText.fontSizeMax = 15;

            var scalePanel = UI.Panel(thermalVisorOverlay.transform, new Vector2(.008f, .535f), new Vector2(.268f, .616f), default, default, new Color(.01f, .04f, .09f, .90f));
            var scaleText = UI.Text(scalePanel.transform, "<color=#2050FF>[FRÍO -18°C]</color>  |  <color=#FFD27A>[VITAL 37°C · 3 firmas]</color>\nBodegas refrigeradas vs. Presencia humana real", 13.5f, Pal.Text, TMPro.TextAlignmentOptions.Center);
            scaleText.margin = new Vector4(6, 2, 6, 2);
            scaleText.enableAutoSizing = true; scaleText.fontSizeMin = 11; scaleText.fontSizeMax = 14;
        }

        void EndDrone(bool lost)
        {
            droneActive = false;
            thermalRoot.SetActive(false);
            if (thermalVisorOverlay != null) Destroy(thermalVisorOverlay);
            if (compassPanelObj != null) compassPanelObj.SetActive(true);
            cam.clearFlags = savedClear; cam.backgroundColor = savedBg; cam.cullingMask = savedMask;
            float away = Vector3.Distance(drone.position, boat.position);
            if (lost || away > 220f)
            {
                bool recovered = Random.value < .5f;
                if (!recovered) Game.S.droneParts--;
                toast.Show(recovered ? "Batería agotada lejos de la lancha, pero recuperas el dron en el mar." : "Dron perdido en el mar: -1 repuesto.", Pal.DeckLight);
            }
        }
    }
}
