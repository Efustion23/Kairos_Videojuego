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
            if (!CaptureDone && Game.C.burned.Contains("e_comms")) { why = "El registro de comunicaciones ya se ha borrado."; return false; }
            if (!CaptureDone && !I.JamActive) { why = "Activa el bloqueador (Q): las radios deben estar cortadas."; return false; }
            if (CaptureDone && I.CaptainAwake()) { why = "El capitán sigue al mando: no puedes asegurar el puente con él despierto."; return false; }
            return true;
        }

        public override void Use()
        {
            if (!CaptureDone)
            {
                if (!Game.SealEvidence("e_comms", I.Gps()))
                {
                    I.Toast("Maletín lleno: retira una prueba con Tab.", Pal.DeckLight);
                    return;
                }
                progress = 0;
                Sfx.Play(Sfx.Seal, .8f);
                I.Toast("Comunicaciones capturadas y selladas. Prueba A.", Pal.Sealed);
                return;
            }
            I.Finish(3);
        }
    }
}
