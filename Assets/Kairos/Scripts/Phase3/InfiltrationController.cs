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
    /// El barco es estático: mar y cielo se mueven, y la IA pisa cubierta fija.
    [DefaultExecutionOrder(-50)]
    public partial class InfiltrationController : MonoBehaviour
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
        public bool codeKnown;

        static CaseState C => Game.C;
        public readonly List<PersonNpc> Followers = new List<PersonNpc>();
        float seaBase = 4f, jamT, distractT, creakT, heelT, nextHeel = 22f, nextCreak = 12f, lockT, burnT, patrolT = -1, visorT, visorCd, pathT, hintT;
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
            seaBase = CaseData.DouglasAt(C.hours); // el mar de esta noche es el que marca el reloj del caso
            Ocean.SetSea(seaBase + .5f);
            exitZone = FindAnyObjectByType<ExitZone>().transform;
            ApplySavedState();
            BuildUI();
            BuildRoute();
            Sfx.Loop(gameObject, Sfx.Wind, .35f);
            rainSrc = Sfx.Loop(gameObject, Sfx.Rain, .25f);
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

        // ---------- Bucle ----------
        void Update()
        {
            Ocean.DriftDouglas(seaBase, seaBase + 2.5f, 5f);
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
                bool couldCondemn = CaseData.MaxIndicators(C.burned) == CaseData.Indicators.Length;
                C.burned.Add(id);
                bool stillCan = CaseData.MaxIndicators(C.burned) == CaseData.Indicators.Length;
                Toast($"Prueba destruida: {e.Def.title}." + (couldCondemn && !stillCan ? " Ya no se pueden probar los 11 indicadores: la Condena queda fuera de alcance." : ""), Pal.Danger, couldCondemn && !stillCan ? 7 : 4);
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
                player.motion.PlayStun();
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

        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (I == this) I = null; }
    }
}
