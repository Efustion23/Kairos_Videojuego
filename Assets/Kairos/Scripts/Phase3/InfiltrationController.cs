using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
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
        float jamT, distractT, creakT, heelT, nextHeel = 22f, nextCreak = 12f, lockT, burnT, patrolT = -1, patrolArm;

        public bool JamActive => jamT > 0;
        public bool Distracted => distractT > 0;
        public bool StepsMasked => creakT > 0;
        public bool Lockdown => lockT > 0;
        public float HeelAngle => heelT > 0 ? 17f : 0f;
        public float WindKnots => 18f + Ocean.Douglas * 3.5f;

        // UI
        Canvas canvas;
        TextMeshProUGUI objectives, stateText, tools, prompt, compass, lockText, envText, breathText;
        UI.Bar suspBar, holdBar, focusBar, staminaBar;
        GameObject holdRoot, reticle, boardingRoot;
        TextMeshProUGUI boardingText, framedText;
        UI.Bar boardBar;
        RectTransform overlay;
        UI.Toasts toast;
        AudioSource rainSrc;
        ParticleSystem rainFx;

        void Awake()
        {
            I = this;
            surface.BuildNavMesh();
        }

        void Start()
        {
            ApplySavedState();
            BuildUI();
            Sfx.Loop(gameObject, Sfx.Wind, .35f);
            rainSrc = Sfx.Loop(gameObject, Sfx.Rain, .25f);
            Sfx.Loop(gameObject, Sfx.Drone, .12f);
            if (compressorRoom != null) Sfx.Loop(compressorRoom.gameObject, Sfx.Hum, .9f, 1, 22);
            foreach (var p in people)
                if (CaseData.PersonDefOf(p.personId).startsFree == false && p.personId != null)
                    Sfx.Loop(p.gameObject, Sfx.Voices, .35f, 1, 9);
            rainFx = RainFx.Create(cam.transform, 900);
            EnterBoarding();
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
            foreach (var l in dangerLights)
                if (l != null && (l.transform.position - p).sqrMagnitude < l.range * l.range * .45f) return true;
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
            player.gameObject.SetActive(false);
            cam.fieldOfView = 58f;
            if (boat != null) boat.gameObject.AddComponent<Buoy>().tilt = .8f;
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
            boardingText.text = $"<b>Abordaje por popa</b>\nLanza el garfio magnético en el seno de la ola: la popa queda cerca y nadie mira abajo.\n\nDouglas {Ocean.Douglas:0.0}. Ventana: {halfWindow * 2:0.0} s.\n{(inWindow ? "<color=#6CFF8A>EN EL SENO: pulsa E</color>" : "<color=#F2A73B>Espera el seno de la ola…</color>")}";
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                Sfx.Play(Sfx.Grapple, 1f);
                if (inWindow) Boarded();
                else
                {
                    Toast("El garfio golpea el casco: se ha oído en cubierta.", Pal.DeckLight);
                    foreach (var g in guards) g.Hear(new Vector3(0, 6, -44), 40f);
                }
            }
        }

        void Boarded()
        {
            C.boarded = true;
            boardingRoot.SetActive(false);
            player.gameObject.SetActive(true);
            mode = Mode.Play;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            Toast("A bordo. Busca el compartimento K4, documenta y habla con quien encuentres.", Pal.Sonar, 6);
            Game.Write();
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

            // Bajo cubierta no llueve.
            bool outside = OnWetDeck(player.transform.position);
            var rainEmission = rainFx.emission; rainEmission.enabled = outside;
            rainSrc.volume = Mathf.MoveTowards(rainSrc.volume, outside ? .25f : .04f, dt);

            // El oleaje ayuda: la escora distrae 3 s, el crujido tapa tus pasos 2 s.
            nextHeel -= dt;
            if (nextHeel <= 0)
            {
                nextHeel = Random.Range(25f, 50f);
                heelT = 3f; distractT = 3f;
                Sfx.Play(Sfx.Creak, .5f);
                Toast("Escora de 17°: la tripulación se agarra durante 3 s.", Pal.Sonar, 3f);
            }
            nextCreak -= dt;
            if (nextCreak <= 0)
            {
                nextCreak = Random.Range(16f, 34f);
                creakT = 2f;
                Sfx.Play(Sfx.Creak, .7f);
                Toast("Crujido del casco: tapa tus pasos 2 s.", Pal.Dim, 2f);
            }

            if (Lockdown)
            {
                lockT -= dt; burnT -= dt;
                if (burnT <= 0) { burnT = 30f; BurnOne(); }
                if (lockT <= 0)
                {
                    foreach (var g in guards) g.suspicion = Mathf.Max(g.suspicion, .6f);
                    Toast("Fin del bloqueo: los guardias siguen buscando.", Pal.DeckLight);
                }
            }

            if (patrolT >= 0)
            {
                patrolT -= dt;
                if (patrolT <= 0) Finish(1);
            }
            patrolArm = Mathf.Max(0, patrolArm - dt);

            Followers.RemoveAll(f => !f.State.following);
        }

        // ---------- Estados globales ----------
        public void StartLockdown(string why)
        {
            if (Lockdown) return;
            lockT = 120f; burnT = 30f;
            Sfx.Play(Sfx.Alarm, .9f);
            Toast($"BLOQUEO DE EVIDENCIAS. {why}. Se destruye una prueba cada 30 s.", Pal.Danger, 6);
        }

        void BurnOne()
        {
            foreach (var e in evidenceProps)
            {
                if (!e.gameObject.activeSelf || Game.HasEvidence(e.evidenceId)) continue;
                e.gameObject.SetActive(false);
                C.burned.Add(e.evidenceId);
                Toast($"Prueba destruida: {e.Def.title}.", Pal.Danger, 4);
                Sfx.Play(Sfx.Alarm, .4f);
                return;
            }
        }

        public void Caught()
        {
            if (mode != Mode.Play) return;
            Sfx.Play(Sfx.Alarm, .8f);
            ShowResult(0, "Te han descubierto y te retiras del buque saltando por la borda. Conservas las pruebas parciales que ya sellaste. Las víctimas quedan pendientes.");
        }

        // ---------- Herramientas ----------
        public void UseJammer()
        {
            if (JamActive) return;
            if (C.jammerCharges <= 0) { Toast("Bloqueador sin carga.", Pal.DeckLight); return; }
            C.jammerCharges--;
            jamT = 45f;
            Sfx.Play(Sfx.Stun, .4f);
            Toast("Bloqueador activo 45 s: radios cortadas y cámaras ciegas.", Pal.Sonar);
        }

        public void UseStunner()
        {
            if (C.stunUsed) { Toast("Aturdidor agotado: un solo uso por misión.", Pal.DeckLight); return; }
            GuardAI best = null; float d = 3f;
            foreach (var g in guards)
            {
                if (g.IsStunned) continue;
                float dist = Vector3.Distance(g.transform.position, player.transform.position);
                if (dist < d) { d = dist; best = g; }
            }
            if (best == null) { Toast("Aturdidor: nadie a menos de 3 m.", Pal.Dim, 2f); return; }
            C.stunUsed = true;
            best.Stun(60f);
            Sfx.PlayAt(Sfx.Stun, best.transform.position, 1f);
            Toast($"{best.displayName} queda fuera de combate 60 s. Sin heridas.", Pal.Sonar);
        }

        public void CallPatrol()
        {
            if (patrolT >= 0) return;
            if (patrolArm <= 0)
            {
                patrolArm = 4f;
                Toast("Llamar a la patrullera es rápido, pero alerta al capitán. Pulsa P otra vez para confirmar.", Pal.DeckLight, 4);
                return;
            }
            patrolT = 60f;
            StartLockdown("La patrullera aliada alerta al capitán");
            Toast("Patrullera aliada en camino: 60 s. Resiste hasta entonces.", Pal.Sonar, 5);
        }

        public void Photograph(EvidenceProp e)
        {
            if (!Game.SealEvidence(e.evidenceId, Gps()))
            {
                Toast("Maletín lleno (12): retira una prueba con Tab.", Pal.DeckLight);
                return;
            }
            Sfx.Play(Sfx.Seal, .9f);
            var seal = C.evidence.Find(x => x.id == e.evidenceId);
            Toast($"<color=#6CFF8A>PRUEBA SELLADA ({e.Def.cat})</color>  {e.Def.title}\nSHA-256 {seal.sha.Substring(0, 16)}…", Pal.Text, 4.5f);
            e.gameObject.SetActive(false);
            Game.Write();
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
                    st.talked = true; st.trust += Game.Has(2, 4) ? 25 : 15;
                    Say(Game.Has(2, 4) ? "(El traductor del casco habla en su idioma.) " + d.intro : d.intro);
                });
            if (st.talked && !st.explained)
                UI.Btn(s, "Explicar el plan de salida y lo que puede pasar", () => { st.explained = true; st.trust += 10; Say("Te escucha sin interrumpir."); });
            if (st.talked && !st.medkit && C.kits > 0)
                UI.Btn(s, $"Ofrecer botiquín ({C.kits})", () => { st.medkit = true; C.kits--; st.trust += 25; Say("Acepta. Por primera vez respira con calma."); });
            if (st.talked && !st.ration && C.rations > 0)
                UI.Btn(s, $"Ofrecer raciones ({C.rations})", () => { st.ration = true; C.rations--; st.trust += 15; Say("Come despacio. Comparte la mitad con quien tiene al lado."); });
            if (st.trust >= 60 && st.explained && d.willTestify && !planHeard.Contains(st.id))
                UI.Btn(s, "«¿Hay algo que deba saber?»", () => { planHeard.Add(st.id); RevealPlan(st); Say(d.plan); });

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
                    if (!Game.SealEvidence(d.testimonyEvidence, Gps())) { Say("<color=#F2A73B>Maletín lleno: retira una prueba con Tab antes de sellar el testimonio.</color>"); return; }
                    st.testimony = 1;
                    Game.AddReputation(3);
                    Sfx.Play(Sfx.Seal, .9f);
                    Say((Game.S.omitSensitive ? d.testimonySafe : d.testimony) + "\n<color=#6CFF8A>Consiente el lector biométrico. Testimonio sellado.</color>");
                });
            if (st.freed && st.trust >= 40)
                UI.Btn(s, st.following ? "«Espera aquí, vuelvo a por ti.»" : "«Salimos juntos. Sigue mis pasos.»", () =>
                {
                    st.following = !st.following;
                    if (st.following && !Followers.Contains(npc)) Followers.Add(npc);
                    Say(st.following ? "Asiente y se pone detrás de ti." : "Se queda donde está, vigilando la puerta.");
                });
            if (st.trust >= 80 && !st.helped)
                UI.Btn(s, "«¿Quieres ayudarme?» (siempre puede decir que no)", () =>
                {
                    st.helped = true;
                    ActiveHelp(npc);
                    Say(d.help);
                }, Pal.Sealed);

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
            if (C.evidence.Count == 0) UI.Line(s, "Vacío. Encuadra un objeto con clic derecho y dispara con clic izquierdo.", 22, Pal.Dim);
            foreach (var ev in C.evidence.ToArray())
            {
                var d = CaseData.Evidence(ev.id);
                UI.Line(s, $"<color=#6CFF8A>[{d.cat}]</color> <b>{d.title}</b>\n{(Game.S.omitSensitive ? d.descSafe : d.desc)}\n<size=15><color=#55666E>{ev.stamp}  ·  SHA-256 {ev.sha.Substring(0, 24)}…</color></size>", 20);
                UI.Btn(s, "Retirar del maletín", () => { C.evidence.Remove(ev); OpenBriefcase(); }, Pal.Dim, 18, 36);
            }
            UI.Btn(s, "Cerrar (Tab)", CloseOverlay, Pal.Dim);
        }

        void OpenPause()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.34f, .25f), new Vector2(.66f, .75f));
            var s = UI.VStack(p.transform, 26, 10);
            UI.Line(s, "Pausa", 34, Pal.Sonar, true);
            UI.Line(s, "WASD mover · Shift correr · C agacharse · E interactuar\nClic der. cámara forense · Clic izq. foto\nQ bloqueador · V aturdidor · P patrullera · Tab maletín", 20, Pal.Dim);
            UI.Btn(s, "Continuar", CloseOverlay, Pal.Sealed);
            UI.Btn(s, "Volver a la base (pierdes el avance de esta incursión)", Game.GoToMenu, Pal.Danger);
        }

        public void OpenExitMenu()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.28f, .22f), new Vector2(.72f, .78f));
            var s = UI.VStack(p.transform, 26, 10);
            UI.Line(s, "Salida por popa", 34, Pal.Sonar, true);
            int here = 0, freed = 0;
            foreach (var n in people)
            {
                if (n.State.rescued) continue;
                if (n.State.freed) freed++;
                if (n.State.following && Vector3.Distance(n.transform.position, player.transform.position) < 14f) here++;
            }
            UI.Line(s, $"Te siguen {here} {(here == 1 ? "persona" : "personas")}. Pruebas A selladas: {Game.CountCategoryA()}.", 22);
            if (here > 0)
            {
                UI.Line(s, "Escoltar en sigilo hasta la lancha: es lento y conserva todas las pruebas.", 20, Pal.Dim);
                UI.Btn(s, $"Bajar a la lancha con {here}", () => { Time.timeScale = 1; Finish(2); }, Pal.Sealed, 22, 52);
            }
            else
                UI.Line(s, "Nadie va contigo. Para escoltar a alguien, gana su confianza y pregúntale si quiere salir.", 20, Pal.Dim);
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
            int a = Game.CountCategoryA();
            var p = UI.Panel(overlay, new Vector2(.22f, .1f), new Vector2(.78f, .9f));
            var s = UI.Scroll(p.transform, Vector2.zero, Vector2.one);
            UI.Line(s, exit == 0 ? "Fuera del buque" : "Salida: " + CaseData.ExitNames[exit], 34, Pal.Sonar, true);
            UI.Line(s, text, 22);
            UI.Line(s, $"Pruebas selladas: {C.evidence.Count}  ·  Categoría A: {a}  ·  Destruidas o perdidas: {C.burned.Count}", 21, Pal.ThermHot);
            foreach (var n in people)
            {
                var st = n.State;
                UI.Line(s, $"{n.Def.name}: {(st.rescued ? "<color=#6CFF8A>a salvo</color>" : "<color=#F2A73B>pendiente, sigue a bordo</color>")}", 21);
            }
            bool someoneLeft = people != null && System.Array.Exists(people, n => !n.State.rescued);
            if (a >= 1)
                UI.Btn(s, "Llevar las pruebas al tribunal: Fase 4", () => Game.GoToPhase(4), Pal.Sealed, 24, 56);
            else
                UI.Line(s, "Sin al menos una prueba de categoría A el caso no llega al tribunal.", 21, Pal.Danger);
            if (a == 0 || (exit == 0 && someoneLeft))
                UI.Btn(s, "Volver a abordar (6 h después)", () => { C.hours += 6; Game.GoToPhase(3); }, Pal.DeckLight, 22, 50);
            UI.Btn(s, "Volver a la base", Game.GoToMenu, Pal.Dim, 20, 46);
            Game.Write();
        }

        // ---------- HUD ----------
        void BuildUI()
        {
            canvas = UI.NewCanvas();
            var tl = UI.Panel(canvas.transform, new Vector2(0, .75f), new Vector2(.24f, 1));
            objectives = UI.Text(tl.transform, "", 18);
            objectives.margin = new Vector4(14, 10, 14, 10);

            var tr = UI.Panel(canvas.transform, new Vector2(.8f, .82f), new Vector2(1, 1));
            var trs = UI.VStack(tr.transform, 12, 6);
            stateText = UI.Line(trs, "", 22, Pal.Sonar, true);
            suspBar = UI.NewBar(trs, Pal.DeckLight, 12);
            envText = UI.Line(trs, "", 16, Pal.Dim);
            breathText = UI.Line(trs, "Aliento", 15, Pal.Dim);
            staminaBar = UI.NewBar(trs, Pal.Sonar, 6);

            var bl = UI.Panel(canvas.transform, new Vector2(0, 0), new Vector2(.42f, .1f));
            tools = UI.Text(bl.transform, "", 17, Pal.Text, TextAlignmentOptions.MidlineLeft);
            tools.margin = new Vector4(14, 4, 14, 4);

            compass = UI.Text(UI.Rect(canvas.transform, "Brujula", new Vector2(.4f, .94f), new Vector2(.6f, 1)), "", 22, Pal.Sonar, TextAlignmentOptions.Center);
            lockText = UI.Text(UI.Rect(canvas.transform, "Lock", new Vector2(.28f, .84f), new Vector2(.72f, .94f)), "", 28, Pal.Danger, TextAlignmentOptions.Center, true);
            prompt = UI.Text(UI.Rect(canvas.transform, "Prompt", new Vector2(.25f, .17f), new Vector2(.75f, .24f)), "", 23, Pal.Text, TextAlignmentOptions.Center);

            var holdR = UI.Rect(canvas.transform, "Hold", new Vector2(.4f, .14f), new Vector2(.6f, .16f));
            holdRoot = holdR.gameObject;
            var hs = UI.VStack(holdR, 0, 0);
            holdBar = UI.NewBar(hs, Pal.Sonar, 14);

            var ret = UI.Rect(canvas.transform, "Reticula", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-180, -130), new Vector2(180, 130));
            reticle = ret.gameObject;
            var frame = ret.gameObject.AddComponent<Outline>();
            var im = ret.gameObject.AddComponent<Image>(); im.color = new Color(1, 1, 1, .02f);
            frame.effectColor = Pal.Sealed; frame.effectDistance = new Vector2(2, -2);
            framedText = UI.Text(UI.Rect(ret, "Txt", new Vector2(0, -.25f), new Vector2(1, -.02f)), "", 19, Pal.Sealed, TextAlignmentOptions.Center);
            var fb = UI.Rect(ret, "FocusBar", new Vector2(0, 1.02f), new Vector2(1, 1.06f));
            focusBar = UI.NewBar(fb, Pal.Sealed, 8);

            var bR = UI.Panel(canvas.transform, new Vector2(.28f, .03f), new Vector2(.72f, .4f));
            boardingRoot = bR.gameObject;
            var bs = UI.VStack(bR.transform, 18, 8);
            boardingText = UI.Line(bs, "", 21);
            boardBar = UI.NewBar(bs, Pal.DeckLight, 18);

            toast = UI.Toasts.Create(canvas.transform, new Vector2(.2f, .68f), new Vector2(.8f, .84f));
            overlay = UI.Rect(canvas.transform, "Overlay", Vector2.zero, Vector2.one);
            overlay.gameObject.SetActive(false);
        }

        static readonly string[] StateNames = { "Tranquilo", "Sospecha", "Búsqueda", "Alerta", "Bloqueo de Evidencias" };

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
            envText.text = $"Douglas {Ocean.Douglas:0.0} · Viento {WindKnots:0} kn{(WindKnots > 30 ? " (te oyen la mitad)" : "")}\nLluvia: los guardias ven 30 % menos{(StepsMasked ? "\n<color=#3FE0D0>Crujido: pasos tapados</color>" : "")}{(Distracted ? "\n<color=#3FE0D0>Tripulación distraída</color>" : "")}";
            staminaBar.Set(player.Stamina);
            staminaBar.img.color = player.Stamina < .25f ? Pal.Danger : Pal.Sonar;
            breathText.text = player.Stamina < .25f ? "<color=#FF5940>Respiración alta: te oyen a menos de 3 m</color>" : "Aliento";

            string jam = JamActive ? $"<color=#3FE0D0>BLOQUEADOR {jamT:0} s</color>" : $"[Q] Bloqueador ({C.jammerCharges})";
            tools.text = $"{jam}   [V] Aturdidor {(C.stunUsed ? "(usado)" : "(1)")}   Botiquines {C.kits}   Raciones {C.rations}\n[Clic der.] Cámara forense   [Tab] Maletín {C.evidence.Count}/{CaseData.MaxEvidence}   [P] Patrullera";

            float heading = Mathf.Repeat(player.transform.eulerAngles.y, 360f);
            compass.text = $"{heading:000}°  {Where(player.transform.position)}";

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

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b><color=#3FE0D0>FASE 3 · INFILTRACIÓN</color></b>");
            int a = Game.CountCategoryA();
            sb.AppendLine(Check(a >= 1, $"Al menos 1 prueba A ({a})"));
            sb.AppendLine(Check(C.openDoors.Contains("babor"), "Llegar al compartimento K4 (babor, abajo)"));
            int rescued = 0, followers = Followers.Count;
            foreach (var n in people) if (n.State.rescued) rescued++;
            sb.AppendLine(Check(rescued >= people.Length, $"Rescatar a la tripulación ({followers} te siguen)"));
            sb.AppendLine("\n<b>Salidas</b>");
            sb.AppendLine("Lancha (popa): lenta, conserva pruebas");
            sb.AppendLine("Patrullera [P]: rápida, alerta al capitán");
            sb.AppendLine("Puente: lo más difícil, mayor sentencia");
            objectives.text = sb.ToString();
        }

        static string Check(bool done, string text) => (done ? "<color=#6CFF8A>■ " : "<color=#8A9AA0>□ ") + text + "</color>";

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
