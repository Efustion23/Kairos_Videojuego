using UnityEngine;

namespace Kairos
{
    /// Escotilla: cortadora de plasma (4-8 s, ruido a 10 m, destello) o clave revelada por una persona.
    public class LockedDoor : Interactable
    {
        public string doorId;
        public float cutSeconds = 6f;
        public bool cuttable = true;
        public string doorName = "escotilla";
        public bool nearCompressors;
        public GameObject solid;
        public string[] freesPersons = new string[0];

        float noiseT;
        Light flash;
        InfiltrationController I => InfiltrationController.I;
        bool HasCode => doorId == "babor" && I.codeKnown;

        public override float Hold => HasCode ? 0 : cuttable ? cutSeconds : 0;
        public override string Prompt => HasCode ? $"[E] Introducir la clave en la {doorName}"
            : cuttable ? $"[Mantén E] Cortar {doorName} con plasma" : $"La {doorName} se abre desde el otro lado";

        public override bool CanUse(out string why)
        {
            why = HasCode || cuttable ? null : "Alguien de dentro tendría que abrirla.";
            return why == null;
        }

        public override void Holding()
        {
            if (flash == null)
            {
                flash = new GameObject("Destello").AddComponent<Light>();
                flash.type = LightType.Point; flash.color = new Color(.6f, .85f, 1f); flash.range = 9;
                flash.transform.position = transform.position;
            }
            flash.intensity = Random.Range(2f, 14f);
            noiseT -= Time.deltaTime;
            if (noiseT <= 0)
            {
                noiseT = .5f;
                Sfx.PlayAt(Sfx.Plasma, transform.position, .8f, 25);
                // Los compresores tapan el ruido de la cortadora: zona segura.
                I.EmitNoise(transform.position, nearCompressors ? 1.5f : 10f);
            }
        }

        void LateUpdate()
        {
            if (flash != null && (I.player.HoldingTarget != this)) Destroy(flash.gameObject);
        }

        public override void Use() => Open(true);

        public void Open(bool announce)
        {
            if (!Game.C.openDoors.Contains(doorId)) Game.C.openDoors.Add(doorId);
            foreach (var id in freesPersons) Game.Person(id).freed = true;
            if (flash != null) Destroy(flash.gameObject);
            if (solid != null) solid.SetActive(false);
            gameObject.SetActive(false);
            if (announce) { Sfx.PlayAt(Sfx.Knock, transform.position, .5f); I.Toast(char.ToUpper(doorName[0]) + doorName.Substring(1) + " abierta.", Pal.Sonar); I.RebuildNav(); }
        }
    }
}
