using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kairos
{
    /// Fase 3: abordaje, sigilo, pruebas y las tres salidas. Condición de paso: al menos 1 prueba A.
    /// El barco es estático (riesgo del GDD): mar y cielo se mueven; la IA pisa cubierta fija.
    [DefaultExecutionOrder(-50)]
    public class InfiltrationController : MonoBehaviour
    {
        public static InfiltrationController I { get; private set; }
        public enum Mode { Boarding, Play, Dialog, Menu, Result }

        public Camera cam;
        public PlayerAgent player;
        public NavMeshSurface surface;
        public GuardAI[] guards;
        public GuardAI captain;
        public PersonNpc[] people;
        public EvidenceProp[] evidenceProps;
        public LockedDoor[] doors;
        public Light[] dangerLights;
        public Transform boat, compressorRoom;

        public Mode mode = Mode.Boarding;
        public bool Running => mode == Mode.Play;
        public bool PlayerCanAct => mode == Mode.Play;
        public bool codeKnown;

        static CaseState C => Game.C;
        public readonly List<PersonNpc> Followers = new List<PersonNpc>();
        float jamT, distractT, creakT, heelT, nextHeel = 22f, nextCreak = 12f, lockT, burnT, patrolT = -1, visorT, visorCd, pathT, hintT;
        string hintId;
        Transform exitZone, objTarget;
        string objective = "";
        NavMeshPath path;
        LineRenderer routeLine;

        public bool JamActive => jamT > 0;
        public bool Distracted => distractT > 0;
        public bool StepsMasked => creakT > 0;
        public bool Lockdown => lockT > 0;
        public float HeelAngle => heelT > 0 ? 17f : 0f;
        public float WindKnots => 18f + Ocean.Douglas * 3.5f;

        // UI
        Canvas canvas;
        RectTransform canvasRect, objMarker;
        TextMeshProUGUI objectives, stateText, tools, prompt, compass, lockText, envText, breathText, hintText, objDistance;
        UI.Bar suspBar, holdBar, focusBar, staminaBar;
        GameObject holdRoot, reticle, boardingRoot, hintRoot;
        GameObject[] hudPanels;
        TextMeshProUGUI boardingText, framedText;
        UI.Bar boardBar;
        Image visorTint, fade;
        RectTransform overlay, marksRoot;
        UI.Toasts toast, saveToast;
        AudioSource rainSrc;
        ParticleSystem rainFx;

        class Mark { public Transform t; public RectTransform r; public Image dot; public TextMeshProUGUI label; public string name; public Color color; public GuardAI guard; public float y; }
        readonly List<Mark> marks = new List<Mark>();

        void Awake()
        {
            I = this;
            surface.BuildNavMesh();
        }

        void Start()
        {
            path = new NavMeshPath();
            exitZone = FindAnyObjectByType<ExitZone>().transform;
            ApplySavedState();
            BuildUI();
            BuildRoute();
            Sfx.Loop(gameObject, Sfx.Wind, .35f);
            rainSrc = Sfx.Loop(gameObject, Sfx.Rain, .25f);
            Sfx.Loop(gameObject, Sfx.Drone, .12f);
            if (compressorRoom != null) Sfx.Loop(compressorRoom.gameObject, Sfx.Hum, .9f, 1, 22);
            foreach (var p in people)
                if (!p.Def.startsFree && p.gameObject.activeSelf)
                    Sfx.Loop(p.gameObject, Sfx.Voices, .35f, 1, 9);
            rainFx = RainFx.Create(cam.transform, 900);
            if (boat != null) boat.gameObject.AddComponent<Buoy>().tilt = .8f;

            if (C.boarded && C.hasCheckpoint)
            {
                player.Teleport(new Vector3(C.cpX, C.cpY, C.cpZ));
                EnterPlay("Sigues a bordo, en tu último punto seguro.");
            }
            else EnterBoarding();
        }

        void ApplySavedState()
        {
            foreach (var d in doors) if (C.openDoors.Contains(d.doorId)) d.Open(false);
            if (C.openDoors.Count > 0) RebuildNav();
            foreach (var e in evidenceProps)
            {
                bool dumped = C.dumped && System.Array.IndexOf(CaseData.DumpedWhenDetected, e.evidenceId) >= 0;
                if (dumped && !C.burned.Contains(e.evidenceId)) C.burned.Add(e.evidenceId);
                if (C.burned.Contains(e.evidenceId) || Game.HasEvidence(e.evidenceId)) e.gameObject.SetActive(false);
            }
            foreach (var p in people)
            {
                var s = p.State;
                s.following = false;
                if (s.rescued) p.gameObject.SetActive(false);
            }
            if (Game.S.networkReorganized) foreach (var g in guards) g.viewAngle += 10;
        }

        public void RebuildNav() => surface.UpdateNavMesh(surface.navMeshData);
        public void Toast(string msg, Color? c = null, float seconds = 3.5f) => toast.Show(msg, c, seconds);
        public string Gps() => "45°24'S 59°12'O";
        public bool CaptainAwake() => captain != null && !captain.IsStunned;
        public bool OnWetDeck(Vector3 p) => p.y > 5.5f && (p.y > 8.5f || Mathf.Abs(p.x) > 5.2f || p.z < -20.2f || p.z > 2.2f);

        public bool InDangerLight(Vector3 p)
        {
            if (p.y < 5.5f) return false; // los focos de cubierta no llegan bajo cubierta
            foreach (var l in dangerLights)
            {
                if (l == null) continue;
                float r = Mathf.Min(l.range, 16f); // el foco del mástil alcanza 40 m, pero no marca todo el barco
                if ((l.transform.position - p).sqrMagnitude < r * r * .45f) return true;
            }
            return false;
        }

        public void EmitNoise(Vector3 pos, float radius)
        {
            if (WindKnots > 30f) radius *= .5f; // viento: tus pasos suenan la mitad para la IA
            foreach (var g in guards) g.Hear(pos, radius);
        }

        // ---------- Abordaje ----------
        void EnterBoarding()
        {
            mode = Mode.Boarding;
            boardingRoot.SetActive(true);
            foreach (var h in hudPanels) h.SetActive(false);
            player.gameObject.SetActive(false);
            cam.fieldOfView = 58f;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void UpdateBoarding()
        {
            var bp = boat.position;
            float h = Ocean.Height(bp.x, bp.z);
            cam.transform.position = new Vector3(bp.x + 7f, h + 3f, bp.z - 13f);
            cam.transform.LookAt(new Vector3(0, 6f + h * .1f, -44f));
            float sw = Ocean.Swell(bp.x, bp.z);
            float halfWindow = (Ocean.Douglas >= 7 ? 1.5f : 3.5f) * .5f;
            bool inWindow = Ocean.SecondsFromTrough(bp.x, bp.z) <= halfWindow;
            boardBar.Set((sw + 1) * .5f);
            boardBar.img.color = inWindow ? Pal.Sealed : Pal.DeckLight;
            boardingText.text = $"<b>Abordaje por popa</b>\nLanza el garfio cuando la barra esté en verde: la ola baja y nadie mira hacia abajo.\n\n{(inWindow ? "<color=#6CFF8A>AHORA: pulsa E</color>" : "<color=#F2A73B>Espera a que la barra se ponga verde…</color>")}";
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                Sfx.Play(Sfx.Grapple, 1f);
                if (inWindow) Boarded();
                else Toast("El garfio resbala. Sin prisa: inténtalo otra vez.", Pal.DeckLight);
            }
        }

        void Boarded()
        {
            C.boarded = true;
            boardingRoot.SetActive(false);
            player.gameObject.SetActive(true);
            EnterPlay("A bordo. Sigue la línea del suelo hasta tu objetivo.");
            StoreCheckpoint();
            Game.Write();
        }

        void EnterPlay(string message)
        {
            boardingRoot.SetActive(false);
            foreach (var h in hudPanels) h.SetActive(true);
            mode = Mode.Play;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            Toast(message, Pal.Sonar, 6);
        }

        // ---------- Puntos seguros ----------
        void StoreCheckpoint()
        {
            var p = player.transform.position;
            C.hasCheckpoint = true; C.cpX = p.x; C.cpY = p.y + .05f; C.cpZ = p.z;
        }

        /// Cada avance real guarda la partida y, si nadie te sigue de cerca, fija aquí el punto seguro.
        public void Progressed()
        {
            bool safe = !Lockdown;
            foreach (var g in guards)
            {
                if (g.IsStunned) continue;
                if ((int)g.state >= (int)GuardState.Busqueda || Vector3.Distance(g.transform.position, player.transform.position) < 6f) safe = false;
            }
            if (safe) { StoreCheckpoint(); saveToast.Show("Punto seguro guardado", Pal.Sealed, 2f); }
            Game.Write();
        }

        /// Te atrapan: vuelves al último punto seguro con todo lo que llevas.
        public void Caught()
        {
            if (mode != Mode.Play) return;
            var cp = new Vector3(C.cpX, C.cpY, C.cpZ);
            Sfx.Play(Sfx.Alarm, .6f);
            player.Teleport(cp);
            foreach (var g in guards) g.Regroup(cp);
            for (int i = 0; i < Followers.Count; i++)
            {
                var a = Followers[i].GetComponent<NavMeshAgent>();
                if (a != null && a.isOnNavMesh) a.Warp(cp + new Vector3(i + 1, 0, -1));
            }
            lockT = 0; distractT = 6f;
            fade.color = Color.black;
            Toast("Te han visto. Te repliegas al último punto seguro con todo lo que llevabas.", Pal.DeckLight, 6);
        }

        // ---------- Objetivo y ruta ----------
        EvidenceProp Prop(string id) => System.Array.Find(evidenceProps, e => e.evidenceId == id);
        bool Pending(string id) { var e = Prop(id); return e != null && e.gameObject.activeSelf && !Game.HasEvidence(id); }

        /// El camino mínimo para llegar al tribunal con la cadena completa y, al menos, una Orden de Retención.
        void UpdateObjective()
        {
            objTarget = exitZone;
            objective = "Vuelve a popa y baja a la lancha con quienes te siguen";
            if (Pending("e_facturas"))
            {
                if (DoorFirst("maquinas", "Abre la reja de la sala de máquinas, en el pasillo inferior")) return;
                objective = "Fotografía las facturas en la sala de máquinas"; objTarget = Prop("e_facturas").transform; return;
            }
            if (DoorFirst("babor", "Abre la escotilla de babor, en el pasillo inferior")) return;
            if (Pending("e_celda")) { objective = "Fotografía el compartimento oculto"; objTarget = Prop("e_celda").transform; return; }
            foreach (var n in people)
                if (n.personId != "rodel" && !n.State.rescued && !n.State.following) { objective = $"Habla con {n.Def.name} y ofrécele salir contigo"; objTarget = n.transform; return; }
            if (Pending("e_manifiesto"))
            {
                if (DoorFirst("oficial", "Abre la puerta del camarote del oficial, en la habilitación")) return;
                objective = "Fotografía el manifiesto en el camarote del oficial"; objTarget = Prop("e_manifiesto").transform; return;
            }
            var rodel = System.Array.Find(people, n => n.personId == "rodel");
            if (rodel != null && !rodel.State.rescued && !rodel.State.following) { objective = "Habla con Rodel Santos, en el comedor"; objTarget = rodel.transform; }
        }

        /// Lo que hay tras una puerta cerrada no tiene ruta: primero, la puerta.
        bool DoorFirst(string id, string text)
        {
            if (C.openDoors.Contains(id)) return false;
            objective = text;
            objTarget = System.Array.Find(doors, d => d.doorId == id).transform;
            return true;
        }

        void BuildRoute()
        {
            var go = new GameObject("Ruta");
            routeLine = go.AddComponent<LineRenderer>();
            routeLine.material = Mats.Glow(new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .55f));
            routeLine.widthMultiplier = .07f;
            routeLine.alignment = LineAlignment.TransformZ;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            routeLine.positionCount = 0;
        }

        void UpdateRoute()
        {
            pathT -= Time.deltaTime;
            if (pathT > 0) return;
            pathT = .3f;
            routeLine.positionCount = 0;
            if (objTarget == null) return;
            if (!NavMesh.SamplePosition(player.transform.position, out var from, 2f, NavMesh.AllAreas)) return;
            // Busca el suelo en la cubierta del objetivo: si no, una prueba alta caería en el piso de arriba.
            var probe = objTarget.position;
            probe.y = probe.y < 5.9f ? 3.1f : probe.y < 8.9f ? 6.1f : 9.2f;
            if (objTarget.GetComponent<LockedDoor>() != null) probe.x = 0; // las puertas se abren desde el pasillo central
            if (!NavMesh.SamplePosition(probe, out var to, 4f, NavMesh.AllAreas)) return;
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path)) return;
            routeLine.positionCount = path.corners.Length;
            for (int i = 0; i < path.corners.Length; i++) routeLine.SetPosition(i, path.corners[i] + Vector3.up * .12f);
        }

        float RouteLength()
        {
            float d = 0;
            for (int i = 1; i < routeLine.positionCount; i++) d += Vector3.Distance(routeLine.GetPosition(i - 1), routeLine.GetPosition(i));
            return d;
        }

        /// Coloca un elemento de la interfaz sobre un punto del mundo. Con clamp, si queda fuera se pega al borde.
        bool Project(RectTransform r, Vector3 world, bool clamp)
        {
            var sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0;
            float x = sp.x / UnityEngine.Screen.width, y = sp.y / UnityEngine.Screen.height;
            if (behind) { x = 1 - x; y = 1 - y; }
            bool offscreen = behind || x < 0 || x > 1 || y < 0 || y > 1;
            if (offscreen && !clamp) return false;
            if (behind) y = 0;
            x = Mathf.Clamp(x, .05f, .95f); y = Mathf.Clamp(y, .1f, .82f); // sin tapar los paneles
            r.anchoredPosition = new Vector2(x * canvasRect.rect.width, y * canvasRect.rect.height);
            return true;
        }

        // ---------- Bucle ----------
        void Update()
        {
            Ocean.DriftDouglas(4, 6.5f, 5f);
            if (mode == Mode.Boarding) { UpdateBoarding(); return; }

            var kb = Keyboard.current;
            if (mode == Mode.Play)
            {
                if (kb != null && kb.tabKey.wasPressedThisFrame) OpenBriefcase();
                if (kb != null && kb.escapeKey.wasPressedThisFrame) OpenPause();
                Tick();
            }
            else if (mode == Mode.Menu || mode == Mode.Dialog)
            {
                if (kb != null && (kb.escapeKey.wasPressedThisFrame || (mode == Mode.Menu && kb.tabKey.wasPressedThisFrame))) CloseOverlay();
            }
            UpdateHud();
        }

        void Tick()
        {
            float dt = Time.deltaTime;
            jamT = Mathf.Max(0, jamT - dt);
            distractT = Mathf.Max(0, distractT - dt);
            creakT = Mathf.Max(0, creakT - dt);
            heelT = Mathf.Max(0, heelT - dt);
            visorT = Mathf.Max(0, visorT - dt);
            visorCd = Mathf.Max(0, visorCd - dt);

            UpdateObjective();
            UpdateRoute();

            // Bajo cubierta no llueve.
            bool outside = OnWetDeck(player.transform.position);
            var rainEmission = rainFx.emission; rainEmission.enabled = outside;
            rainSrc.volume = Mathf.MoveTowards(rainSrc.volume, outside ? .25f : .04f, dt);

            // El oleaje ayuda: la escora distrae 3 s, el crujido tapa tus pasos 2 s.
            nextHeel -= dt;
            if (nextHeel <= 0)
            {
                nextHeel = Random.Range(25f, 50f);
                heelT = 3f; distractT = Mathf.Max(distractT, 3f);
                Sfx.Play(Sfx.Creak, .5f);
                Toast("El barco se inclina: la tripulación se agarra 3 s. ¡Aprovecha!", Pal.Sonar, 3f);
            }
            nextCreak -= dt;
            if (nextCreak <= 0)
            {
                nextCreak = Random.Range(16f, 34f);
                creakT = 2f;
                Sfx.Play(Sfx.Creak, .7f);
            }

            if (Lockdown)
            {
                lockT -= dt; burnT -= dt;
                if (burnT <= 0) { burnT = 45f; BurnOne(); }
                if (lockT <= 0) Toast("Fin del bloqueo: los guardias vuelven a sus rondas.", Pal.Sonar);
            }

            if (patrolT >= 0)
            {
                patrolT -= dt;
                if (patrolT <= 0) Finish(1);
            }

            Followers.RemoveAll(f => !f.State.following);
        }

        // ---------- Estados globales ----------
        public void StartLockdown(string why)
        {
            if (Lockdown) return;
            lockT = 90f; burnT = 45f;
            Sfx.Play(Sfx.Alarm, .9f);
            Toast($"BLOQUEO DE EVIDENCIAS. {why}. Se destruye una prueba cada 45 s.", Pal.Danger, 6);
        }

        void BurnOne()
        {
            foreach (var id in CaseData.BurnOrder)
            {
                if (!Pending(id)) continue;
                var e = Prop(id);
                e.gameObject.SetActive(false);
                C.burned.Add(id);
                Toast($"Prueba destruida: {e.Def.title}.", Pal.Danger, 4);
                Sfx.Play(Sfx.Alarm, .4f);
                return;
            }
        }

        // ---------- Herramientas ----------
        GuardAI GuardInReach()
        {
            GuardAI best = null; float d = 3f;
            foreach (var g in guards)
            {
                if (g.IsStunned) continue;
                float dist = Vector3.Distance(g.transform.position, player.transform.position);
                if (dist < d) { d = dist; best = g; }
            }
            return best;
        }

        /// Q hace lo que toca: aturdir si hay alguien al alcance; si no, bloquear radios y cámaras.
        public void UseTool()
        {
            MarkHint("tool");
            var g = C.stunUsed ? null : GuardInReach();
            if (g != null)
            {
                C.stunUsed = true;
                g.Stun(60f);
                Sfx.PlayAt(Sfx.Stun, g.transform.position, 1f);
                Toast($"{g.displayName} queda fuera de combate 60 s. Sin heridas.", Pal.Sonar);
                return;
            }
            if (JamActive) return;
            if (C.jammerCharges <= 0) { Toast("Bloqueador sin carga.", Pal.DeckLight); return; }
            C.jammerCharges--;
            jamT = 45f;
            Sfx.Play(Sfx.Stun, .4f);
            Toast("Bloqueador activo 45 s: radios cortadas y cámaras ciegas.", Pal.Sonar);
        }

        public void UseVisor()
        {
            if (visorCd > 0) return;
            MarkHint("visor");
            visorT = 7f; visorCd = 14f;
            Sfx.Play(Sfx.PingHollow, .4f);
        }

        void CallPatrol()
        {
            patrolT = 60f;
            StartLockdown("La patrullera aliada alerta al capitán");
            Toast("Patrullera aliada en camino: 60 s. Resiste hasta entonces.", Pal.Sonar, 5);
        }

        public void Photograph(EvidenceProp e)
        {
            Game.SealEvidence(e.evidenceId, Gps());
            MarkHint("photo");
            Sfx.Play(Sfx.Shutter, .9f);
            Sfx.Play(Sfx.Seal, .9f);
            var seal = C.evidence.Find(x => x.id == e.evidenceId);
            Toast($"<color=#6CFF8A>PRUEBA SELLADA ({e.Def.cat})</color>  {e.Def.title}\nSHA-256 {seal.sha.Substring(0, 16)}…", Pal.Text, 4.5f);
            e.gameObject.SetActive(false);
            Progressed();
        }

        // ---------- Personas ----------
        static string TrustWord(int t) => t < 40 ? "Desconfía" : t < 60 ? "Escucha" : t < 80 ? "Quiere hablar" : "Confía en ti";

        public void OpenDialog(PersonNpc npc)
        {
            SetOverlay(Mode.Dialog);
            var p = UI.Panel(overlay, new Vector2(.28f, .12f), new Vector2(.72f, .88f));
            var s = UI.VStack(p.transform, 26, 8);
            var d = npc.Def; var st = npc.State;
            UI.Line(s, d.name, 34, Pal.Sonar, true);
            UI.Line(s, $"{d.origin}. {d.voice}", 20, Pal.Dim);
            UI.Line(s, "Actitud: " + TrustWord(st.trust), 20, Pal.ThermHot);
            var said = UI.Line(s, st.talked ? "" : d.intro, 24);

            void Say(string line) { CloseOverlayKeep(); OpenDialog(npc); lastSaid.text = line; }
            lastSaid = said;

            if (!st.talked)
                UI.Btn(s, "«Soy inspector de KAIROS. No trabajo para esta empresa.»", () =>
                {
                    st.talked = true; st.trust += Game.Has(2, 4) ? 35 : 25;
                    Say(Game.Has(2, 4) ? "(El traductor del casco habla en su idioma.) " + d.intro : d.intro);
                });
            if (st.talked && !st.explained)
                UI.Btn(s, "Explicar el plan de salida y lo que puede pasar", () => { st.explained = true; st.trust += 20; Say("Te escucha sin interrumpir."); });
            if (st.freed && st.trust >= 40)
                UI.Btn(s, st.following ? "«Espera aquí, vuelvo a por ti.»" : "«Salimos juntos. Sigue mis pasos.»", () =>
                {
                    st.following = !st.following;
                    if (st.following && !Followers.Contains(npc)) Followers.Add(npc);
                    Progressed();
                    Say(st.following ? "Asiente y se pone detrás de ti." : "Se queda donde está, vigilando la puerta.");
                }, Pal.Sealed);
            if (st.trust >= 60 && st.testimony == 0)
                UI.Btn(s, "Preguntar si quiere dar testimonio (solo si quiere)", () =>
                {
                    if (!d.willTestify)
                    {
                        st.testimony = 2;
                        Game.AddReputation(2);
                        Say(d.plan + "\n<color=#55666E>Respetas su decisión.</color>");
                        return;
                    }
                    Game.SealEvidence(d.testimonyEvidence, Gps());
                    st.testimony = 1;
                    Game.AddReputation(3);
                    Sfx.Play(Sfx.Seal, .9f);
                    Progressed();
                    Say((Game.S.omitSensitive ? d.testimonySafe : d.testimony) + "\n<color=#6CFF8A>Consiente el lector biométrico. Testimonio sellado.</color>");
                });
            if (st.trust >= 60 && st.explained && d.willTestify && !planHeard.Contains(st.id))
                UI.Btn(s, "«¿Hay algo que deba saber?»", () => { planHeard.Add(st.id); RevealPlan(st); Say(d.plan); });
            if (st.talked && !st.medkit && C.kits > 0)
                UI.Btn(s, $"Ofrecer botiquín ({C.kits})", () => { st.medkit = true; C.kits--; st.trust += 25; Say("Acepta. Por primera vez respira con calma."); });
            if (st.talked && !st.ration && C.rations > 0)
                UI.Btn(s, $"Ofrecer raciones ({C.rations})", () => { st.ration = true; C.rations--; st.trust += 15; Say("Come despacio. Comparte la mitad con quien tiene al lado."); });
            if (st.trust >= 80 && !st.helped)
                UI.Btn(s, "«¿Quieres ayudarme?» (siempre puede decir que no)", () =>
                {
                    st.helped = true;
                    ActiveHelp(npc);
                    Say(d.help);
                });

            UI.Btn(s, "Cerrar (Esc)", CloseOverlay, Pal.Dim);
        }

        TextMeshProUGUI lastSaid;

        readonly HashSet<string> planHeard = new HashSet<string>();

        void RevealPlan(PersonState st)
        {
            if (st.id == "rodel") { codeKnown = true; Toast("Clave de la escotilla de babor: 4-7-1-9. Ya no hace falta cortarla.", Pal.Sealed, 6); }
            if (st.id == "moussa") OpenDoorById("servicio");
        }

        void OpenDoorById(string id)
        {
            foreach (var d in doors) if (d.doorId == id && d.gameObject.activeSelf) { d.Open(true); return; }
        }

        void ActiveHelp(PersonNpc npc)
        {
            switch (npc.personId)
            {
                case "ari":
                    GuardAI nearest = null; float best = 1e9f;
                    foreach (var g in guards)
                    {
                        float dist = Vector3.Distance(g.transform.position, npc.transform.position);
                        if (!g.IsStunned && dist < best) { best = dist; nearest = g; }
                    }
                    if (nearest != null) nearest.Lure(npc.transform.position + Vector3.forward * 3);
                    Sfx.PlayAt(Sfx.Knock, npc.transform.position, 1f);
                    break;
                case "moussa": OpenDoorById("servicio"); break;
                case "rodel": distractT = Mathf.Max(distractT, 3f); nextHeel = Mathf.Min(nextHeel, 6f); break;
                case "thant": creakT = 8f; break;
            }
        }

        // ---------- Maletín, salidas y resultado ----------
        void SetOverlay(Mode m)
        {
            mode = m;
            UI.Clear(overlay);
            overlay.gameObject.SetActive(true);
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void CloseOverlayKeep() { UI.Clear(overlay); }

        public void CloseOverlay()
        {
            UI.Clear(overlay);
            overlay.gameObject.SetActive(false);
            Time.timeScale = 1;
            mode = Mode.Play;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        void OpenBriefcase()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.2f, .1f), new Vector2(.8f, .9f));
            var s = UI.Scroll(p.transform, Vector2.zero, Vector2.one);
            UI.Line(s, $"Maletín sellado: {C.evidence.Count}/{CaseData.MaxEvidence}", 32, Pal.Sealed, true);
            if (C.evidence.Count == 0) UI.Line(s, "Vacío. Acércate a una prueba y mantén pulsado E para fotografiarla.", 22, Pal.Dim);
            foreach (var ev in C.evidence)
            {
                var d = CaseData.Evidence(ev.id);
                UI.Line(s, $"<color=#6CFF8A>[{d.cat}]</color> <b>{d.title}</b>\n{(Game.S.omitSensitive ? d.descSafe : d.desc)}\n<size=15><color=#55666E>{ev.stamp}  ·  SHA-256 {ev.sha.Substring(0, 24)}…</color></size>", 20);
            }
            UI.Line(s, "Otras salidas", 26, Pal.DeckLight, true);
            UI.Line(s, "Lo normal es salir por popa con la lancha. Si te ves en apuros, la patrullera te saca en 60 s, pero alerta al capitán. Si tomas el puente (Q para aturdir al capitán y E en la consola) llevas la mejor baza al tribunal.", 20);
            if (patrolT < 0)
                UI.Btn(s, "Llamar a la patrullera aliada", () => { CloseOverlay(); CallPatrol(); }, Pal.DeckLight, 21, 48);
            UI.Btn(s, "Cerrar (Tab)", CloseOverlay, Pal.Dim);
        }

        void OpenPause()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.34f, .25f), new Vector2(.66f, .75f));
            var s = UI.VStack(p.transform, 26, 10);
            UI.Line(s, "Pausa", 34, Pal.Sonar, true);
            UI.Line(s, "WASD mover · Shift correr · Ratón mirar\nE usar lo que tienes delante\nF visor · Q herramienta · Tab maletín\nClic derecho: fotografiar de lejos", 20, Pal.Dim);
            UI.Btn(s, "Continuar", CloseOverlay, Pal.Sealed);
            UI.Btn(s, "Volver a la base (tu avance queda guardado)", Game.GoToMenu, Pal.Dim);
        }

        public void OpenExitMenu()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.28f, .26f), new Vector2(.72f, .74f));
            var s = UI.VStack(p.transform, 26, 10);
            UI.Line(s, "Salida por popa", 34, Pal.Sonar, true);
            int here = 0;
            foreach (var n in people)
                if (!n.State.rescued && n.State.following && Vector3.Distance(n.transform.position, player.transform.position) < 14f) here++;
            UI.Line(s, $"Te siguen {here} {(here == 1 ? "persona" : "personas")}. Pruebas A selladas: {Game.CountCategoryA()}.", 22);
            if (here > 0)
                UI.Btn(s, $"Bajar a la lancha con {here}", () => { Time.timeScale = 1; Finish(2); }, Pal.Sealed, 22, 52);
            else
                UI.Line(s, "Nadie va contigo. Habla con las personas retenidas y ofréceles salir.", 20, Pal.Dim);
            UI.Btn(s, "Retirarme yo (conservo pruebas; las víctimas quedan pendientes)", () =>
            {
                Time.timeScale = 1;
                ShowResult(0, "Te retiras del buque. Conservas las pruebas selladas. Las víctimas quedan pendientes.");
            }, Pal.DeckLight, 21, 50);
            UI.Btn(s, "Seguir a bordo", CloseOverlay, Pal.Dim);
        }

        public void Finish(int exit)
        {
            if (mode == Mode.Result) return;
            C.exitUsed = exit;
            foreach (var n in people)
            {
                var st = n.State;
                if (st.rescued) continue;
                bool saved = exit == 2
                    ? st.following && Vector3.Distance(n.transform.position, player.transform.position) < 14f
                    : st.freed; // patrullera o puente asegurado
                if (saved) { st.rescued = true; Game.AddReputation(5); }
            }
            string text = exit == 1 ? "La patrullera aliada aborda el buque. Quienes estaban libres salen con ella. El capitán ha tenido tiempo de deshacerse de lo que no sellaste."
                : exit == 2 ? "Bajas a la lancha en silencio con quienes decidieron salir. El buque no se entera hasta el amanecer."
                : "Con el capitán fuera de juego, tomas el puente y fuerzas la entrada a puerto. Es la mayor baza que puedes llevar al tribunal.";
            ShowResult(exit, text);
        }

        void ShowResult(int exit, string text)
        {
            SetOverlay(Mode.Result);
            Time.timeScale = 1;
            player.enabled = false;
            routeLine.positionCount = 0;
            C.boarded = false; C.hasCheckpoint = false; // la próxima incursión empieza por el abordaje
            int a = Game.CountCategoryA();
            var p = UI.Panel(overlay, new Vector2(.22f, .1f), new Vector2(.78f, .9f));
            var s = UI.Scroll(p.transform, Vector2.zero, Vector2.one);
            UI.Line(s, exit == 0 ? "Fuera del buque" : "Salida: " + CaseData.ExitNames[exit], 34, Pal.Sonar, true);
            UI.Line(s, text, 22);
            UI.Line(s, $"Pruebas selladas: {C.evidence.Count}  ·  Categoría A: {a}  ·  Perdidas: {C.burned.Count}", 21, Pal.ThermHot);
            foreach (var n in people)
            {
                var st = n.State;
                UI.Line(s, $"{n.Def.name}: {(st.rescued ? "<color=#6CFF8A>a salvo</color>" : "<color=#F2A73B>pendiente, sigue a bordo</color>")}", 21);
            }
            bool someoneLeft = System.Array.Exists(people, n => !n.State.rescued);
            if (a >= 1)
            {
                UI.Line(s, "Qué sigue: en el tribunal usarás estas pruebas para conectar el buque con su dueño real.", 21, Pal.Dim);
                UI.Btn(s, "Llevar las pruebas al tribunal: Fase 4", () => Game.GoToPhase(4), Pal.Sealed, 24, 56);
            }
            else
                UI.Line(s, "Sin al menos una prueba de categoría A el caso no llega al tribunal: vuelve a abordar.", 21, Pal.Danger);
            if (a == 0 || (exit == 0 && someoneLeft))
                UI.Btn(s, "Volver a abordar (6 h después)", () => { C.hours += 6; Game.GoToPhase(3); }, Pal.DeckLight, 22, 50);
            UI.Btn(s, "Volver a la base", Game.GoToMenu, Pal.Dim, 20, 46);
            Game.Write();
        }

        // ---------- HUD ----------
        void BuildUI()
        {
            canvas = UI.NewCanvas();
            canvasRect = (RectTransform)canvas.transform;
            visorTint = UI.Fullscreen(canvas.transform, new Color(Pal.Sealed.r, Pal.Sealed.g, Pal.Sealed.b, 0));
            visorTint.raycastTarget = false;
            marksRoot = UI.Rect(canvas.transform, "Marcas", Vector2.zero, Vector2.one);

            var tl = UI.Panel(canvas.transform, new Vector2(0, .85f), new Vector2(.34f, 1));
            objectives = UI.Text(tl.transform, "", 21);
            objectives.margin = new Vector4(14, 10, 14, 10);

            var tr = UI.Panel(canvas.transform, new Vector2(.8f, .85f), new Vector2(1, 1));
            var trs = UI.VStack(tr.transform, 12, 5);
            stateText = UI.Line(trs, "", 22, Pal.Sonar, true);
            suspBar = UI.NewBar(trs, Pal.DeckLight, 12);
            envText = UI.Line(trs, "", 15, Pal.Dim);
            breathText = UI.Line(trs, "Aliento", 15, Pal.Dim);
            staminaBar = UI.NewBar(trs, Pal.Sonar, 6);

            var bl = UI.Panel(canvas.transform, new Vector2(0, 0), new Vector2(.5f, .06f));
            tools = UI.Text(bl.transform, "", 18, Pal.Text, TextAlignmentOptions.MidlineLeft);
            tools.margin = new Vector4(14, 2, 14, 2);

            compass = UI.Text(UI.Rect(canvas.transform, "Brujula", new Vector2(.4f, .95f), new Vector2(.6f, 1)), "", 22, Pal.Sonar, TextAlignmentOptions.Center);
            lockText = UI.Text(UI.Rect(canvas.transform, "Lock", new Vector2(.28f, .86f), new Vector2(.72f, .95f)), "", 28, Pal.Danger, TextAlignmentOptions.Center, true);
            prompt = UI.Text(UI.Rect(canvas.transform, "Prompt", new Vector2(.25f, .17f), new Vector2(.75f, .24f)), "", 25, Pal.Text, TextAlignmentOptions.Center);

            var holdR = UI.Rect(canvas.transform, "Hold", new Vector2(.4f, .14f), new Vector2(.6f, .16f));
            holdRoot = holdR.gameObject;
            var hs = UI.VStack(holdR, 0, 0);
            holdBar = UI.NewBar(hs, Pal.Sonar, 14);

            var hintP = UI.Panel(canvas.transform, new Vector2(.28f, .065f), new Vector2(.72f, .135f), default, default, new Color(.02f, .16f, .17f, .92f));
            hintRoot = hintP.gameObject;
            hintText = UI.Text(hintP.transform, "", 22, Pal.Sonar, TextAlignmentOptions.Center);
            hintText.margin = new Vector4(12, 4, 12, 4);

            // Marcador del objetivo
            objMarker = UI.Rect(canvas.transform, "Objetivo", Vector2.zero, Vector2.zero, new Vector2(-70, -30), new Vector2(70, 30));
            var dia = UI.Rect(objMarker, "Rombo", new Vector2(.5f, .62f), new Vector2(.5f, .62f), new Vector2(-9, -9), new Vector2(9, 9));
            dia.localRotation = Quaternion.Euler(0, 0, 45);
            var diaImg = dia.gameObject.AddComponent<Image>(); diaImg.color = Pal.Sonar; diaImg.raycastTarget = false;
            objDistance = UI.Text(UI.Rect(objMarker, "Dist", Vector2.zero, new Vector2(1, .4f)), "", 19, Pal.Sonar, TextAlignmentOptions.Center, true);

            var ret = UI.Rect(canvas.transform, "Reticula", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-180, -130), new Vector2(180, 130));
            reticle = ret.gameObject;
            var frame = ret.gameObject.AddComponent<Outline>();
            var im = ret.gameObject.AddComponent<Image>(); im.color = new Color(1, 1, 1, .02f); im.raycastTarget = false;
            frame.effectColor = Pal.Sealed; frame.effectDistance = new Vector2(2, -2);
            framedText = UI.Text(UI.Rect(ret, "Txt", new Vector2(0, -.25f), new Vector2(1, -.02f)), "", 19, Pal.Sealed, TextAlignmentOptions.Center);
            var fb = UI.Rect(ret, "FocusBar", new Vector2(0, 1.02f), new Vector2(1, 1.06f));
            focusBar = UI.NewBar(fb, Pal.Sealed, 8);

            var bR = UI.Panel(canvas.transform, new Vector2(.28f, .05f), new Vector2(.72f, .34f));
            boardingRoot = bR.gameObject;
            var bs = UI.VStack(bR.transform, 18, 8);
            boardingText = UI.Line(bs, "", 23);
            boardBar = UI.NewBar(bs, Pal.DeckLight, 22);

            toast = UI.Toasts.Create(canvas.transform, new Vector2(.2f, .68f), new Vector2(.8f, .84f));
            saveToast = UI.Toasts.Create(canvas.transform, new Vector2(.72f, .78f), new Vector2(1, .84f));
            fade = UI.Fullscreen(canvas.transform, new Color(0, 0, 0, 0));
            fade.raycastTarget = false;
            overlay = UI.Rect(canvas.transform, "Overlay", Vector2.zero, Vector2.one);
            overlay.gameObject.SetActive(false);

            hudPanels = new[] { tl.gameObject, tr.gameObject, bl.gameObject };
            reticle.SetActive(false); holdRoot.SetActive(false); hintRoot.SetActive(false); objMarker.gameObject.SetActive(false);

            foreach (var g in guards) AddMark(g.transform, g.displayName, Pal.Danger, 2.2f, g);
            foreach (var p in people) AddMark(p.transform, p.Def.name, Pal.ThermHot, 2.1f);
            foreach (var e in evidenceProps) AddMark(e.transform, e.Def.title, Pal.Sealed, .4f);
        }

        void AddMark(Transform t, string name, Color c, float y, GuardAI guard = null)
        {
            var r = UI.Rect(marksRoot, "Marca", Vector2.zero, Vector2.zero, new Vector2(-110, -24), new Vector2(110, 24));
            var d = UI.Rect(r, "Rombo", new Vector2(.5f, .25f), new Vector2(.5f, .25f), new Vector2(-6, -6), new Vector2(6, 6));
            d.localRotation = Quaternion.Euler(0, 0, 45);
            var img = d.gameObject.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
            var label = UI.Text(UI.Rect(r, "Txt", new Vector2(0, .45f), Vector2.one), name, 17, c, TextAlignmentOptions.Center, true);
            r.gameObject.SetActive(false);
            marks.Add(new Mark { t = t, r = r, dot = img, label = label, name = name, color = c, guard = guard, y = y });
        }

        void UpdateMarks()
        {
            bool visor = visorT > 0 && mode == Mode.Play;
            var tint = visorTint.color; tint.a = visor ? .02f : 0; visorTint.color = tint;
            foreach (var m in marks)
            {
                bool show = m.t.gameObject.activeInHierarchy && mode == Mode.Play;
                bool alarm = false;
                float dist2 = (m.t.position - player.transform.position).sqrMagnitude;
                m.label.enabled = dist2 < 225f; // de lejos solo el punto: los nombres se amontonarían
                if (show)
                {
                    if (m.guard != null)
                    {
                        alarm = (int)m.guard.state >= (int)GuardState.Sospecha && !m.guard.IsStunned;
                        show = visor || alarm;
                        var col = m.guard.state == GuardState.Sospecha ? Pal.DeckLight : Pal.Danger;
                        if (!alarm) col = Pal.Sonar;
                        m.dot.color = col; m.label.color = col;
                        m.label.text = !alarm ? m.name : m.guard.state == GuardState.Sospecha ? "?" : "!";
                        m.label.fontSize = alarm ? 34 : 17;
                        if (alarm) m.label.enabled = true;
                    }
                    else show = visor && dist2 < 2500f;
                }
                // Un guardia alerta se ve siempre, aunque esté a tu espalda.
                if (show) show = Project(m.r, m.t.position + Vector3.up * m.y, alarm);
                if (m.r.gameObject.activeSelf != show) m.r.gameObject.SetActive(show);
            }
        }

        // ---------- Tutorial ----------
        void MarkHint(string id) { if (!Game.S.hints.Contains(id)) Game.S.hints.Add(id); }

        void UpdateHint()
        {
            string id = null, text = null;
            void Offer(string i, string t, bool when = true) { if (id == null && when && !Game.S.hints.Contains(i)) { id = i; text = t; } }

            var near = player.Near;
            float nearestGuard = 1e9f;
            foreach (var g in guards) nearestGuard = Mathf.Min(nearestGuard, Vector3.Distance(g.transform.position, player.transform.position));

            if (player.Moved) MarkHint("move");
            if (player.Running) MarkHint("run");
            Offer("move", "Muévete con WASD y mira con el ratón. Sigue la línea del suelo.");
            Offer("photo", "Mantén pulsado E para fotografiar la prueba. Queda sellada en tu maletín.", near is EvidenceProp);
            Offer("hold", "Mantén pulsado E para usar lo que tienes delante.", near != null && near.Hold > 0);
            Offer("talk", "Pulsa E para hablar. Cada persona decide por sí misma.", near is PersonNpc);
            Offer("tool", "Guardia al alcance: pulsa Q para aturdirlo. Solo una vez, y sin heridas.", nearestGuard < 3f && !C.stunUsed);
            Offer("cone", "Las zonas de color son lo que ve cada guardia. Evítalas: si no te ven, no pasa nada.", nearestGuard < 14f);
            Offer("visor", "Pulsa F: el visor muestra guardias, personas y pruebas a través de las paredes.");
            Offer("run", "Mantén Shift para correr. Es más rápido, pero hace ruido.");

            if (id != hintId) { hintId = id; hintT = 0; }
            if (id != null) { hintT += Time.deltaTime; if (hintT > 9f) MarkHint(id); }
            bool show = id != null && mode == Mode.Play;
            if (hintRoot.activeSelf != show) hintRoot.SetActive(show);
            if (show) hintText.text = text;
        }

        static readonly string[] StateNames = { "Nadie te ha visto", "Sospechan algo", "Te están buscando", "¡Te han visto! Huye", "Bloqueo de Evidencias" };

        void UpdateHud()
        {
            GuardState worst = GuardState.Tranquilo; float susp = 0;
            foreach (var g in guards)
            {
                if ((int)g.state > (int)worst) worst = g.state;
                if (g.suspicion > susp) susp = g.suspicion;
            }
            if (Lockdown && worst != GuardState.Alerta) worst = GuardState.Bloqueo;
            stateText.text = StateNames[(int)worst];
            stateText.color = worst == GuardState.Tranquilo ? Pal.Sonar : worst == GuardState.Sospecha ? Pal.DeckLight : Pal.Danger;
            suspBar.Set(susp);
            suspBar.img.color = stateText.color;
            envText.text = StepsMasked ? "<color=#3FE0D0>Crujido del casco: pasos tapados</color>" : Distracted ? "<color=#3FE0D0>Tripulación distraída</color>" : $"Douglas {Ocean.Douglas:0.0} · Viento {WindKnots:0} kn";
            staminaBar.Set(player.Stamina);
            staminaBar.img.color = player.Stamina < .25f ? Pal.Danger : Pal.Sonar;
            breathText.text = player.Stamina < .25f ? "<color=#FF5940>Respiración alta: descansa</color>" : "Aliento";

            string tool = !C.stunUsed && GuardInReach() != null ? "<color=#3FE0D0>[Q] Aturdir</color>"
                : JamActive ? $"<color=#3FE0D0>Bloqueador {jamT:0} s</color>" : $"[Q] Bloqueador ({C.jammerCharges})";
            string visorLabel = visorT > 0 ? $"<color=#6CFF8A>Visor {visorT:0} s</color>" : visorCd > 0 ? $"<color=#55666E>Visor {visorCd:0} s</color>" : "[F] Visor";
            tools.text = $"[E] Usar    {visorLabel}    {tool}    [Tab] Maletín {C.evidence.Count}/{CaseData.MaxEvidence}";

            compass.text = Where(player.transform.position);

            lockText.text = Lockdown ? $"BLOQUEO DE EVIDENCIAS {lockT:0} s" : patrolT >= 0 ? $"PATRULLERA EN {patrolT:0} s" : "";
            if (patrolT >= 0 && Lockdown) lockText.text = $"BLOQUEO {lockT:0} s  ·  PATRULLERA EN {patrolT:0} s";

            bool playing = mode == Mode.Play;
            var near = player.Near;
            prompt.text = playing && near != null && !player.Aiming ? near.Prompt : "";
            bool holding = playing && near != null && near.Hold > 0 && near.progress > 0;
            holdRoot.SetActive(holding && !player.Aiming);
            if (holding) holdBar.Set(near.progress / near.Hold);

            reticle.SetActive(playing && player.Aiming);
            if (player.Aiming)
            {
                focusBar.Set(player.Focus);
                focusBar.img.color = player.Focus >= .8f ? Pal.Sealed : Pal.DeckLight;
                framedText.color = player.FrameBlock != null ? Pal.Danger : Pal.Sealed;
                framedText.text = player.FrameBlock ?? (player.Framed != null ? $"{player.Framed.Def.title}  [{player.Framed.Def.cat}]" : "Sin objetivo");
            }

            int a = Game.CountCategoryA();
            objectives.text = $"<color=#3FE0D0><b>OBJETIVO</b></color>\n<b>{objective}</b>\n<size=17><color=#8A9AA0>Pruebas A: {a}   ·   Te siguen: {Followers.Count}</color></size>";

            // Al llegar sobra el marcador: manda el aviso de lo que puedes hacer.
            bool showMarker = playing && objTarget != null && !player.Aiming && (routeLine.positionCount == 0 || RouteLength() > 2.5f);
            if (showMarker) showMarker = Project(objMarker, objTarget.position + Vector3.up * 1.2f, true);
            if (objMarker.gameObject.activeSelf != showMarker) objMarker.gameObject.SetActive(showMarker);
            if (showMarker) objDistance.text = $"{RouteLength():0} m";

            var fc = fade.color; fc.a = Mathf.MoveTowards(fc.a, 0, Time.unscaledDeltaTime * .9f); fade.color = fc;

            UpdateMarks();
            UpdateHint();
        }

        static string Where(Vector3 p)
        {
            if (p.y < 5.5f) return p.x < -1.3f ? "Cubierta inferior, babor" : p.x > 1.3f ? "Cubierta inferior, estribor" : "Pasillo inferior";
            if (p.y > 8.5f) return "Puente";
            if (Mathf.Abs(p.x) < 5 && p.z > -20 && p.z < 2) return "Habilitación";
            return p.z < -20 ? "Cubierta de popa" : p.z > 2 ? "Cubierta de proa" : "Pasarela";
        }

        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (I == this) I = null; }
    }
}
