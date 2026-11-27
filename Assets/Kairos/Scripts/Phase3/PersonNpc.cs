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

        void Update()
        {
            var I = InfiltrationController.I;
            if (motion != null) motion.talking = I != null && I.mode == InfiltrationController.Mode.Dialog && I.DialogWith == this;
            if (I == null || agent == null || !agent.isOnNavMesh) return;
            if (motion != null)
            {
                motion.downed = Captive; // al abrirse la celda se pone de pie
            }
            if (State.following)
            {
                var player = I.player;
                // Se agachan contigo, o por su cuenta si hay un guardia en alerta cerca.
                bool hide = player.Crouching || GuardAlertNear(10f);
                if (motion != null) motion.stealthCrouch = hide;
                agent.speed = hide ? 1.6f : Game.Has(2, 3) ? 4.4f : 3.1f;
                // Cada uno a su distancia: en fila detrás de ti, no amontonados encima.
                int idx = Mathf.Max(0, I.Followers.IndexOf(this));
                agent.stoppingDistance = 1.8f + idx * 1.1f;
                agent.SetDestination(player.transform.position);
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + .1f) FaceTowards(player.transform.position);
            }
            else
            {
                if (motion != null) motion.stealthCrouch = false;
                if (agent.hasPath) agent.ResetPath();
            }
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
