using UnityEngine;

namespace Kairos
{
    /// Objeto que se documenta con la cámara forense: la foto es la prueba.
    /// De cerca rigen las mismas reglas que al apuntar: enfocar sin moverse y sin rostros sin permiso.
    public class EvidenceProp : Interactable
    {
        public string evidenceId;
        public EvidenceDef Def => CaseData.Evidence(evidenceId);
        public override float Hold => 1.2f; // el enfoque de la cámara forense

        PlayerAgent Player => InfiltrationController.I.player;
        Vector3 Eye => Player.transform.position + Vector3.up * 1.5f;

        public override string Prompt => CanUse(out string why) ? $"[Mantén E, sin moverte] Fotografiar: {Def.title}" : why;

        public override bool CanUse(out string why)
        {
            why = Player.FaceInFrame(Eye, transform.position - Eye);
            if (why != null) why += ": cambia de ángulo";
            return why == null;
        }

        public override void Holding() { if (Player.IsMoving) progress = 0; }
        public override void Use() => InfiltrationController.I.Photograph(this);
    }
}
