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
        }

        void Update()
        {
            var I = InfiltrationController.I;
            if (motion != null) motion.talking = I != null && I.mode == InfiltrationController.Mode.Dialog && I.DialogWith == this;
            if (I == null || agent == null || !agent.isOnNavMesh) return;
            if (State.following)
            {
                agent.speed = Game.Has(2, 3) ? 4.4f : 3.1f;
                agent.stoppingDistance = 2f;
                agent.SetDestination(I.player.transform.position);
            }
            else if (agent.hasPath) agent.ResetPath();
        }
    }
}
