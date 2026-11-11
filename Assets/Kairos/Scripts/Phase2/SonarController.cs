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
    public partial class SonarController : MonoBehaviour
    {
        public Camera cam;
        public Transform ship, boat;
        public Vector3 boatStart = new Vector3(-90, 0, -300); // ya a tiro de sonar y por el lado de babor

        const int Bit_Density = 1, Bit_Thermal = 2, Bit_Sound = 4;
        const float MaxSpeed = 18f;

        static CaseState C => Game.C;
        Buoy buoy;
        float speed, yaw, camYaw, camPitch = 14f, camDist = 16f;
        float pulseCooldown, seaBase = 4f;
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
        TextMeshProUGUI status, confidenceText, crestText, hintText, compassText;
        UI.Bar suspBar, crestBar, confBar, listenBar;
        readonly TextMeshProUGUI[] cardText = new TextMeshProUGUI[5];
        readonly Button[] classBtn = new Button[5], selectBtn = new Button[5];
        readonly UI.Bar[] scanBars = new UI.Bar[5];
        Button board;
        UI.Toasts toast;
        Transform canvasRoot;
        GameObject pausePanel;

        float WakeRange => Game.Has(1, 4) ? 100 : Game.Has(1, 1) ? 200 : 300;
        float PulseRange => Game.Has(0, 4) ? 600 : 350;
        float CrestWindow => Game.Has(0, 1) ? .7f : .5f; // media ventana: 1 s y 1,4 s
        float HydroRange => Game.Has(0, 3) ? 350 : 200;
        float DistToShip => Vector3.Distance(new Vector3(boat.position.x, 0, boat.position.z), new Vector3(ship.position.x, 0, ship.position.z));

        void Start()
        {
            SyncSea();
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
            Music.Play("underwater");
            toast.Show("Quédate quieto o ve despacio: a toda marcha, tu estela te delata cerca del buque.", Pal.DeckLight, 7);
            RefreshPanel();
        }

        /// El mar de esta noche es el que marca el reloj del caso.
        void SyncSea()
        {
            seaBase = CaseData.DouglasAt(C.hours);
            Ocean.SetSea(seaBase);
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

        // ---------- Bucle ----------
        void TogglePause()
        {
            bool pause = pausePanel == null;
            Time.timeScale = pause ? 0 : 1;
            if (!pause) { Destroy(pausePanel); return; }
            var panel = UI.Panel(canvasRoot, new Vector2(.34f, .3f), new Vector2(.66f, .7f));
            pausePanel = panel.gameObject;
            var s = UI.VStack(panel.transform, 26, 10);
            UI.Line(s, "Pausa", 34, Pal.Sonar, true);
            UI.Line(s, "W/S marcha · A/D timón · Espacio pulso · H hidrófono · T dron · Clic derecho: cámara", 20, Pal.Dim);
            UI.Btn(s, "Continuar", TogglePause, Pal.Sealed);
            UI.Btn(s, "Volver a la base", Game.GoToMenu, Pal.Dim);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) TogglePause();
            if (pausePanel != null) return;
            Ocean.DriftDouglas(seaBase, seaBase + 2.2f, 5f);
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
            if (d < WakeRange && Mathf.Abs(speed) > 2.5f) rate = 4f * Mathf.Abs(speed) / MaxSpeed * factor;
            if (droneActive && Vector3.Distance(drone.position, ship.position) < 22f) rate = Mathf.Max(rate, 3f * factor);
            C.suspicion = Mathf.Clamp(C.suspicion + rate * Time.deltaTime, 0, 100);
            if (C.suspicion >= 100) Detected();

            // HUD
            hintText.text = CurrentHint(d);
            suspBar.Set(C.suspicion / 100f);
            suspBar.img.color = C.suspicion > 65f ? Pal.Danger : C.suspicion > 30f ? Pal.DeckLight : Pal.Sonar;
            float sw = Ocean.Swell(boat.position.x, boat.position.z);
            crestBar.Set((sw + 1) * .5f);
            bool onCrest = Ocean.SecondsFromCrest(boat.position.x, boat.position.z) <= CrestWindow;
            crestBar.img.color = onCrest ? Pal.Sealed : Pal.Sonar;
            crestText.text = onCrest ? "<color=#6CFF8A>¡AHORA! Pulsa Espacio</color>" : "Ola: el pulso rinde más en verde";

            // Brújula táctica náutica (Alexander - Tarea S12-1)
            if (compassText != null && boat != null && ship != null)
            {
                float boatHeading = (boat.eulerAngles.y % 360f + 360f) % 360f;
                Vector3 toShip = (new Vector3(ship.position.x, 0, ship.position.z) - new Vector3(boat.position.x, 0, boat.position.z)).normalized;
                float bearingToShip = Vector3.SignedAngle(boat.forward, toShip, Vector3.up);
                string side = bearingToShip >= 0 ? "Estribor" : "Babor";
                compassText.text = $"<color=#3FE0D0>RUMBO</color> {boatHeading:000}°  |  <color=#FFD27A>BLANCO</color> {Mathf.Abs(bearingToShip):000}° {side} ({d:0} m)";
            }
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
            var heading = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            boat.position += heading * speed * Time.deltaTime;
            Shader.SetGlobalVector("_BoatPos", new Vector4(boat.position.x, boat.position.y, boat.position.z, Mathf.Abs(speed)));
            Shader.SetGlobalVector("_BoatDir", heading * Mathf.Sign(speed + .001f));
            engineSrc.pitch = .7f + Mathf.Abs(speed) / MaxSpeed;

            if (Mathf.Abs(throttle) > 0)
            {
                Game.S.fuel -= Time.deltaTime * 3f * Mathf.Abs(throttle) * (Game.Has(1, 2) ? .7f : 1f);
                if (Game.S.fuel <= 0) Tow();
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var md = mouse.delta.ReadValue() * Game.S.mouseSens;
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
            pulseCooldown = .9f;
            bool crest = Ocean.SecondsFromCrest(boat.position.x, boat.position.z) <= CrestWindow;
            float q = crest ? 1f : Game.Has(1, 3) ? .85f : .6f;
            if (d > PulseRange) { toast.Show($"Fuera de alcance del sonar: acércate a {PulseRange:0} m.", Pal.DeckLight); return; }
            if (Game.Has(0, 2)) q *= 1.5f;
            if (d < 150f)
            {
                C.suspicion = Mathf.Min(100, C.suspicion + (Game.Has(1, 4) ? 3 : 6));
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
            float rangeFactor = d < 220 ? .4f : .28f;
            float dense = 0, total = 0;
            var rnd = new System.Random(Time.frameCount);
            for (int i = 0; i < 5; i++)
            {
                var k = CaseData.Compartments[i];
                float sideF = Mathf.Sign(k.center.x) == side ? 1f : .6f;
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
            C.hours = Game.NextNight(C.hours + 6);
            SyncSea();
            string back = $"Vuelves a distancia segura: {Game.FormatClock(C.hours)}.";
            if (!C.dumped)
            {
                C.dumped = true;
                toast.Show($"¡Te detectan! Tiran pruebas al mar, entre ellas la planilla de turnos: la Condena queda fuera de alcance. {back}", Pal.Danger, 9);
            }
            else toast.Show($"¡Te detectan otra vez! {back}", Pal.Danger, 6);
            Sfx.Play(Sfx.Alarm, .7f);
            if (droneActive) EndDrone(false);
            ResetBoat();
            Game.Write();
        }

        void Tow()
        {
            Game.S.fuel = 400;
            Game.AddBudget(-10000);
            C.hours = Game.NextNight(C.hours + 24);
            SyncSea();
            toast.Show($"Sin combustible: te remolcan. -24 h en el reloj del caso y -10.000 ¢. Vuelves: {Game.FormatClock(C.hours)}.", Pal.DeckLight, 6);
            ResetBoat();
        }
    }
}
