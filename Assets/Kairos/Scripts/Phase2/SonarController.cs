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

        ParticleSystem cloud, wakeFx;
        AudioSource engineSrc, listenSrc;
        readonly List<Vector3> personFoci = new List<Vector3>();

        // Dron
        bool droneActive; Transform drone; float droneBattery; GameObject thermalRoot;
        const int ThermalLayer = 10;
        Color savedBg; CameraClearFlags savedClear; int savedMask;

        // UI
        TextMeshProUGUI status, confidenceText, crestText, hintText, compassText, leftMiniText, rightMiniText;
        UI.Bar suspBar, crestBar, confBar, listenBar;
        readonly TextMeshProUGUI[] cardText = new TextMeshProUGUI[5];
        readonly Button[] classBtn = new Button[5], selectBtn = new Button[5];
        readonly UI.Bar[] scanBars = new UI.Bar[5];
        Button board, rightMiniBtn;
        UI.Toasts toast;
        Transform canvasRoot;
        GameObject pausePanel, leftPanelObj, leftMiniObj, rightPanelObj, rightMiniObj, compassPanelObj;
        RectTransform hintRect, toastRect, compassRect;
        bool leftCollapsed, rightCollapsed;

        float WakeRange => Game.Has(1, 4) ? 100 : Game.Has(1, 1) ? 200 : 300;
        float PulseRange => Game.Has(0, 4) ? 600 : 350;
        float CrestWindow => Game.Has(0, 1) ? .7f : .5f; // media ventana: 1 s y 1,4 s
        float HydroRange => Game.Has(0, 3) ? 350 : 200;
        float DistToShip => Vector3.Distance(new Vector3(boat.position.x, 0, boat.position.z), new Vector3(ship.position.x, 0, ship.position.z));

        void Start()
        {
            SyncSea();
            SectorTheme.Apply(C.sector, ship);
            buoy = boat.gameObject.AddComponent<Buoy>();
            buoy.draft = .1f; buoy.tilt = Game.Has(1, 3) ? .35f : .8f;
            ResetBoat();
            for (int i = 0; i < 5; i++)
            {
                if ((C.scanSignals[i] & Bit_Density) != 0) scan[i] = 1;
                if ((C.scanSignals[i] & Bit_Thermal) != 0) thermal[i] = 1;
            }
            BuildCloud();
            BuildWake();
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
            seaBase = CaseData.DouglasFor(C.sector, C.hours);
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

        static Texture2D CreateSoftFoamTex()
        {
            const int s = 64;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = (x / (float)(s - 1)) * 2f - 1f;
                    float v = (y / (float)(s - 1)) * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float soft = Mathf.Clamp01(1f - r);
                    soft = soft * soft * (3f - 2f * soft);
                    float froth = .75f + .25f * Mathf.PerlinNoise(x * .22f, y * .22f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, soft * froth));
                }
            t.Apply();
            return t;
        }

        void BuildWake()
        {
            var foamTex = CreateSoftFoamTex();
            var foamMat = Mats.Glow(new Color(.82f, .94f, 1f, .52f), false, false, false, foamTex);

            // Espuma y bruma suave detrás de la popa (textura difuminada, sin triángulos ni cintas)
            var go = new GameObject("EstelaLancha");
            go.transform.SetParent(boat, false);
            go.transform.localPosition = new Vector3(0, .12f, -3.2f);
            wakeFx = go.AddComponent<ParticleSystem>();
            var main = wakeFx.main;
            main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.55f, 1.35f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(.84f, .95f, 1f, .42f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = wakeFx.emission; em.rateOverTime = 0;
            var sh = wakeFx.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(1.4f, .1f, .3f);
            var size = wakeFx.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, .5f, 1, 2.6f));
            var col = wakeFx.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(.9f, .97f, 1f), 0f), new GradientColorKey(new Color(.62f, .82f, .92f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.55f, .15f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = foamMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void EmitPoint(Vector3 p, Color c, float size = .14f, float life = 26f)
        {
            var e = new ParticleSystem.EmitParams { position = p, startColor = c, startSize = size, startLifetime = life, velocity = Vector3.zero, applyShapeToPosition = false };
            cloud.Emit(e, 1);
        }

        /// Dimensiones reales del casco del sector activo (por defecto Hai Feng 77: 14 m manga × 90 m eslora, cubierta a y = 6 m).
        [HideInInspector] public float beamScale = 1f;
        [HideInInspector] public float lengthScale = 1f;
        [HideInInspector] public float hullMinZ = -45f;
        [HideInInspector] public float hullMaxZ = 45f;
        [HideInInspector] public float hullDeckY = 6.2f;

        /// Altura mínima segura sobre las olas para evitar que los puntos X-ray del sonar parezcan flotar sobre el océano.
        public float MinSonarPointY => Mathf.Clamp(Ocean.Amp + 0.95f, 1.55f, Mathf.Max(1.65f, hullDeckY - 2.2f));

        public void SetShipHullBounds(Bounds localHullBounds, float newBeamScale, float newLengthScale)
        {
            beamScale = Mathf.Clamp(newBeamScale, .7f, 1.85f);
            lengthScale = Mathf.Clamp(newLengthScale, .7f, 1.45f);
            hullMinZ = localHullBounds.min.z;
            hullMaxZ = localHullBounds.max.z;
            hullDeckY = Mathf.Clamp(localHullBounds.max.y - 0.8f, 5.8f, 9.6f);
        }

        /// Semimanga conservadora del casco a una posición (localZ, localY), incluyendo el afinamiento en proa, popa y obra viva.
        public static float HullHalfWidthAt(float localZ, float localY, float beamScale, float lengthScale, float hullMinZ, float hullMaxZ, float hullDeckY)
        {
            float baseHalfW = 7f * Mathf.Max(.5f, beamScale);
            float zSpan = Mathf.Max(20f, hullMaxZ - hullMinZ);
            float u = Mathf.Clamp01((localZ - hullMinZ) / zSpan);

            // Afinamiento progresivo en proa (último 35%) y popa (primer 15%) para cualquier casco naval.
            float bowFactor = u > 0.65f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((u - 0.65f) / 0.35f, 2f))) : 1f;
            float sternFactor = u < 0.14f ? Mathf.Lerp(0.76f, 1f, u / 0.14f) : 1f;
            // Afinamiento vertical hacia la línea de flotación.
            float yNorm = Mathf.Clamp01(localY / Mathf.Max(3f, hullDeckY));
            float vertFactor = Mathf.Lerp(0.78f, 0.96f, yNorm);

            return baseHalfW * bowFactor * sternFactor * vertFactor;
        }

        /// Garantiza que cualquier punto local de la nube del sonar quede estrictamente dentro del casco visible sobre el agua.
        public static Vector3 ClampLocalPointInsideHull(Vector3 localPos, float beamScale, float lengthScale, float hullMinZ, float hullMaxZ, float hullDeckY, float minY, float inset = 0.65f)
        {
            float safeMinZ = hullMinZ + 3.2f;
            float safeMaxZ = hullMaxZ - 5.2f;
            if (safeMinZ > safeMaxZ) { float mid = (hullMinZ + hullMaxZ) * .5f; safeMinZ = mid - 2f; safeMaxZ = mid + 2f; }

            float safeMinY = Mathf.Min(minY, hullDeckY - 0.6f);
            float safeMaxY = Mathf.Max(safeMinY + 0.4f, hullDeckY - 0.45f);

            float z = Mathf.Clamp(localPos.z, safeMinZ, safeMaxZ);
            float y = Mathf.Clamp(localPos.y, safeMinY, safeMaxY);
            float maxHalfX = Mathf.Max(0.6f, HullHalfWidthAt(z, y, beamScale, lengthScale, hullMinZ, hullMaxZ, hullDeckY) - inset);
            float x = Mathf.Clamp(localPos.x, -maxHalfX, maxHalfX);
            return new Vector3(x, y, z);
        }

        Vector3 ClampLocalPoint(Vector3 localPos, float inset = 0.65f) =>
            ClampLocalPointInsideHull(localPos, beamScale, lengthScale, hullMinZ, hullMaxZ, hullDeckY, MinSonarPointY, inset);

        /// Ajusta cada compartimento al volumen interior visible (sobre el agua y dentro de la manga/eslora real del sector).
        public static void MapCompartmentBounds(Compartment k, float beamScale, float lengthScale, float hullMinZ, float hullMaxZ, float hullDeckY, float minY, out Vector3 center, out Vector3 size)
        {
            float yBottom = Mathf.Min(minY + 0.15f, hullDeckY - 1.2f);
            float yTop = Mathf.Max(yBottom + 1.2f, hullDeckY - 0.55f);
            float yCenter = (yBottom + yTop) * 0.5f;
            float yHeight = Mathf.Max(1.1f, yTop - yBottom);

            float xScale = Mathf.Lerp(1f, Mathf.Clamp(beamScale, 0.78f, 1.35f), 0.75f);
            float zCenterOffset = (hullMinZ + hullMaxZ) * 0.5f;
            float zScale = Mathf.Clamp(lengthScale, 0.8f, 1.15f);

            float mappedZ = zCenterOffset + k.center.z * zScale;
            float mappedZSize = Mathf.Min(k.size.z * zScale, (hullMaxZ - hullMinZ) * 0.28f);
            float halfW = HullHalfWidthAt(mappedZ, yCenter, beamScale, lengthScale, hullMinZ, hullMaxZ, hullDeckY) - 0.85f;
            float mappedX = Mathf.Clamp(k.center.x * xScale, -Mathf.Max(1.2f, halfW * 0.58f), Mathf.Max(1.2f, halfW * 0.58f));
            float mappedXSize = Mathf.Clamp(k.size.x * 0.78f * xScale, 1.8f, Mathf.Max(2.0f, halfW * 0.85f));

            center = new Vector3(mappedX, yCenter, mappedZ);
            size = new Vector3(mappedXSize, yHeight, mappedZSize);
        }

        void MapCompartment(Compartment k, out Vector3 center, out Vector3 size) =>
            MapCompartmentBounds(k, beamScale, lengthScale, hullMinZ, hullMaxZ, hullDeckY, MinSonarPointY, out center, out size);

        Vector3 SamplePoint(Compartment k, System.Random rnd)
        {
            float R() => (float)rnd.NextDouble() - .5f;
            MapCompartment(k, out var center, out var size);
            Vector3 local;
            if (k.kind == 0)
            {
                // Masa densa y homogénea: rejilla de cajas estrictamente dentro de la bodega.
                var v = new Vector3(R() * size.x, R() * size.y, R() * size.z);
                v = new Vector3(Mathf.Round(v.x / .75f) * .75f, Mathf.Round(v.y / .75f) * .75f, Mathf.Round(v.z / .75f) * .75f);
                local = center + v;
            }
            else if (k.kind == 2)
            {
                // Maquinaria: grandes cilindros dentro de la sala de máquinas.
                var c = center + new Vector3(0, -0.2f, -2.5f + 5f * rnd.Next(2));
                float a = (float)rnd.NextDouble() * 6.28f;
                float r = Mathf.Min(1.5f, size.x * 0.35f);
                local = c + new Vector3(Mathf.Cos(a) * r, R() * size.y * 0.85f, Mathf.Sin(a) * r);
            }
            else if (rnd.NextDouble() < .55f)
            {
                // Espacio habitable: paredes y objetos dispersos.
                var v = new Vector3(R() * size.x, R() * size.y, R() * size.z);
                switch (rnd.Next(3))
                {
                    case 0: v.x = Mathf.Sign(v.x) * size.x * .48f; break;
                    case 1: v.z = Mathf.Sign(v.z) * size.z * .48f; break;
                    default: v.y = Mathf.Sign(v.y) * size.y * .48f; break;
                }
                local = center + v;
            }
            else
            {
                var f = personFoci[rnd.Next(personFoci.Count)];
                local = f + new Vector3(R() * .6f, R() * 1.1f, R() * .6f);
            }
            return ship.TransformPoint(ClampLocalPoint(local, 0.70f));
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
            UI.Line(s, "W/S marcha · A/D timón · Espacio pulso · Tab panel casco · H hidrófono · T dron · V térmica · Clic derecho: cámara", 19, Pal.Dim);
            UI.Btn(s, "Continuar", TogglePause, Pal.Sealed);
            UI.Btn(s, "Volver a la base", Game.GoToMenu, Pal.Dim);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) TogglePause();
            if (pausePanel != null) return;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
            {
                Sfx.Play(Sfx.Click, .35f);
                SetRightCollapsed(!rightCollapsed);
            }
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
            if (leftMiniText != null)
                leftMiniText.text = $"<b>{CaseData.TargetFor(C.sector).name}</b> · {d:0} m · {(onCrest ? "<color=#6CFF8A>OLA</color>" : $"{Mathf.Abs(speed):0} m/s")}";
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
            KeepBoatOutsideShipHull();
            Shader.SetGlobalVector("_BoatPos", new Vector4(boat.position.x, boat.position.y, boat.position.z, Mathf.Abs(speed)));
            Shader.SetGlobalVector("_BoatDir", heading * Mathf.Sign(speed + .001f));
            engineSrc.pitch = .7f + Mathf.Abs(speed) / MaxSpeed;
            if (wakeFx != null) { var em = wakeFx.emission; em.rateOverTime = Mathf.Abs(speed) > 1.2f ? Mathf.Abs(speed) * 4.5f : 0f; }

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

        /// Evita que la lancha atraviese el casco del buque del sector activo (incluye eslora real de S2 y S3).
        void KeepBoatOutsideShipHull()
        {
            if (ship == null) return;
            var local = ship.InverseTransformPoint(boat.position);
            float minZ = hullMinZ - 2.5f;
            float maxZ = hullMaxZ + 2.5f;
            if (local.z <= minZ || local.z >= maxZ) return;
            float zClamped = Mathf.Clamp(local.z, hullMinZ, hullMaxZ);
            float beam = HullHalfWidthAt(zClamped, hullDeckY * 0.7f, beamScale, lengthScale, hullMinZ, hullMaxZ, hullDeckY);
            float halfW = beam + 2.8f;
            if (Mathf.Abs(local.x) >= halfW) return;

            float pushX = halfW - Mathf.Abs(local.x);
            float pushZ = Mathf.Min(local.z - minZ, maxZ - local.z);
            if (pushX < pushZ)
                local.x = (local.x >= 0f ? 1f : -1f) * halfW;
            else
                local.z = (local.z - minZ) < (maxZ - local.z) ? minZ : maxZ;

            var pushed = ship.TransformPoint(local);
            pushed.y = boat.position.y;
            boat.position = pushed;
            speed *= .25f;
        }

        void DriveDrone(Keyboard kb)
        {
            droneBattery -= Time.deltaTime;
            if (droneBattery <= 0 || (Ocean.Douglas >= 6 && !Game.Has(1, 3))) { EndDrone(true); return; }
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
                MapCompartment(CaseData.Compartments[i], out var mc, out _);
                var c = ship.TransformPoint(mc);
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
            if (d < 150f && !Game.Has(0, 4))
            {
                C.suspicion = Mathf.Min(100, C.suspicion + (Game.Has(1, 4) ? 3 : 6));
                toast.Show("Pulso a menos de 150 m: el buque lo nota.", Pal.Danger);
            }
            StartCoroutine(Ring());
            SendMessage("FireSonarPulse", boat.position, SendMessageOptions.DontRequireReceiver);

            // Ping acústico del sonar submarino en sincronía con la onda cian
            Sfx.Play(Sfx.PingAcoustic, crest ? .95f : .65f);

            // Eco metálico retrasado por propagación acústica en agua de mar (~1500 m/s)
            float acousticDelay = Mathf.Clamp(d / 450f, 0.15f, 1.25f);
            StartCoroutine(DelayedEco(acousticDelay, crest));

            // El casco se reconstruye con impactos reales sobre la obra muerta (nunca bajo el agua ni fuera del perfil visible).
            var from = boat.position + Vector3.up * 2.2f;
            var hullCol = new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .50f);
            float minY = MinSonarPointY;
            float maxTargetX = 7f * beamScale * 0.92f;
            for (int i = 0; i < (crest ? 140 : 55); i++)
            {
                var targetLocal = new Vector3(
                    Random.Range(-maxTargetX, maxTargetX),
                    Random.Range(minY, hullDeckY),
                    Random.Range(hullMinZ + 2f, hullMaxZ - 2f));
                var target = ship.TransformPoint(targetLocal);
                if (Physics.Raycast(from, (target - from).normalized, out var hit, PulseRange + 120f, 1 << Layers.HullShell))
                {
                    var localHit = ship.InverseTransformPoint(hit.point);
                    if (localHit.y >= minY - 0.15f && localHit.y <= hullDeckY + 0.5f &&
                        localHit.z >= hullMinZ + 1.0f && localHit.z <= hullMaxZ - 1.0f)
                    {
                        EmitPoint(hit.point, hullCol, .13f, 24f);
                    }
                }
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

                // Estructura tenue del compartimento (bodega fría azul, maquinaria verde-azulado, habitable cian suave)
                int structPoints = (int)(75 * q * sideF);
                Color col = k.kind == 0 ? new Color(.18f, .44f, .78f, .38f)
                    : k.kind == 1 ? new Color(.22f, .78f, .74f, .34f)
                    : new Color(.25f, .60f, .58f, .38f);
                MapCompartment(k, out var mc, out var ms);
                for (int n = 0; n < structPoints; n++)
                {
                    if (k.kind == 1)
                    {
                        // Solo contorno de paredes de K4 dentro de la envolvente real del casco
                        var v = new Vector3(((float)rnd.NextDouble() - .5f) * ms.x, ((float)rnd.NextDouble() - .5f) * ms.y, ((float)rnd.NextDouble() - .5f) * ms.z);
                        switch (rnd.Next(3))
                        {
                            case 0: v.x = Mathf.Sign(v.x) * ms.x * .48f; break;
                            case 1: v.z = Mathf.Sign(v.z) * ms.z * .48f; break;
                            default: v.y = Mathf.Sign(v.y) * ms.y * .48f; break;
                        }
                        EmitPoint(ship.TransformPoint(ClampLocalPoint(mc + v, 0.70f)), col, .11f, 24f);
                    }
                    else
                    {
                        EmitPoint(SamplePoint(k, rnd), col, .11f, 24f);
                    }
                }

                // Exactamente 3 siluetas humanas reconocibles en K4 (cabeza + torso + hombros + piernas sentadas)
                if (k.kind == 1)
                {
                    var bioCol = new Color(Pal.ThermHot.r, Pal.ThermHot.g, Pal.ThermHot.b, .92f);
                    var silhouette = new[]
                    {
                        new Vector3(0f, .62f, 0f),      // cabeza
                        new Vector3(-.18f, .34f, 0f),   // hombro izq
                        new Vector3(.18f, .34f, 0f),    // hombro der
                        new Vector3(0f, .24f, 0f),      // pecho
                        new Vector3(0f, .02f, .04f),    // abdomen
                        new Vector3(-.14f, -.22f, .18f),// rodilla izq
                        new Vector3(.14f, -.22f, .18f), // rodilla der
                        new Vector3(-.12f, -.48f, .28f),// pie izq
                        new Vector3(.12f, -.48f, .28f), // pie der
                    };
                    foreach (var focus in personFoci)
                        foreach (var off in silhouette)
                            EmitPoint(ship.TransformPoint(ClampLocalPoint(focus + off, 0.50f)), bioCol, .19f, 18f);
                }
                dense += k.densityValue * sideF; total += sideF;
            }
            Sfx.Play(dense / total > .62f ? Sfx.PingDense : Sfx.PingHollow, crest ? .9f : .5f);
            if (crest) toast.Show("Pulso en la cresta.", Pal.Sealed, 1.2f);
            RefreshPanel();
        }

        IEnumerator DelayedEco(float delay, bool crest)
        {
            yield return new WaitForSeconds(delay);
            Sfx.Play(Sfx.EcoMetallic, crest ? .85f : .55f);
        }

        IEnumerator Ring()
        {
            LineRenderer MakeRing(float width)
            {
                var go = new GameObject("PulsoRing");
                var lr = go.AddComponent<LineRenderer>();
                var sweepTemplate = Resources.Load<Material>("KSonarRadialSweep");
                var sweepShader = sweepTemplate != null ? sweepTemplate.shader : Shader.Find("Kairos/Pruebas/SonarRadialSweep");
                if (sweepTemplate != null || sweepShader != null)
                {
                    var m = sweepTemplate != null ? new Material(sweepTemplate) : new Material(sweepShader);
                    m.SetColor("_SweepColor", Pal.Sonar);
                    lr.material = m;
                }
                else
                {
                    lr.material = Mats.Glow(Pal.Sonar);
                }
                lr.loop = true; lr.positionCount = 64; lr.widthMultiplier = width;
                return lr;
            }
            var lr1 = MakeRing(1.2f);
            var lr2 = MakeRing(.6f);
            var m1 = lr1.sharedMaterial;
            var m2 = lr2.sharedMaterial;
            var c = boat.position;
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                float r1 = t * 300f, r2 = Mathf.Max(0f, (t - .18f) * 260f);
                for (int i = 0; i < 64; i++)
                {
                    float a = i * Mathf.PI * 2 / 64;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    float x1 = c.x + ca * r1, z1 = c.z + sa * r1;
                    lr1.SetPosition(i, new Vector3(x1, Ocean.Height(x1, z1) + .45f, z1));
                    float x2 = c.x + ca * r2, z2 = c.z + sa * r2;
                    lr2.SetPosition(i, new Vector3(x2, Ocean.Height(x2, z2) + .45f, z2));
                }
                float alpha = 1f - t / 1.6f;
                if (m1 != null) m1.SetColor("_Color", new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, alpha));
                if (m2 != null) m2.SetColor("_Color", new Color(Pal.Sealed.r, Pal.Sealed.g, Pal.Sealed.b, alpha * .65f));
                yield return null;
            }
            Destroy(lr1.gameObject);
            Destroy(lr2.gameObject);
            if (m1 != null) Destroy(m1);
            if (m2 != null) Destroy(m2);
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
