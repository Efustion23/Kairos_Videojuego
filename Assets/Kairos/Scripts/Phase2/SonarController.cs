using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kairos
{
    /// Fase 2: lancha, pulsos de sonar en la cresta, dron térmico e hidrófono. Condición de paso: Confianza ≥ 70 %.
    public class SonarController : MonoBehaviour
    {
        public Camera cam;
        public Transform ship, boat;
        public Vector3 boatStart = new Vector3(-90, 0, -430);

        const int Bit_Density = 1, Bit_Thermal = 2, Bit_Sound = 4;
        const float MaxSpeed = 18f;

        static CaseState C => Game.C;
        Buoy buoy;
        float speed, yaw, camYaw, camPitch = 14f, camDist = 16f;
        float pulseCooldown;
        readonly float[] scan = new float[5], thermal = new float[5];
        float listen;
        int selected = 3;

        ParticleSystem cloud;
        AudioSource engineSrc, listenSrc;
        readonly List<Vector3> personFoci = new List<Vector3>();

        // Dron
        bool droneActive; Transform drone; float droneBattery; GameObject thermalRoot;
        const int ThermalLayer = 10;
        Color savedBg; CameraClearFlags savedClear; int savedMask;

        // UI
        TextMeshProUGUI status, confidenceText, crestText;
        UI.Bar suspBar, crestBar, confBar, listenBar;
        readonly TextMeshProUGUI[] cardText = new TextMeshProUGUI[5];
        readonly Button[] classBtn = new Button[5], selectBtn = new Button[5];
        readonly UI.Bar[] scanBars = new UI.Bar[5];
        Button board;
        UI.Toasts toast;

        float WakeRange => Game.Has(1, 4) ? 100 : Game.Has(1, 1) ? 200 : 300;
        float PulseRange => Game.Has(0, 4) ? 600 : 350;
        float CrestWindow => Game.Has(0, 1) ? .45f : .3f; // media ventana: 0,6 s y 0,9 s
        float HydroRange => Game.Has(0, 3) ? 350 : 200;
        float DistToShip => Vector3.Distance(new Vector3(boat.position.x, 0, boat.position.z), new Vector3(ship.position.x, 0, ship.position.z));

        void Start()
        {
            buoy = boat.gameObject.AddComponent<Buoy>();
            buoy.draft = .1f; buoy.tilt = Game.Has(1, 3) ? .35f : .8f;
            ResetBoat();
            for (int i = 0; i < 5; i++)
            {
                if ((C.scanSignals[i] & Bit_Density) != 0) scan[i] = 1;
                if ((C.scanSignals[i] & Bit_Thermal) != 0) thermal[i] = 1;
            }
            BuildCloud();
            BuildThermal();
            BuildUI();
            RainFx.Create(cam.transform, 500);
            engineSrc = Sfx.Loop(boat.gameObject, Sfx.Engine, .3f);
            listenSrc = Sfx.Loop(gameObject, Sfx.Hum, 0);
            Sfx.Loop(gameObject, Sfx.Wind, .3f);
            Sfx.Loop(gameObject, Sfx.Drone, .14f);
            toast.Show("Te acercas de noche. A menos de " + WakeRange + " m la estela te delata con luna: reduce la marcha.", Pal.DeckLight, 7);
            RefreshPanel();
        }

        void ResetBoat()
        {
            boat.position = boatStart;
            yaw = Mathf.Atan2(ship.position.x - boatStart.x, ship.position.z - boatStart.z) * Mathf.Rad2Deg;
            buoy.yaw = yaw; camYaw = 0; speed = 0;
        }

        // ---------- Nube de puntos del casco ----------
        void BuildCloud()
        {
            var go = new GameObject("NubeSonar");
            cloud = go.AddComponent<ParticleSystem>();
            cloud.Stop();
            var main = cloud.main;
            main.maxParticles = 40000; main.loop = false; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0;
            var em = cloud.emission; em.enabled = false;
            var sh = cloud.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = Mats.Glow(Color.white, true, true, true);
            cloud.Play();
        }

        void EmitPoint(Vector3 p, Color c, float size = .22f, float life = 90f)
        {
            var e = new ParticleSystem.EmitParams { position = p, startColor = c, startSize = size, startLifetime = life, velocity = Vector3.zero, applyShapeToPosition = false };
            cloud.Emit(e, 1);
        }

        Vector3 SamplePoint(Compartment k, System.Random rnd)
        {
            float R() => (float)rnd.NextDouble() - .5f;
            var half = k.size;
            if (k.kind == 0)
            {
                // Masa densa y homogénea: rejilla de cajas.
                var v = new Vector3(R() * half.x, R() * half.y, R() * half.z);
                v = new Vector3(Mathf.Round(v.x / .9f) * .9f, Mathf.Round(v.y / .9f) * .9f, Mathf.Round(v.z / .9f) * .9f);
                return ship.TransformPoint(k.center + v);
            }
            if (k.kind == 2)
            {
                // Maquinaria: grandes cilindros.
                var c = k.center + new Vector3(0, -1, -3 + 6 * (rnd.Next(2)));
                float a = (float)rnd.NextDouble() * 6.28f;
                return ship.TransformPoint(c + new Vector3(Mathf.Cos(a) * 1.8f, R() * 5f, Mathf.Sin(a) * 1.8f));
            }
            // Espacio habitable: paredes y objetos dispersos.
            if (rnd.NextDouble() < .55f)
            {
                var v = new Vector3(R() * half.x, R() * half.y, R() * half.z);
                switch (rnd.Next(3))
                {
                    case 0: v.x = Mathf.Sign(v.x) * half.x * .5f; break;
                    case 1: v.z = Mathf.Sign(v.z) * half.z * .5f; break;
                    default: v.y = Mathf.Sign(v.y) * half.y * .5f; break;
                }
                return ship.TransformPoint(k.center + v);
            }
            var f = personFoci[rnd.Next(personFoci.Count)];
            return ship.TransformPoint(f + new Vector3(R() * .7f, R() * 1.6f, R() * .7f));
        }

        // ---------- Vista térmica ----------
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
            for (int i = 0; i < 4; i++)
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
        }

        void EndDrone(bool lost)
        {
            droneActive = false;
            thermalRoot.SetActive(false);
            cam.clearFlags = savedClear; cam.backgroundColor = savedBg; cam.cullingMask = savedMask;
            float away = Vector3.Distance(drone.position, boat.position);
            if (lost || away > 220f)
            {
                bool recovered = Random.value < .5f;
                if (!recovered) Game.S.droneParts--;
                toast.Show(recovered ? "Batería agotada lejos de la lancha, pero recuperas el dron en el mar." : "Dron perdido en el mar: -1 repuesto.", Pal.DeckLight);
            }
        }

        // ---------- UI ----------
        void BuildUI()
        {
            var canvas = UI.NewCanvas();
            var left = UI.Panel(canvas.transform, new Vector2(0, .66f), new Vector2(.24f, 1));
            var ls = UI.VStack(left.transform, 14, 7);
            UI.Line(ls, "FASE 2 · ESCANEO", 26, Pal.Sonar, true);
            status = UI.Line(ls, "", 20);
            UI.Line(ls, "Sospecha del buque", 18, Pal.Dim);
            suspBar = UI.NewBar(ls, Pal.Danger, 12);
            crestText = UI.Line(ls, "Ola", 18, Pal.Dim);
            crestBar = UI.NewBar(ls, Pal.Sonar, 18);
            UI.Line(ls, "W/S marcha · A/D timón · Espacio pulso · H hidrófono · T dron · Clic der. cámara", 16, Pal.Dim);

            var right = UI.Panel(canvas.transform, new Vector2(.68f, .38f), new Vector2(1, 1));
            var rs = UI.VStack(right.transform, 12, 5);
            UI.Line(rs, "Lectura del casco", 24, Pal.Sonar, true);
            UI.Line(rs, "Distingue bodegas de compartimentos con personas. Frío uniforme y masa densa son carga; calor, voces y consumo de agua, no.", 16, Pal.Dim);
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                var row = UI.HStack(rs, 36);
                selectBtn[i] = UI.Btn(row, CaseData.Compartments[i].name, () => { selected = k; RefreshPanel(); }, null, 18, 36);
                classBtn[i] = UI.Btn(row, "¿?", () => CycleClass(k), Pal.DeckLight, 18, 36);
                scanBars[i] = UI.NewBar(rs, Pal.Sonar, 5);
                cardText[i] = UI.Line(rs, "", 15);
            }
            UI.Line(rs, "Hidrófono sobre el compartimento elegido", 15, Pal.Dim);
            listenBar = UI.NewBar(rs, Pal.ThermHot, 6);
            confidenceText = UI.Line(rs, "", 22, Pal.Text, true);
            confBar = UI.NewBar(rs, Pal.Sealed, 12);
            var bottom = UI.HStack(rs, 46);
            board = UI.Btn(bottom, "Abordar: Fase 3", () => Game.GoToPhase(3), Pal.Sealed, 21, 46);
            UI.Btn(bottom, "Volver a base", Game.GoToMenu, Pal.Dim, 19, 46);
            toast = UI.Toasts.Create(canvas.transform, new Vector2(.05f, .03f), new Vector2(.62f, .13f));

            // Retícula de pulso: mínima, el objetivo es siempre el buque.
            var hint = UI.Text(canvas.transform, "", 20, Pal.Sonar, TextAlignmentOptions.Center);
            hint.rectTransform.anchorMin = new Vector2(.1f, .15f); hint.rectTransform.anchorMax = new Vector2(.6f, .2f);
        }

        void CycleClass(int i)
        {
            C.classified[i] = C.classified[i] >= 2 ? 0 : C.classified[i] + 1;
            RefreshPanel();
        }

        void RefreshPanel()
        {
            for (int i = 0; i < 5; i++)
            {
                var k = CaseData.Compartments[i];
                int sig = C.scanSignals[i];
                string dens = (sig & Bit_Density) != 0 ? k.density : scan[i] > 0 ? $"{scan[i] * 100:0} %" : "—";
                string th = (sig & Bit_Thermal) != 0 ? $"{k.temp}. {k.cycle}. Agua: {k.water}" : thermal[i] > 0 ? $"{thermal[i] * 100:0} %" : "—";
                string sn = (sig & Bit_Sound) != 0 ? k.sound : "—";
                cardText[i].text = $"<color=#3FE0D0>Densidad</color> {dens}  <color=#FFD27A>Térmica</color> {th}  <color=#9fb>Sonido</color> {sn}";
                scanBars[i].Set(scan[i]);
                UI.SetLabel(classBtn[i], C.classified[i] < 0 ? "Sin clasificar" : CaseData.KindNames[C.classified[i]]);
                UI.SetLabel(selectBtn[i], (selected == i ? "> " : "") + k.name);
            }
            C.confidence = CaseData.ComputeConfidence(C.classified, C.scanSignals);
            confidenceText.text = $"Confianza: {C.confidence:0} % (mínimo {CaseData.ConfidenceToBoard} %)";
            confBar.Set(C.confidence / 100f);
            board.interactable = C.confidence >= CaseData.ConfidenceToBoard;
        }

        // ---------- Bucle ----------
        void Update()
        {
            Ocean.DriftDouglas(4, 6.2f, 5f);
            var kb = Keyboard.current;
            pulseCooldown -= Time.deltaTime;
            float d = DistToShip;

            if (droneActive) DriveDrone(kb); else DriveBoat(kb);

            // Pulso
            if (kb != null && kb.spaceKey.wasPressedThisFrame && !droneActive && pulseCooldown <= 0) Pulse(d);

            // Hidrófono
            bool hearing = kb != null && kb.hKey.isPressed && !droneActive;
            if (hearing)
            {
                if (d > HydroRange) { if (kb.hKey.wasPressedThisFrame) toast.Show($"Hidrófono fuera de alcance: acércate a {HydroRange} m.", Pal.DeckLight); }
                else
                {
                    var k = CaseData.Compartments[selected];
                    var clip = k.kind == 1 ? Sfx.Voices : k.kind == 2 ? Sfx.Engine : Sfx.Hum;
                    if (listenSrc.clip != clip) { listenSrc.clip = clip; listenSrc.Play(); }
                    listenSrc.volume = .8f;
                    listen += Time.deltaTime / 3f;
                    if (listen >= 1) { C.scanSignals[selected] |= Bit_Sound; listen = 0; toast.Show($"{k.name}: {k.sound}.", Pal.ThermHot); RefreshPanel(); }
                }
            }
            else { listen = Mathf.Max(0, listen - Time.deltaTime); listenSrc.volume = 0; }
            listenBar.Set(listen);
            engineSrc.volume = hearing ? .05f : .3f;

            // Sospecha
            float rate = -2f;
            float factor = Game.Has(1, 4) ? .5f : 1f;
            if (d < WakeRange && Mathf.Abs(speed) > 2.5f) rate = 8f * Mathf.Abs(speed) / MaxSpeed * factor;
            if (droneActive && Vector3.Distance(drone.position, ship.position) < 22f) rate = Mathf.Max(rate, 3f * factor);
            C.suspicion = Mathf.Clamp(C.suspicion + rate * Time.deltaTime, 0, 100);
            if (C.suspicion >= 100) Detected();

            // HUD
            suspBar.Set(C.suspicion / 100f);
            float sw = Ocean.Swell(boat.position.x, boat.position.z);
            crestBar.Set((sw + 1) * .5f);
            bool onCrest = Ocean.SecondsFromCrest(boat.position.x, boat.position.z) <= CrestWindow;
            crestBar.img.color = onCrest ? Pal.Sealed : Pal.Sonar;
            crestText.text = onCrest ? "<color=#6CFF8A>EN LA CRESTA: ¡pulso a pleno rendimiento!</color>" : "Ola: el pulso rinde más en la cresta";
            status.text = $"Distancia al buque: <b>{d:0} m</b>\nMarcha: {Mathf.Abs(speed):0} m/s\nCombustible: {Game.S.fuel:0} L\nRepuestos de dron: {Game.S.droneParts}\nDouglas: {Ocean.Douglas:0.0}{(droneActive ? $"\n<color=#FFD27A>Batería del dron: {droneBattery:0} s. T: volver</color>" : "")}";
        }

        void DriveBoat(Keyboard kb)
        {
            float throttle = 0, rudder = 0;
            if (kb != null)
            {
                if (kb.wKey.isPressed) throttle += 1;
                if (kb.sKey.isPressed) throttle -= .5f;
                if (kb.aKey.isPressed) rudder -= 1;
                if (kb.dKey.isPressed) rudder += 1;
                if (kb.tKey.wasPressedThisFrame) ToggleDrone();
            }
            speed = Mathf.MoveTowards(speed, throttle * MaxSpeed, Time.deltaTime * 6f);
            yaw += rudder * Time.deltaTime * 45f * Mathf.Clamp01(Mathf.Abs(speed) / 4f + .2f);
            buoy.yaw = yaw;
            boat.position += Quaternion.Euler(0, yaw, 0) * Vector3.forward * speed * Time.deltaTime;
            engineSrc.pitch = .7f + Mathf.Abs(speed) / MaxSpeed;

            if (Mathf.Abs(throttle) > 0)
            {
                Game.S.fuel -= Time.deltaTime * 3f * Mathf.Abs(throttle) * (Game.Has(1, 2) ? .7f : 1f);
                if (Game.S.fuel <= 0) Tow();
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var md = mouse.delta.ReadValue();
                camYaw += md.x * .2f; camPitch = Mathf.Clamp(camPitch - md.y * .15f, 4, 50);
            }
            var rot = Quaternion.Euler(camPitch, yaw + camYaw, 0);
            var targetPos = boat.position + Vector3.up * 2f + rot * Vector3.back * camDist;
            targetPos.y = Mathf.Max(targetPos.y, Ocean.Height(targetPos.x, targetPos.z) + 1f);
            cam.transform.SetPositionAndRotation(targetPos, rot);
        }

        void DriveDrone(Keyboard kb)
        {
            droneBattery -= Time.deltaTime;
            if (droneBattery <= 0 || Ocean.Douglas >= 6) { EndDrone(true); return; }
            Vector3 move = Vector3.zero;
            float turn = 0;
            if (kb != null)
            {
                if (kb.wKey.isPressed) move += Vector3.forward;
                if (kb.sKey.isPressed) move += Vector3.back;
                if (kb.aKey.isPressed) turn -= 1;
                if (kb.dKey.isPressed) turn += 1;
                if (kb.tKey.wasPressedThisFrame) { EndDrone(false); return; }
            }
            drone.rotation *= Quaternion.Euler(0, turn * 70f * Time.deltaTime, 0);
            drone.position += drone.rotation * move * 30f * Time.deltaTime;
            var p = drone.position; p.y = 30; drone.position = p;
            cam.transform.SetPositionAndRotation(drone.position, drone.rotation * Quaternion.Euler(50, 0, 0));

            // Sobrevolar un compartimento 2 s da la lectura térmica.
            for (int i = 0; i < 5; i++)
            {
                var c = ship.TransformPoint(CaseData.Compartments[i].center);
                float xz = new Vector2(c.x - p.x, c.z - p.z).magnitude;
                if (xz < 16f && (C.scanSignals[i] & Bit_Thermal) == 0)
                {
                    thermal[i] += Time.deltaTime / 2f;
                    if (thermal[i] >= 1) { C.scanSignals[i] |= Bit_Thermal; toast.Show($"{CaseData.Compartments[i].name}: {CaseData.Compartments[i].temp}.", Pal.ThermHot); }
                    RefreshPanel();
                }
            }
        }

        void Pulse(float d)
        {
            pulseCooldown = 1.2f;
            bool crest = Ocean.SecondsFromCrest(boat.position.x, boat.position.z) <= CrestWindow;
            float q = crest ? 1f : Game.Has(1, 3) ? .6f : .35f;
            if (d > PulseRange) { toast.Show($"Fuera de alcance del sonar: acércate a {PulseRange:0} m.", Pal.DeckLight); return; }
            if (Game.Has(0, 2)) q *= 1.5f;
            if (d < 150f)
            {
                C.suspicion = Mathf.Min(100, C.suspicion + (Game.Has(1, 4) ? 6 : 12));
                toast.Show("Pulso a menos de 150 m: el buque lo nota.", Pal.Danger);
            }
            StartCoroutine(Ring());

            // El casco se reconstruye con impactos reales; las bodegas, según el lado desde el que disparas.
            var from = boat.position + Vector3.up * 1.5f;
            for (int i = 0; i < (crest ? 260 : 90); i++)
            {
                var target = ship.TransformPoint(new Vector3(Random.Range(-7f, 7f), Random.Range(-4f, 11f), Random.Range(-46f, 46f)));
                if (Physics.Raycast(from, (target - from).normalized, out var hit, PulseRange + 80f, 1 << Layers.HullShell))
                    EmitPoint(hit.point, Pal.Sonar, .24f);
            }

            var side = Mathf.Sign(ship.InverseTransformPoint(boat.position).x);
            float rangeFactor = d < 220 ? .34f : .22f;
            float dense = 0, total = 0;
            var rnd = new System.Random(Time.frameCount);
            for (int i = 0; i < 5; i++)
            {
                var k = CaseData.Compartments[i];
                float sideF = Mathf.Sign(k.center.x) == side ? 1f : .35f;
                scan[i] = Mathf.Min(1, scan[i] + q * rangeFactor * sideF);
                if (scan[i] >= 1) C.scanSignals[i] |= Bit_Density;
                int points = (int)(320 * q * sideF);
                Color col = k.kind == 0 ? new Color(.25f, .62f, .78f) : k.kind == 1 ? Pal.Sonar : new Color(.3f, .7f, .65f);
                for (int n = 0; n < points; n++) EmitPoint(SamplePoint(k, rnd), col, .18f);
                dense += k.densityValue * sideF; total += sideF;
            }
            Sfx.Play(dense / total > .62f ? Sfx.PingDense : Sfx.PingHollow, crest ? .9f : .5f);
            if (crest) toast.Show("Pulso en la cresta.", Pal.Sealed, 1.2f);
            RefreshPanel();
        }

        IEnumerator Ring()
        {
            var go = new GameObject("PulsoRing");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = Mats.Glow(Pal.Sonar);
            lr.loop = true; lr.positionCount = 64; lr.widthMultiplier = 1f;
            var c = boat.position;
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                float r = t * 300f;
                for (int i = 0; i < 64; i++)
                {
                    float a = i * Mathf.PI * 2 / 64;
                    float x = c.x + Mathf.Cos(a) * r, z = c.z + Mathf.Sin(a) * r;
                    lr.SetPosition(i, new Vector3(x, Ocean.Height(x, z) + .4f, z));
                }
                lr.material.SetColor("_Color", new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, 1 - t / 1.6f));
                yield return null;
            }
            Destroy(go);
        }

        void Detected()
        {
            C.suspicion = 0;
            C.hours += 6;
            if (!C.dumped)
            {
                C.dumped = true;
                toast.Show("¡Te detectan! El buque entra en alerta y tira pruebas al mar. Vuelves a distancia segura, 6 h después.", Pal.Danger, 7);
            }
            else toast.Show("¡Te detectan otra vez! Te retiras a distancia segura, 6 h después.", Pal.Danger, 6);
            Sfx.Play(Sfx.Alarm, .7f);
            if (droneActive) EndDrone(false);
            ResetBoat();
            Game.Write();
        }

        void Tow()
        {
            Game.S.fuel = 400;
            Game.AddBudget(-10000);
            C.hours += 24;
            toast.Show("Sin combustible: te remolcan. -24 h en el reloj del caso y -10.000 ¢.", Pal.DeckLight, 6);
            ResetBoat();
        }
    }
}
