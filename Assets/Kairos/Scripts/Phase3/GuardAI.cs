using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Kairos
{
    /// Guardia: ronda, sospecha, búsqueda y alerta. Gira antes de echar a andar (para que la animación case
    /// con el movimiento), investiga lo que oye, rastrea varios puntos alrededor de donde te perdió,
    /// predice hacia dónde huías, avisa a quien tenga cerca y reacciona si encuentra a un compañero aturdido.
    public class GuardAI : MonoBehaviour
    {
        public Transform[] route;
        public float viewRange = 11f, viewAngle = 100f;
        [Header("Tiempos de parada en waypoints (s)")]
        public float minWaitTime = 1.6f;
        public float maxWaitTime = 3.2f;
        public string displayName = "Guardia";
        public GuardState state;
        public float suspicion;

        // Velocidades que casan con los clips de Mixamo a 1,80 m (caminar 1,3 · trote · carrera).
        const float PatrolSpeed = 1.35f, InvestigateSpeed = 1.1f, SearchSpeed = 2.6f, ChaseSpeed = 3.8f;
        const float ShoutRadius = 14f;
        static readonly Color CalmFan = new Color(1f, .93f, .72f);

        NavMeshAgent agent;
        VisionFan fan;
        Walker walker;
        HumanoidMotion motion;
        float stepDistance;
        int wp; float wait, stunned, radio, lostFor, scanPhase, suspectT, searchWait;
        int searchLeft;
        bool waitingAtWp, predicted;
        Vector3 lastKnown, playerLastPos, playerVel;
        GuardState prevState;
        readonly HashSet<GuardAI> noticedDown = new HashSet<GuardAI>();
        InfiltrationController I => InfiltrationController.I;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            agent.autoBraking = true;
            agent.angularSpeed = 240;
            fan = VisionFan.Create(transform, .08f, viewRange, viewAngle);
            walker = GetComponent<Walker>();
            motion = GetComponentInChildren<HumanoidMotion>();

            var lamp = transform.Find("LinternaHalogena") ?? transform.Find("Linterna");
            if (lamp == null)
            {
                var lGo = new GameObject("Linterna");
                lGo.transform.SetParent(transform, false);
                lGo.transform.localPosition = new Vector3(0, 1.35f, .25f);
                lGo.transform.localRotation = Quaternion.Euler(10, 0, 0);
                var l = lGo.AddComponent<Light>();
                l.type = LightType.Spot; l.color = new Color(1f, .88f, .68f); l.range = 10f; l.spotAngle = 46f; l.intensity = 3.5f;
                l.shadows = LightShadows.None;
            }
        }

        // Alcance efectivo condicionado por lluvia nocturna, mar de fondo Douglas y sector
        public float EffectiveRange => viewRange * (Ocean.Douglas >= 5f ? .6f : .7f) * (Game.S.networkReorganized ? 1.15f : 1f) * (Game.C.sector == 2 ? 1.15f : 1f);

        bool See(Vector3 target, out float dist)
        {
            var eye = transform.position + Vector3.up * 1.6f;
            var to = target - eye;
            dist = to.magnitude;
            if (dist > EffectiveRange) return false;
            var flat = new Vector3(to.x, 0, to.z);
            if (dist > 2.2f && Vector3.Angle(transform.forward, flat) > viewAngle * .5f) return false;
            return !Physics.Linecast(eye, target, Layers.SolidMask);
        }

        public void Hear(Vector3 pos, float radius)
        {
            if (stunned > 0) return;
            float d = Vector3.Distance(pos, transform.position);
            if (d > radius) return;
            suspicion = Mathf.Max(suspicion, Mathf.Min(.65f, suspicion + (1 - d / radius) * .25f)); // oír nunca dispara la alerta
            lastKnown = pos; lostFor = 0; // la duda dura lo bastante para ir a mirar
        }

        /// Un compañero grita: va a mirar, pero no sabe dónde estás exactamente (no salta a alerta).
        public void HearShout(Vector3 pos)
        {
            if (stunned > 0 || state == GuardState.Alerta) return;
            suspicion = Mathf.Max(suspicion, .7f);
            lastKnown = pos; lostFor = 0; // margen antes de que la duda empiece a bajar
            searchLeft = 3; predicted = true;
        }

        /// Tras un repliegue: vuelve a su ronda, lo más lejos posible del punto seguro.
        public void Regroup(Vector3 safePoint)
        {
            suspicion = 0; radio = 0; lostFor = 0; state = prevState = GuardState.Tranquilo; searchLeft = 0;
            if (agent == null || !agent.isOnNavMesh || route == null || route.Length == 0) return;
            for (int i = 0; i < route.Length; i++)
                if (Vector3.Distance(route[i].position, safePoint) > Vector3.Distance(route[wp].position, safePoint)) wp = i;
            agent.Warp(route[wp].position);
            agent.ResetPath();
        }

        public void Lure(Vector3 pos) { suspicion = Mathf.Max(suspicion, .65f); lastKnown = pos; lostFor = 0; searchLeft = 3; predicted = true; }
        public void Stun(float seconds)
        {
            stunned = seconds; suspicion = 0; radio = 0; searchLeft = 0;
            if (agent.isOnNavMesh) { agent.ResetPath(); agent.velocity = Vector3.zero; }
            if (walker != null) walker.crouched = true;
            if (motion != null) motion.downed = true; // cae de rodillas y se queda en el suelo, no "pulsa un botón"
        }
        public bool IsStunned => stunned > 0;

        void Update()
        {
            if (I == null || !I.Running || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            if (stunned > 0)
            {
                stunned -= Time.deltaTime;
                state = GuardState.Tranquilo;
                fan.SetColor(new Color(.2f, .2f, .2f));
                agent.isStopped = true;
                if (stunned <= 0)
                {
                    // Se levanta desorientado: sospecha y mira a su alrededor antes de volver a la ronda.
                    if (motion != null) motion.downed = false;
                    suspicion = .4f; lastKnown = transform.position + transform.forward * 2f; suspectT = 0;
                }
                return;
            }
            agent.isStopped = false;
            if (walker != null) walker.crouched = false;
            if (fan != null) fan.range = EffectiveRange;
            Footsteps();

            // Reacción visible ante escora fuerte del buque (>15°): se sujetan y pierden foco unos segundos
            if (I.HeelAngle > 15f && state != GuardState.Alerta)
            {
                agent.isStopped = true;
                if (fan != null) fan.SetColor(new Color(.35f, .55f, .65f));
                return;
            }

            var p = I.player;
            bool seen = false; float dist = 99f;
            if (!I.Distracted)
            {
                if (See(p.ChestPos, out float dp))
                {
                    seen = true; dist = dp; lastKnown = p.transform.position; predicted = false;
                    suspicion += Time.deltaTime * Mathf.Lerp(.7f, .25f, dp / EffectiveRange) * p.Visibility * (I.Lockdown ? 1.5f : 1f);
                }
                foreach (var f in I.Followers)
                    if (See(f.transform.position + Vector3.up, out float df)) { seen = true; lastKnown = f.transform.position; predicted = false; suspicion += Time.deltaTime * .3f; }
                CheckFallenColleagues();
            }
            TrackPlayer(p.transform.position);

            if (seen) lostFor = 0;
            else
            {
                lostFor += Time.deltaTime;
                if (lostFor > 1.5f) suspicion -= Time.deltaTime * (state == GuardState.Alerta ? .15f : searchLeft > 0 ? .06f : .2f);
            }
            suspicion = Mathf.Clamp01(suspicion);

            if (suspicion >= 1f) state = GuardState.Alerta;
            else if (state == GuardState.Alerta && suspicion > .5f) { }
            else if (I.Lockdown) state = GuardState.Bloqueo;
            else if (suspicion >= .6f) state = GuardState.Busqueda;
            else if (suspicion >= .25f) state = GuardState.Sospecha;
            else state = GuardState.Tranquilo;

            if (state != prevState) OnStateChanged(prevState, state);
            prevState = state;

            switch (state)
            {
                case GuardState.Tranquilo:
                    agent.speed = PatrolSpeed;
                    Patrol();
                    break;
                case GuardState.Sospecha:
                    // Primero se para y mira; si la duda sigue, se acerca despacio a comprobarlo.
                    suspectT += Time.deltaTime;
                    if (suspectT < 1.3f || Vector3.Distance(transform.position, lastKnown) < 2.5f) { agent.ResetPath(); Face(lastKnown); }
                    else { agent.speed = InvestigateSpeed; agent.SetDestination(lastKnown); }
                    break;
                case GuardState.Busqueda:
                    agent.speed = SearchSpeed;
                    Search();
                    break;
                case GuardState.Alerta:
                    agent.speed = ChaseSpeed; // correr (5,2) siempre te saca de encima
                    if (!seen && lostFor > .6f && !predicted)
                    {
                        // Te ha perdido: va hacia donde ibas, no al último punto donde te vio.
                        predicted = true;
                        lastKnown = OnNav(lastKnown + Vector3.ClampMagnitude(playerVel * 1.4f, 5f), lastKnown);
                    }
                    agent.SetDestination(lastKnown);
                    if (!agent.pathPending && agent.remainingDistance < .8f) transform.Rotate(0, 90f * Time.deltaTime, 0);
                    if (I.JamActive) radio = 0;
                    else
                    {
                        radio += Time.deltaTime;
                        if (radio > 15f && !I.Lockdown) { radio = 0; I.StartLockdown(displayName + " avisa por radio"); }
                    }
                    if (seen && Vector3.Distance(p.transform.position, transform.position) < 1.2f) I.Caught();
                    break;
                case GuardState.Bloqueo:
                    agent.speed = SearchSpeed;
                    Patrol();
                    break;
            }

            TurnBeforeWalking();
            // En calma, el tono de su linterna (el cian es el de la ruta y se confundían); naranja si sospecha, rojo en alerta.
            fan.SetColor(state == GuardState.Tranquilo ? CalmFan : state == GuardState.Sospecha ? Pal.DeckLight : Pal.Danger);
            bool alert = state == GuardState.Alerta || state == GuardState.Busqueda;
            SendMessage("SetAlert", alert, SendMessageOptions.DontRequireReceiver);
        }

        void OnStateChanged(GuardState from, GuardState to)
        {
            if (to == GuardState.Sospecha) suspectT = 0;
            if (to == GuardState.Busqueda && from < GuardState.Busqueda) searchLeft = 3;
            // Al darte por visto avisa a voces a quien tenga cerca: acuden a buscar.
            if (to == GuardState.Alerta)
            {
                foreach (var g in I.guards)
                    if (g != this && g != null && (g.transform.position - transform.position).sqrMagnitude < ShoutRadius * ShoutRadius)
                        g.HearShout(lastKnown);
                Sfx.PlayAt(Sfx.Radio, transform.position + Vector3.up * 1.5f, .7f, 22);
            }
            // De vuelta a la calma: retoma la ronda por el punto más cercano, no cruzando medio barco.
            if (to == GuardState.Tranquilo && from >= GuardState.Busqueda && route != null && route.Length > 0)
            {
                int best = 0;
                for (int i = 1; i < route.Length; i++)
                    if ((route[i].position - transform.position).sqrMagnitude < (route[best].position - transform.position).sqrMagnitude) best = i;
                wp = best; waitingAtWp = false;
                agent.SetDestination(route[wp].position);
            }
        }

        /// Llega al último punto conocido y rastrea alrededor: varios puntos cercanos, mirando en cada uno.
        void Search()
        {
            if (agent.pathPending) return;
            if (agent.hasPath && agent.remainingDistance > .8f) { searchWait = 0; return; }
            if (searchWait <= 0) searchWait = Random.Range(.9f, 1.6f);
            searchWait -= Time.deltaTime;
            transform.Rotate(0, Mathf.Sin(Time.time * 2.3f) * 80f * Time.deltaTime, 0); // barre con la linterna
            if (searchWait > 0) return;
            searchWait = 0;
            if (searchLeft > 0)
            {
                searchLeft--;
                var dir = Quaternion.Euler(0, Random.Range(-120f, 120f), 0) * transform.forward;
                agent.SetDestination(OnNav(lastKnown + dir * Random.Range(2.5f, 5f), lastKnown));
            }
        }

        /// Si ve a un compañero tirado, va a ver qué ha pasado.
        void CheckFallenColleagues()
        {
            foreach (var g in I.guards)
            {
                if (g == this || g == null || !g.IsStunned || noticedDown.Contains(g)) continue;
                if (!See(g.transform.position + Vector3.up * .5f, out _)) continue;
                noticedDown.Add(g);
                suspicion = Mathf.Max(suspicion, .7f);
                lastKnown = g.transform.position; lostFor = 0; searchLeft = 3; predicted = true;
            }
        }

        void TrackPlayer(Vector3 pos)
        {
            if (Time.deltaTime > 0) playerVel = Vector3.Lerp(playerVel, (pos - playerLastPos) / Time.deltaTime, .1f);
            playerVel.y = 0;
            if (playerVel.sqrMagnitude > 100f) playerVel = Vector3.zero; // un repliegue no es una carrera
            playerLastPos = pos;
        }

        static Vector3 OnNav(Vector3 p, Vector3 fallback) =>
            NavMesh.SamplePosition(p, out var hit, 2.5f, NavMesh.AllAreas) ? hit.position : fallback;

        /// Si el camino tuerce mucho, se para y gira primero (con su animación de giro): sin esto el agente
        /// arrancaba de lado o hacia atrás y el modelo caminaba de espaldas.
        void TurnBeforeWalking()
        {
            if (agent.pathPending || !agent.hasPath || agent.remainingDistance < .5f) return;
            var to = agent.steeringTarget - transform.position; to.y = 0;
            if (to.sqrMagnitude < .04f) return;
            float limit = state >= GuardState.Alerta ? 85f : 55f;
            bool turning = Vector3.Angle(transform.forward, to) > limit;
            if (!turning) return;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            float rate = state >= GuardState.Busqueda ? 420f : 220f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), rate * Time.deltaTime);
        }

        /// Sus pasos se oyen: ayudan a saber por dónde viene.
        void Footsteps()
        {
            const float StepLength = 1.4f;
            stepDistance += agent.velocity.magnitude * Time.deltaTime;
            if (stepDistance < StepLength) return;
            stepDistance = 0;
            Sfx.PlayAt(Sfx.Step, transform.position, .22f, 14, Random.Range(.8f, 1f));
        }

        void Patrol()
        {
            if (route == null || route.Length == 0) return;
            if (route.Length == 1 && (route[0].position - transform.position).sqrMagnitude < 1f) { transform.Rotate(0, 18f * Time.deltaTime, 0); return; }
            if (agent.pathPending) return;
            if (!agent.hasPath || agent.remainingDistance < .6f)
            {
                if (!waitingAtWp)
                {
                    waitingAtWp = true;
                    wait = Random.Range(minWaitTime, Mathf.Max(minWaitTime, maxWaitTime));
                    scanPhase = 0f;
                }
                wait -= Time.deltaTime;
                if (wait > 0f)
                {
                    // Ojeo suave con la linterna mientras hace pausa en el waypoint
                    scanPhase += Time.deltaTime * 1.4f;
                    transform.Rotate(0, Mathf.Sin(scanPhase) * 22f * Time.deltaTime, 0);
                    return;
                }
                waitingAtWp = false;
                wp = (wp + 1) % route.Length;
                agent.SetDestination(route[wp].position);
                // La radio delata un cambio de ronda: solo se oye de cerca y avisa al jugador.
                if ((transform.position - I.player.transform.position).sqrMagnitude < 18f * 18f)
                    Sfx.PlayAt(Sfx.Radio, transform.position + Vector3.up * 1.4f, .5f, 20);
            }
            else
            {
                waitingAtWp = false;
            }
        }

        void Face(Vector3 pos)
        {
            var d = pos - transform.position; d.y = 0;
            if (d.sqrMagnitude < .01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 180f * Time.deltaTime);
        }
    }
}
