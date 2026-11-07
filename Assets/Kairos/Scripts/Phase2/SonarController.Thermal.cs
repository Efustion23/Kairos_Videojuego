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
                ghost.GetComponent<MeshFilter>().sharedMesh = hull.GetComponent<MeshFilter>().sharedMesh;
                ghost.GetComponent<MeshRenderer>().material = Mats.Glow(new Color(.1f, .2f, .35f, .16f));
                ghost.layer = ThermalLayer;
            }
            foreach (var k in CaseData.Compartments)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(b.GetComponent<Collider>());
                b.layer = ThermalLayer;
                b.transform.SetParent(thermalRoot.transform, false);
                b.transform.position = ship.TransformPoint(k.center);
                b.transform.localScale = k.size;
                Color c = k.kind == 0 ? Pal.ThermCold : k.kind == 2 ? new Color(1f, .55f, .2f) : new Color(Pal.ThermHot.r, Pal.ThermHot.g * .8f, Pal.ThermHot.b * .5f);
                c.a = k.kind == 1 ? .28f : .42f;
                b.GetComponent<MeshRenderer>().material = Mats.Glow(c);
            }
            // Focos de 36-37 °C dentro del compartimento con personas: nunca un contador, solo huella térmica.
            var k4 = CaseData.Compartments[3];
            var rnd = new System.Random(4);
            for (int i = 0; i < 3; i++) // Ari, Moussa y Thant; Rodel trabaja fuera de K4
            {
                var f = k4.center + new Vector3(((float)rnd.NextDouble() - .5f) * 3.5f, 1.8f, ((float)rnd.NextDouble() - .5f) * 12f);
                personFoci.Add(f);
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(s.GetComponent<Collider>());
                s.layer = ThermalLayer;
                s.transform.SetParent(thermalRoot.transform, false);
                s.transform.position = ship.TransformPoint(f);
                s.transform.localScale = new Vector3(.9f, 1.7f, .9f);
                s.GetComponent<MeshRenderer>().material = Mats.Glow(Pal.ThermHot, true, true);
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
        }

        GameObject thermalVisorOverlay;

        void ToggleDrone()
        {
            if (droneActive) { EndDrone(false); return; }
            if (Ocean.Douglas >= 6) { toast.Show("El dron no vuela con Douglas 6 o más.", Pal.DeckLight); return; }
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

            thermalVisorOverlay = new GameObject("ThermalVisorOverlay");
            thermalVisorOverlay.transform.SetParent(canvasRoot, false);

            var topPanel = UI.Panel(thermalVisorOverlay.transform, new Vector2(.25f, .93f), new Vector2(.75f, .985f), default, default, new Color(.01f, .03f, .08f, .85f));
            var topText = UI.Text(topPanel.transform, "● ÓPTICA TÉRMICA FLIR DRON · CONTRASTE TÉRMICO AZUL/ÁMBAR", 15, Pal.ThermHot, TMPro.TextAlignmentOptions.Center);
            topText.margin = new Vector4(6, 2, 6, 2);

            var scalePanel = UI.Panel(thermalVisorOverlay.transform, new Vector2(.02f, .02f), new Vector2(.28f, .10f), default, default, new Color(.01f, .04f, .09f, .88f));
            var scaleText = UI.Text(scalePanel.transform, "<color=#2050FF>■ Frío (-18°C)</color>  |  <color=#FFD27A>■ Vital (37°C)</color>\nBodegas refrigeradas vs. Presencia humana", 13, Pal.Text, TMPro.TextAlignmentOptions.Center);
            scaleText.margin = new Vector4(4, 2, 4, 2);
        }

        void EndDrone(bool lost)
        {
            droneActive = false;
            thermalRoot.SetActive(false);
            if (thermalVisorOverlay != null) Destroy(thermalVisorOverlay);
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
