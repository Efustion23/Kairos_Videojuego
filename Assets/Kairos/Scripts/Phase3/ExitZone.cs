using UnityEngine;

namespace Kairos
{
    public class ExitZone : Interactable
    {
        public override string Prompt => "[E] Punto de salida: la lancha está bajo la popa";
        public override void Use() => InfiltrationController.I.OpenExitMenu();
    }
}
