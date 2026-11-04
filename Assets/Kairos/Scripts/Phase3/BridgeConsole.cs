using UnityEngine;

namespace Kairos
{
    /// Consola del puente: capturar comunicaciones y, después, asegurar el puente.
    public class BridgeConsole : Interactable
    {
        InfiltrationController I => InfiltrationController.I;
        bool CaptureDone => Game.HasEvidence("e_comms");

        public override float Hold => CaptureDone ? 20f : 30f;
        public override string Prompt => CaptureDone
            ? "[Mantén E] Asegurar el puente y forzar la entrada a puerto"
            : "[Mantén E] Capturar comunicaciones (con el bloqueador activo)";

        public override bool CanUse(out string why)
        {
            why = null;
            if (!CaptureDone && !I.JamActive) { why = "Pulsa Q para activar el bloqueador: las radios deben estar cortadas."; return false; }
            if (CaptureDone && I.CaptainAwake()) { why = "El capitán sigue al mando: acércate y pulsa Q para aturdirlo."; return false; }
            return true;
        }

        public override void Use()
        {
            if (!CaptureDone)
            {
                Game.SealEvidence("e_comms", I.Gps());
                progress = 0;
                Sfx.Play(Sfx.Seal, .8f);
                I.Toast("Comunicaciones capturadas y selladas. Prueba A.", Pal.Sealed);
                I.Progressed();
                return;
            }
            I.Finish(3);
        }
    }
}
