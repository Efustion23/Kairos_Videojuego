using UnityEngine;

namespace Kairos
{
    public partial class InfiltrationController
    {
        void SetOverlay(Mode m)
        {
            mode = m;
            UI.Clear(overlay);
            overlay.gameObject.SetActive(true);
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        void CloseOverlayKeep() { UI.Clear(overlay); }

        public void CloseOverlay()
        {
            UI.Clear(overlay);
            overlay.gameObject.SetActive(false);
            Time.timeScale = 1;
            mode = Mode.Play;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        void OpenBriefcase()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.18f, .08f), new Vector2(.82f, .92f));
            var s = UI.Scroll(p.transform, Vector2.zero, Vector2.one);

            // Cabecera de Maletín Forense (Alexander - S14-1)
            UI.Line(s, $"MALETÍN FORENSE KAIROS · {C.evidence.Count}/{CaseData.MaxEvidence}", 30, Pal.Sealed, true);
            UI.Line(s, $"Expediente Oficial: {Game.Inspector}  |  Cadena de Custodia: <color=#3FE0D0>VERIFICADA</color>", 18, Pal.Dim);

            var evBar = UI.NewBar(s, Pal.Sealed, 8);
            evBar.Set((float)C.evidence.Count / CaseData.MaxEvidence);
            UI.Line(s, $"Índice de Carga Probatoria: <b>{(float)C.evidence.Count / CaseData.MaxEvidence * 100:0}%</b> para el Tribunal de la Fase 4", 17, Pal.Sonar);

            if (C.evidence.Count == 0)
                UI.Line(s, "\nMaletín vacío. Explora el buque, acércate a documentos o bitácoras y mantén pulsado E para fotografiar y sellar digitalmente.", 21, Pal.Dim);
            else
            {
                UI.Line(s, "\n<b>Pruebas Forenses Aseguradas:</b>", 21, Pal.Text);
                foreach (var ev in C.evidence)
                {
                    var d = CaseData.Evidence(ev.id);
                    string catTag = d.cat == 'C' ? "CONTRATO" : d.cat == 'L' ? "BITÁCORA" : d.cat == 'T' ? "TESTIMONIO" : "FORENSE";
                    UI.Line(s, $"<color=#3FE0D0>◄ {catTag} ►</color> <b>{d.title}</b>\n{(Game.S.omitSensitive ? d.descSafe : d.desc)}\n<size=15><color=#6CFF8A>✔ SELLO SHA-256: {ev.sha.Substring(0, 20)}…</color>  ·  <color=#8DA2AB>{ev.stamp}</color></size>", 19);
                }
            }

            UI.Line(s, "\nOpciones de Extracción y Salida", 24, Pal.DeckLight, true);
            UI.Line(s, "• Vía Popa: Regresar a la lancha semirrígida (sigilo total y cero sospecha).\n• Vía Puente: Neutralizar al capitán y tomar la consola de navegación para incautación completa.\n• Vía Patrullera: Extracción de emergencia en 60 s (alerta al buque).", 18, Pal.Dim);
            if (patrolT < 0)
                UI.Btn(s, "Llamar a la patrullera aliada de apoyo", () => { CloseOverlay(); CallPatrol(); }, Pal.DeckLight, 20, 44);
            UI.Btn(s, "Cerrar Maletín (Tab o Esc)", CloseOverlay, Pal.Dim, 18, 40);
        }

