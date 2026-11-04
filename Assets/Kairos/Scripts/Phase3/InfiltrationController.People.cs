using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Kairos
{
    public partial class InfiltrationController
    {
        static string TrustWord(int t) => t < 40 ? "Desconfía" : t < 60 ? "Escucha" : t < 80 ? "Quiere hablar" : "Confía en ti";

        public void OpenDialog(PersonNpc npc)
        {
            SetOverlay(Mode.Dialog);
            player.FaceTo(npc.transform.position);
            var toPlayer = player.transform.position - npc.transform.position; toPlayer.y = 0;
            if (toPlayer.sqrMagnitude > .01f) npc.transform.rotation = Quaternion.LookRotation(toPlayer);
            var p = UI.Panel(overlay, new Vector2(.52f, .08f), new Vector2(.98f, .92f)); // a un lado: los personajes siguen a la vista
            var s = UI.VStack(p.transform, 26, 8);
            var d = npc.Def; var st = npc.State;
            UI.Line(s, d.name, 34, Pal.Sonar, true);
            UI.Line(s, $"{d.origin}. {d.voice}", 20, Pal.Dim);
            UI.Line(s, "Actitud: " + TrustWord(st.trust), 20, Pal.ThermHot);
            var said = UI.Line(s, st.talked ? "" : d.intro, 24);

            void Say(string line) { CloseOverlayKeep(); OpenDialog(npc); lastSaid.text = line; }
            lastSaid = said;

            if (!st.talked)
                UI.Btn(s, "«Soy inspector de KAIROS. No trabajo para esta empresa.»", () =>
                {
                    st.talked = true; st.trust += Game.Has(2, 4) ? 35 : 25;
                    Say(Game.Has(2, 4) ? "(El traductor del casco habla en su idioma.) " + d.intro : d.intro);
                });
            if (st.talked && !st.explained)
                UI.Btn(s, "Explicar el plan de salida y lo que puede pasar", () => { st.explained = true; st.trust += 20; Say("Te escucha sin interrumpir."); });
            if (st.freed && st.trust >= 40)
                UI.Btn(s, st.following ? "«Espera aquí, vuelvo a por ti.»" : "«Salimos juntos. Sigue mis pasos.»", () =>
                {
                    st.following = !st.following;
                    if (st.following && !Followers.Contains(npc)) Followers.Add(npc);
                    Progressed();
                    Say(st.following ? "Asiente y se pone detrás de ti." : "Se queda donde está, vigilando la puerta.");
                }, Pal.Sealed);
            if (st.trust >= 60 && st.testimony == 0)
                UI.Btn(s, "Preguntar si quiere dar testimonio (solo si quiere)", () =>
                {
                    if (!d.willTestify)
                    {
                        st.testimony = 2;
                        Game.AddReputation(2);
                        Say(d.plan + "\n<color=#7F9098>Respetas su decisión.</color>");
                        return;
                    }
                    Game.SealEvidence(d.testimonyEvidence, Gps());
                    st.testimony = 1;
                    Game.AddReputation(3);
                    Sfx.Play(Sfx.Seal, .9f);
                    Progressed();
                    Say((Game.S.omitSensitive ? d.testimonySafe : d.testimony) + "\n<color=#6CFF8A>Consiente el lector biométrico. Testimonio sellado.</color>");
                });
            if (st.trust >= 60 && st.explained && d.willTestify && !planHeard.Contains(st.id))
                UI.Btn(s, "«¿Hay algo que deba saber?»", () => { planHeard.Add(st.id); RevealPlan(st); Say(d.plan); });
            if (st.talked && !st.medkit && C.kits > 0)
                UI.Btn(s, $"Ofrecer botiquín ({C.kits})", () => { st.medkit = true; C.kits--; st.trust += 25; player.motion.PlayKneel(); Say("Acepta. Por primera vez respira con calma."); });
            if (st.talked && !st.ration && C.rations > 0)
                UI.Btn(s, $"Ofrecer raciones ({C.rations})", () => { st.ration = true; C.rations--; st.trust += 15; player.motion.PlayKneel(); Say("Come despacio. Comparte la mitad con quien tiene al lado."); });
            if (st.trust >= 80 && !st.helped)
                UI.Btn(s, "«¿Quieres ayudarme?» (siempre puede decir que no)", () =>
                {
                    st.helped = true;
                    ActiveHelp(npc);
                    Say(d.help);
                });

            UI.Btn(s, "Cerrar (Esc)", CloseOverlay, Pal.Dim);
        }

        TextMeshProUGUI lastSaid;

        readonly HashSet<string> planHeard = new HashSet<string>();

        void RevealPlan(PersonState st)
        {
            if (st.id == "rodel") { codeKnown = true; Toast("Clave de la escotilla de babor: 4-7-1-9. Ya no hace falta cortarla.", Pal.Sealed, 6); }
            if (st.id == "moussa") OpenDoorById("servicio");
        }

        void OpenDoorById(string id)
        {
            foreach (var d in doors) if (d.doorId == id && d.gameObject.activeSelf) { d.Open(true); return; }
        }

        void ActiveHelp(PersonNpc npc)
        {
            switch (npc.personId)
            {
                case "ari":
                    GuardAI nearest = null; float best = 1e9f;
                    foreach (var g in guards)
                    {
                        float dist = Vector3.Distance(g.transform.position, npc.transform.position);
                        if (!g.IsStunned && dist < best) { best = dist; nearest = g; }
                    }
                    if (nearest != null) nearest.Lure(npc.transform.position + Vector3.forward * 3);
                    Sfx.PlayAt(Sfx.Knock, npc.transform.position, 1f);
                    break;
                case "moussa": OpenDoorById("servicio"); break;
                case "rodel": distractT = Mathf.Max(distractT, 3f); nextHeel = Mathf.Min(nextHeel, 6f); break;
                case "thant": creakT = 8f; break;
            }
        }
    }
}
