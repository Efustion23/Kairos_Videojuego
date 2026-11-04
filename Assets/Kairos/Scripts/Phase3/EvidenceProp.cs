using UnityEngine;

namespace Kairos
{
    /// Objeto que se documenta con la cámara forense: la foto es la prueba.
    public class EvidenceProp : Interactable
    {
        public string evidenceId;
        public EvidenceDef Def => CaseData.Evidence(evidenceId);
        public override string Prompt => $"{Def.title}: clic derecho para encuadrar con la cámara forense";
        public override void Use() { }
    }
}
