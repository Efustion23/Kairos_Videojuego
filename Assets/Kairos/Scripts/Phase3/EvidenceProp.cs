using UnityEngine;

namespace Kairos
{
    /// Objeto que se documenta con la cámara forense: la foto es la prueba.
    public class EvidenceProp : Interactable
    {
        public string evidenceId;
        public EvidenceDef Def => CaseData.Evidence(evidenceId);
        public override float Hold => 1.2f; // el enfoque de la cámara forense
        public override string Prompt => $"[Mantén E] Fotografiar: {Def.title}";
        public override void Use() => InfiltrationController.I.Photograph(this);
    }
}
