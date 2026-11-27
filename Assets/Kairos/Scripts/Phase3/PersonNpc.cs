using UnityEngine;
using UnityEngine.AI;

namespace Kairos
{
    /// Persona retenida: tiene nombre, voz y decisión propia.
    public class PersonNpc : Interactable
    {
        public string personId;
        /// Solo con modelo animado; la figura simple no tiene.
        public HumanoidMotion motion;
        NavMeshAgent agent;
        public PersonState State => Game.Person(personId);
        public PersonDef Def => CaseData.PersonDefOf(personId);

        public override string Prompt => $"[E] Hablar con {Def.name}";
        public override void Use() => InfiltrationController.I.OpenDialog(this);

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = true;
            ApplySectorLook();
            // Quien sigue encerrado empieza ya sentado en el suelo, sin animación de arrodillarse al cargar.
            if (motion != null && Captive) { motion.downedQuick = true; motion.downed = true; }
        }

        /// Encerrado y sin salir todavía: espera sentado en el suelo de su celda.
        bool Captive => State != null && !State.freed && !State.following && !State.rescued;

        void ApplySectorLook()
        {
            int sector = Game.S?.c?.sector ?? 0;
            Color tint = CaseData.CrewOutfitTintFor(sector, personId);
            if (tint == Color.white) return;

            var block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mat = r.sharedMaterial;
                if (mat == null || mat.name.Contains($"_S{sector + 1}_")) continue;
                Color baseCol = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", baseCol * tint);
                block.SetColor("_Color", baseCol * tint);
                r.SetPropertyBlock(block);
            }
        }

        int trailIdx = -1; // por qué punto de tu rastro va
        Vector3 lastDest = Vector3.positiveInfinity;
        public void ShiftTrail(int n) { if (trailIdx >= 0) trailIdx = Mathf.Max(0, trailIdx - n); }
        public void ResetTrail() => trailIdx = -1;

        void Update()
        {
            var I = InfiltrationController.I;
            bool talking = I != null && I.mode == InfiltrationController.Mode.Dialog && I.DialogWith == this;
            if (motion != null)
            {
                motion.talking = talking;
                // Si sigue encerrado, al hablar se pone de pie y luego vuelve a sentarse.
                motion.downed = Captive && !talking;
                motion.lookTarget = talking && I != null ? Head(I.player.transform, I.player.Crouching) : (Vector3?)null;
            }
            if (talking && I.player.motion != null) I.player.motion.lookTarget = Head(transform, motion != null && motion.downed);
            if (I == null || agent == null || !agent.isOnNavMesh) return;
            if (State.following)
            {
                var player = I.player;
                // Se agachan contigo, o por su cuenta si hay un guardia en alerta cerca.
                bool hide = player.Crouching || GuardAlertNear(10f);
                if (motion != null) motion.stealthCrouch = hide;
                agent.speed = hide ? 1.6f : Game.Has(2, 3) ? 4.4f : 3.1f;
                FollowTrail(I, player);
            }
            else
            {
                trailIdx = -1;
                if (motion != null) motion.stealthCrouch = false;
                if (agent.hasPath) agent.ResetPath();
            }
        }

        static Vector3 Head(Transform t, bool low) => t.position + Vector3.up * (low ? 1.05f : 1.62f);

        /// Va por tu rastro, punto a punto y en fila: cada uno se queda a su distancia detrás de ti.
        /// Si aún está lejos del rastro (recién liberado), primero se acerca por el camino más corto.
        void FollowTrail(InfiltrationController I, PlayerAgent player)
        {
            var trail = I.Trail;
            int idx = Mathf.Max(0, I.Followers.IndexOf(this));
            float keep = 1.8f + idx * 1.1f;
            agent.stoppingDistance = .3f;
            if (trail.Count == 0) { Go(player.transform.position, keep); return; }

            // Último punto del rastro que queda al menos 'keep' metros por detrás de ti.
            int limit = trail.Count - 1; float behind = Vector3.Distance(player.transform.position, trail[limit]);
            while (limit > 0 && behind < keep) { behind += Vector3.Distance(trail[limit], trail[limit - 1]); limit--; }

            if (trailIdx < 0)
            {
                // Engancharse al punto del rastro más cercano (de los recientes); si no hay ninguno cerca, ir directo.
                float best = 36f;
                for (int i = Mathf.Max(0, trail.Count - 150); i < trail.Count; i++)
                {
                    float d = (trail[i] - transform.position).sqrMagnitude;
                    if (d < best) { best = d; trailIdx = i; }
                }
                if (trailIdx < 0) { Go(player.transform.position, keep); return; }
            }
            while (trailIdx < limit && (trail[trailIdx] - transform.position).sqrMagnitude < 1.4f) trailIdx++;
            if (trailIdx > limit) trailIdx = limit;

            bool arrived = trailIdx == limit && (trail[limit] - transform.position).sqrMagnitude < .5f;
            if (arrived) { if (agent.hasPath) agent.ResetPath(); lastDest = Vector3.positiveInfinity; FaceTowards(player.transform.position); }
            else Go(trail[trailIdx], 0);
        }

        void Go(Vector3 dest, float stop)
        {
            if (stop > 0 && (dest - transform.position).sqrMagnitude < stop * stop)
            {
                if (agent.hasPath) agent.ResetPath();
                FaceTowards(dest);
                return;
            }
            if ((dest - lastDest).sqrMagnitude < .01f && agent.hasPath) return; // no recalcular la ruta cada frame
            lastDest = dest;
            agent.SetDestination(dest);
        }

        bool GuardAlertNear(float radius)
        {
            var I = InfiltrationController.I;
            foreach (var g in I.guards)
                if (g != null && !g.IsStunned && g.state >= GuardState.Busqueda && (g.transform.position - transform.position).sqrMagnitude < radius * radius)
                    return true;
            return false;
        }

        void FaceTowards(Vector3 p)
        {
            var d = p - transform.position; d.y = 0;
            if (d.sqrMagnitude < .01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 120f * Time.deltaTime);
        }
    }
}
