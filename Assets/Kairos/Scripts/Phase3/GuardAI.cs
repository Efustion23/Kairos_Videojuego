using UnityEngine;
using UnityEngine.AI;

namespace Kairos
{
    public class GuardAI : MonoBehaviour
    {
        public Transform[] route;
        public float viewRange = 11f, viewAngle = 100f;
        public string displayName = "Guardia";
        public GuardState state;
        public float suspicion;

        NavMeshAgent agent;
        VisionFan fan;
        Walker walker;
        float stepDistance;
        int wp; float wait, stunned, radio, lostFor;
        Vector3 lastKnown;
        InfiltrationController I => InfiltrationController.I;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            agent.autoBraking = true;
            fan = VisionFan.Create(transform, .08f, viewRange, viewAngle);
            walker = GetComponent<Walker>();
        }

        public float EffectiveRange => viewRange * .7f * (Game.S.networkReorganized ? 1.15f : 1f); // la lluvia reduce 30 % el alcance

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
            lastKnown = pos;
        }

        /// Tras un repliegue: vuelve a su ronda, lo más lejos posible del punto seguro.
        public void Regroup(Vector3 safePoint)
        {
            suspicion = 0; radio = 0; lostFor = 0; state = GuardState.Tranquilo;
            if (agent == null || !agent.isOnNavMesh || route == null || route.Length == 0) return;
            for (int i = 0; i < route.Length; i++)
                if (Vector3.Distance(route[i].position, safePoint) > Vector3.Distance(route[wp].position, safePoint)) wp = i;
            agent.Warp(route[wp].position);
            agent.ResetPath();
        }

        public void Lure(Vector3 pos) { suspicion = Mathf.Max(suspicion, .65f); lastKnown = pos; }
        public void Stun(float seconds)
        {
            stunned = seconds; suspicion = 0; radio = 0;
            if (agent.isOnNavMesh) agent.ResetPath();
            walker.crouched = true;
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
                return;
            }
            agent.isStopped = false;
            walker.crouched = false;
            Footsteps();

            var p = I.player;
            bool seen = false; float dist = 99f;
            if (!I.Distracted)
            {
                if (See(p.ChestPos, out float dp)) { seen = true; dist = dp; lastKnown = p.transform.position; suspicion += Time.deltaTime * Mathf.Lerp(.7f, .25f, dp / EffectiveRange) * p.Visibility * (I.Lockdown ? 1.5f : 1f); }
                foreach (var f in I.Followers)
                    if (See(f.transform.position + Vector3.up, out float df)) { seen = true; lastKnown = f.transform.position; suspicion += Time.deltaTime * .3f; }
            }

            if (seen) lostFor = 0;
            else
            {
                lostFor += Time.deltaTime;
                if (lostFor > 1.5f) suspicion -= Time.deltaTime * (state == GuardState.Alerta ? .15f : .2f);
            }
            suspicion = Mathf.Clamp01(suspicion);

            if (suspicion >= 1f) state = GuardState.Alerta;
            else if (state == GuardState.Alerta && suspicion > .5f) { }
            else if (I.Lockdown) state = GuardState.Bloqueo;
            else if (suspicion >= .6f) state = GuardState.Busqueda;
            else if (suspicion >= .25f) state = GuardState.Sospecha;
            else state = GuardState.Tranquilo;

            switch (state)
            {
                case GuardState.Tranquilo:
                    agent.speed = 1.7f;
                    Patrol();
                    break;
                case GuardState.Sospecha:
                    agent.ResetPath();
                    Face(lastKnown);
                    break;
                case GuardState.Busqueda:
                    agent.speed = 3f;
                    agent.SetDestination(lastKnown);
                    if (!agent.pathPending && agent.remainingDistance < .8f) transform.Rotate(0, 70f * Time.deltaTime, 0);
                    break;
                case GuardState.Alerta:
                    agent.speed = 3.8f; // correr (5,2) siempre te saca de encima
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
                    agent.speed = 3.2f;
                    Patrol();
                    break;
            }

            fan.SetColor(state == GuardState.Tranquilo ? Pal.Sonar : state == GuardState.Sospecha ? Pal.DeckLight : Pal.Danger);
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
                wait -= Time.deltaTime;
                if (wait > 0) return;
                wait = Random.Range(.8f, 2.4f);
                wp = (wp + 1) % route.Length;
                agent.SetDestination(route[wp].position);
                // La radio delata un cambio de ronda: solo se oye de cerca y avisa al jugador.
                if ((transform.position - I.player.transform.position).sqrMagnitude < 18f * 18f)
                    Sfx.PlayAt(Sfx.Radio, transform.position + Vector3.up * 1.4f, .5f, 20);
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