        void OpenPause()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.34f, .25f), new Vector2(.66f, .75f));
            var s = UI.VStack(p.transform, 26, 10);
            UI.Line(s, "Pausa", 34, Pal.Sonar, true);
            UI.Line(s, "WASD mover · Shift correr · Ratón mirar\nE usar lo que tienes delante\nF visor · Q herramienta · Tab maletín\nClic derecho: fotografiar de lejos", 20, Pal.Dim);
            UI.Btn(s, "Continuar", CloseOverlay, Pal.Sealed);
            UI.Btn(s, "Volver a la base (tu avance queda guardado)", Game.GoToMenu, Pal.Dim);
        }

        public void OpenExitMenu()
        {
            SetOverlay(Mode.Menu);
            var p = UI.Panel(overlay, new Vector2(.28f, .26f), new Vector2(.72f, .74f));
            var s = UI.VStack(p.transform, 26, 10);
            UI.Line(s, "Salida por popa", 34, Pal.Sonar, true);
            int here = 0;
            foreach (var n in people)
                if (!n.State.rescued && n.State.following && Vector3.Distance(n.transform.position, player.transform.position) < 14f) here++;
            UI.Line(s, $"Te siguen {here} {(here == 1 ? "persona" : "personas")}. Pruebas A selladas: {Game.CountCategoryA()}.", 22);
            if (here > 0)
                UI.Btn(s, $"Bajar a la lancha con {here}", () => { Time.timeScale = 1; Finish(2); }, Pal.Sealed, 22, 52);
            else
                UI.Line(s, "Nadie va contigo. Habla con las personas retenidas y ofréceles salir.", 20, Pal.Dim);
            UI.Btn(s, "Retirarme yo (conservo pruebas; las víctimas quedan pendientes)", () =>
            {
                Time.timeScale = 1;
                ShowResult(0, "Te retiras del buque. Conservas las pruebas selladas. Las víctimas quedan pendientes.");
            }, Pal.DeckLight, 21, 50);
            UI.Btn(s, "Seguir a bordo", CloseOverlay, Pal.Dim);
        }

        public void Finish(int exit)
        {
            if (mode == Mode.Result) return;
            C.exitUsed = exit;
            foreach (var n in people)
            {
                var st = n.State;
                if (st.rescued) continue;
                bool saved = exit == 2
                    ? st.following && Vector3.Distance(n.transform.position, player.transform.position) < 14f
                    : st.freed; // patrullera o puente asegurado
                if (saved) { st.rescued = true; Game.AddReputation(5); }
            }
            string text = exit == 1 ? "La patrullera aliada aborda el buque. Quienes estaban libres salen con ella. El capitán ha tenido tiempo de deshacerse de lo que no sellaste."
                : exit == 2 ? "Bajas a la lancha en silencio con quienes decidieron salir. El buque no se entera hasta el amanecer."
                : "Con el capitán fuera de juego, tomas el puente y fuerzas la entrada a puerto. Es la mayor baza que puedes llevar al tribunal.";
            ShowResult(exit, text);
        }

        void ShowResult(int exit, string text)
        {
            SetOverlay(Mode.Result);
            Time.timeScale = 1;
            player.enabled = false;
            routeLine.positionCount = 0;
            C.boarded = false; C.hasCheckpoint = false; // la próxima incursión empieza por el abordaje
            int a = Game.CountCategoryA();
            // Tras la patrullera o el puente, el buque ya no está en alta mar: lo que queda es el tribunal.
            if (a >= 1 && (exit == 1 || exit == 3)) C.phase = 4;
            var p = UI.Panel(overlay, new Vector2(.22f, .1f), new Vector2(.78f, .9f));
            var s = UI.Scroll(p.transform, Vector2.zero, Vector2.one);
            UI.Line(s, exit == 0 ? "Fuera del buque" : "Salida: " + CaseData.ExitNames[exit], 34, Pal.Sonar, true);
            UI.Line(s, text, 22);
            UI.Line(s, $"Pruebas selladas: {C.evidence.Count}  ·  Categoría A: {a}  ·  Perdidas: {C.burned.Count}", 21, Pal.ThermHot);
            int reachable = CaseData.MaxIndicators(C.burned);
            if (reachable < CaseData.Indicators.Length)
                UI.Line(s, $"Con lo perdido, el máximo posible es {reachable} de {CaseData.Indicators.Length} indicadores: la Condena queda fuera de alcance.", 20, Pal.DeckLight);
            foreach (var n in people)
            {
                var st = n.State;
                UI.Line(s, $"{n.Def.name}: {(st.rescued ? "<color=#6CFF8A>a salvo</color>" : "<color=#F2A73B>pendiente, sigue a bordo</color>")}", 21);
            }
            bool someoneLeft = System.Array.Exists(people, n => !n.State.rescued);
            if (a >= 1)
            {
                UI.Line(s, "Qué sigue: en el tribunal usarás estas pruebas para conectar el buque con su dueño real.", 21, Pal.Dim);
                UI.Btn(s, "Llevar las pruebas al tribunal: Fase 4", () => Game.GoToPhase(4), Pal.Sealed, 24, 56);
            }
            else
                UI.Line(s, "Sin al menos una prueba de categoría A el caso no llega al tribunal: vuelve a abordar.", 21, Pal.Danger);
            if (a == 0 || (exit == 0 && someoneLeft))
                UI.Btn(s, a == 0 && exit == 1 ? "Volver al buque retenido a documentar (al menos 6 h después)" : "Volver a abordar (al menos 6 h después)", () => { C.hours += 6; Game.GoToPhase(3); }, Pal.DeckLight, 22, 50);
            UI.Btn(s, "Volver a la base", Game.GoToMenu, Pal.Dim, 20, 46);
            Game.Write();
        }
    }
}
