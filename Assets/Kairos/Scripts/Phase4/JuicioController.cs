using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kairos
{
    /// Fase 4: grafo de pruebas hasta el beneficiario final, sentencia y repatriación.
    /// Cada prueba se usa con un solo clic: conecta su enlace y, a la vez, cuenta para los indicadores de la OIT.
    public class JuicioController : MonoBehaviour
    {
        static CaseState C => Game.C;
        const float StartDays = 14f;
        Canvas canvas;
        RectTransform root, board, cards, actions;
        TextMeshProUGUI clock, detail, indicatorText, verdictText, hint;
        UI.Toasts toast;
        Button sentenceBtn, reboardBtn, closeBtn;
        string selected;
        readonly List<Image> lines = new List<Image>();

        float ActionCost => Game.Has(3, 4) ? .25f : .5f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            canvas = UI.NewCanvas();
            root = UI.Rect(canvas.transform, "Root", Vector2.zero, Vector2.one);
            UI.Fullscreen(root, new Color(.02f, .05f, .07f, .9f));
            Music.Play(C.phase >= 5 ? "airy" : "pulse");
            if (C.trialDays < 0) C.trialDays = StartDays;
            if (C.phase >= 5) ShowRepatriation(); else BuildBoard();
            if (C.phase >= 5) UI.SceneIntro(canvas.transform, "", "");
            else UI.SceneIntro(canvas.transform, "FASE 4", "Juicio financiero");
        }

        // ---------- Tablero ----------
        void BuildBoard()
        {
            var top = UI.Panel(root, new Vector2(0, .93f), Vector2.one);
            clock = UI.Text(top.transform, "", 24, Pal.Text, TextAlignmentOptions.Left);
            clock.margin = new Vector4(24, 0, 24, 0);

            var left = UI.Panel(root, new Vector2(0, 0), new Vector2(.25f, .93f));
            cards = UI.Scroll(left.transform, Vector2.zero, Vector2.one);

            var center = UI.Panel(root, new Vector2(.25f, .36f), new Vector2(.75f, .93f), default, default, new Color(.02f, .06f, .09f, .95f));
            board = (RectTransform)center.transform;

            for (int i = 0; i < CaseData.Nodes.Length; i++)
            {
                var n = CaseData.Nodes[i];
                var col = i == 5 ? Pal.Danger : i == 0 ? Pal.DeckLight : Pal.Sonar;
                var node = UI.Panel(board, n.pos, n.pos, new Vector2(-95, -44), new Vector2(95, 44), new Color(col.r * .18f, col.g * .18f, col.b * .18f, 1));
                var t = UI.Text(node.transform, $"<b>{n.name}</b>\n<size=16>{n.role}</size>", 18, col, TextAlignmentOptions.Center);
                t.margin = new Vector4(6, 2, 6, 2);
            }

            var det = UI.Panel(root, new Vector2(.25f, 0), new Vector2(.75f, .36f));
            var ds = UI.VStack(det.transform, 16, 6);
            detail = UI.Line(ds, "", 19);
            hint = UI.Line(ds, "", 18, Pal.Dim);
            actions = UI.VStack(ds, 0, 6);
            actions.gameObject.AddComponent<LayoutElement>().minHeight = 50;

            var right = UI.Panel(root, new Vector2(.75f, 0), new Vector2(1, .93f));
            var rs = UI.VStack(right.transform, 14, 4);
            UI.Line(rs, "Indicadores OIT probados", 22, Pal.Sonar, true);
            indicatorText = UI.Line(rs, "", 18);
            verdictText = UI.Line(rs, "", 19, Pal.ThermHot);
            sentenceBtn = UI.Btn(rs, "Ejecutar sentencia", ExecuteSentence, Pal.Sealed, 22, 52);
            reboardBtn = UI.Btn(rs, "Volver a abordar en busca de más pruebas (-3 días)", () => { C.trialDays -= 3; Game.GoToPhase(3); }, Pal.DeckLight, 17, 44);
            closeBtn = UI.Btn(rs, "Cerrar el caso sin sentencia", () => CaseLost("Caso cerrado sin sentencia"), Pal.Danger, 18, 44);
            UI.Btn(rs, "Volver a la base", Game.GoToMenu, Pal.Dim, 18, 40);

            toast = UI.Toasts.Create(canvas.transform, new Vector2(.25f, .02f), new Vector2(.75f, .1f));
            selected = FirstUnused();
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        string FirstUnused()
        {
            foreach (var e in C.evidence) if (!C.presented.Contains(e.id)) return e.id;
            return null;
        }

        Vector2 NodeLocal(int i)
        {
            var r = board.rect;
            return new Vector2((CaseData.Nodes[i].pos.x - .5f) * r.width, (CaseData.Nodes[i].pos.y - .5f) * r.height);
        }

        void DrawEdge(string key, Color c, float width)
        {
            var p = key.Split('-');
            var seg = UI.Segment(board, NodeLocal(int.Parse(p[0])), NodeLocal(int.Parse(p[1])), c, width);
            seg.transform.SetAsFirstSibling();
            lines.Add(seg);
        }

        void DrawLinks()
        {
            foreach (var l in lines) if (l != null) Destroy(l.gameObject);
            lines.Clear();
            foreach (var key in C.links) DrawEdge(key, Pal.Sealed, 4);
            if (selected != null)
                foreach (var key in CaseData.Evidence(selected).edges)
                    if (!C.links.Contains(key)) DrawEdge(key, new Color(Pal.DeckLight.r, Pal.DeckLight.g, Pal.DeckLight.b, .7f), 3);
        }

        static string EdgeName(string key)
        {
            var p = key.Split('-');
            return CaseData.Nodes[int.Parse(p[0])].name + " - " + CaseData.Nodes[int.Parse(p[1])].name;
        }

        /// Una sola acción: enlaza (si procede) y cuenta para los indicadores.
        void UseEvidence(string id, string edge)
        {
            if (!C.presented.Contains(id)) C.presented.Add(id);
            if (edge != null && !C.links.Contains(edge)) C.links.Add(edge);
            C.trialDays -= ActionCost;
            Sfx.Play(Sfx.Seal, .8f);
            toast.Show(edge != null ? $"Enlace probado: {EdgeName(edge)}." : "Prueba admitida por el tribunal.", Pal.Sealed);
            if (selected == id && NoActionsLeft(id)) selected = FirstUnused() ?? id;
            Game.Write();
            if (C.trialDays <= 0) { TrialFailed(); return; }
            Refresh();
        }

        bool NoActionsLeft(string id)
        {
            foreach (var k in CaseData.Evidence(id).edges) if (!C.links.Contains(k)) return false;
            return C.presented.Contains(id);
        }

        /// ¿Podrían estas pruebas llegar al beneficiario final?
        bool ChainPossible()
        {
            var all = new List<string>(C.links);
            foreach (var e in C.evidence) all.AddRange(CaseData.Evidence(e.id).edges);
            return CaseData.ChainComplete(all);
        }

        /// Tras la patrullera o el puente, el buque ya está interceptado: no se vuelve a subir con garfio.
        bool ShipReachable => C.exitUsed != 1 && C.exitUsed != 3;
        bool CanReboard => ShipReachable && C.trialDays > 3;

        /// ¿Basta lo que hay en el maletín para llegar a una sentencia?
        bool SentencePossible()
        {
            var all = new List<string>();
            foreach (var e in C.evidence) all.Add(e.id);
            return ChainPossible() && CaseData.VerdictFor(CaseData.CountIndicators(all)) > 0;
        }

        void Refresh()
        {
            UI.Clear(cards);
            UI.Line(cards, $"Maletín sellado ({C.evidence.Count})", 24, Pal.Sealed, true);
            foreach (var e in C.evidence)
            {
                var d = CaseData.Evidence(e.id);
                string mark = selected == e.id ? "> " : "";
                string pres = NoActionsLeft(e.id) ? " (usada)" : "";
                var col = NoActionsLeft(e.id) ? Pal.Dim : d.cat == 'A' ? Pal.Sealed : Pal.Sonar;
                UI.Btn(cards, $"{mark}[{d.cat}] {d.title}{pres}", () => { selected = e.id; Refresh(); }, col, 17, 44);
            }
            if (C.evidence.Count == 0) UI.Line(cards, "No hay pruebas.", 18, Pal.Dim);

            int inds = CaseData.CountIndicators(C.presented);
            bool chain = CaseData.ChainComplete(C.links);
            clock.text = $"<b>FASE 4 · JUICIO FINANCIERO</b>     Antes de que cambie de nombre y bandera: <color={(C.trialDays <= 3 ? "#FF5940" : "#F2A73B")}>{C.trialDays:0.#} días</color>     Salida: {CaseData.ExitNames[C.exitUsed]}";

            UI.Clear(actions);
            if (selected != null)
            {
                var d = CaseData.Evidence(selected);
                var seal = C.evidence.Find(x => x.id == selected);
                var indNames = new List<string>();
                foreach (var i in d.indicators) indNames.Add(CaseData.Indicators[i]);
                detail.text = $"<b>{d.title}</b>  [{d.cat}]  {(Game.S.omitSensitive ? d.descSafe : d.desc)}\nPrueba: {(indNames.Count > 0 ? string.Join(", ", indNames) : "ningún indicador, pero conecta empresas")}\n<size=16><color=#6F828A>{seal.stamp} · SHA-256 {seal.sha.Substring(0, 32)}…</color></size>";
                bool any = false;
                foreach (var key in d.edges)
                {
                    if (C.links.Contains(key)) continue;
                    any = true;
                    string k = key, id = selected;
                    UI.Btn(actions, $"Conectar {EdgeName(key)}", () => UseEvidence(id, k), Pal.Sealed, 19, 42);
                }
                if (!any && !C.presented.Contains(selected) && d.indicators.Length > 0)
                {
                    string id = selected;
                    UI.Btn(actions, "Presentar ante el tribunal", () => UseEvidence(id, null), Pal.ThermHot, 19, 42);
                }
                else if (!any) UI.Line(actions, "Esta prueba ya no tiene más uso. Elige otra del maletín.", 18, Pal.Dim);
            }
            else detail.text = "No hay pruebas en el maletín.";

            hint.text = chain
                ? "Cadena completa. Cuantas más pruebas uses, mayor será la sentencia."
                : ChainPossible()
                    ? "Elige una prueba a la izquierda y úsala. Las líneas ámbar muestran lo que puede conectar."
                    : CanReboard
                        ? "<color=#F2A73B>Con estas pruebas no llegas al beneficiario final: vuelve a abordar y busca facturas y manifiesto.</color>"
                        : "<color=#F2A73B>Con estas pruebas no llegas al beneficiario final, y ya no puedes volver al buque.</color>";

            var proved = new HashSet<int>();
            foreach (var id in C.presented) foreach (var i in CaseData.Evidence(id).indicators) proved.Add(i);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < CaseData.Indicators.Length; i++)
                sb.AppendLine((proved.Contains(i) ? "<color=#6CFF8A>■ " : "<color=#7F9098>□ ") + CaseData.Indicators[i] + "</color>");
            indicatorText.text = sb.ToString();

            int verdict = CaseData.VerdictFor(inds);
            verdictText.text = $"<b>{inds} de 11</b>. {CaseData.VerdictNames[verdict]}.\n" +
                (chain ? "<color=#6CFF8A>Cadena completa hasta el beneficiario final.</color>" : "<color=#F2A73B>Falta conectar el buque con el beneficiario final.</color>") +
                "\n4 de 11: Retención · 7 de 11: Congelación · 11 de 11: Condena";
            int reachable = CaseData.MaxIndicators(C.burned);
            if (reachable < CaseData.Indicators.Length)
                verdictText.text += $"\n<color=#F2A73B>Por las pruebas perdidas, el máximo es {reachable} de {CaseData.Indicators.Length}: la Condena queda fuera de alcance.</color>";
            sentenceBtn.interactable = chain && verdict > 0;
            reboardBtn.gameObject.SetActive(ShipReachable);
            reboardBtn.interactable = CanReboard;
            closeBtn.gameObject.SetActive(!CanReboard && !SentencePossible()); // nunca queda un caso sin salida
            DrawLinks();
        }

        void ExecuteSentence()
        {
            int inds = CaseData.CountIndicators(C.presented);
            C.verdict = CaseData.VerdictFor(inds);
            C.embargo = Mathf.RoundToInt(CaseData.VerdictEmbargo[C.verdict] * CaseData.ExitEmbargoMul[C.exitUsed]);
            Game.AddBudget(Mathf.RoundToInt(C.embargo * .15f));
            Game.AddReputation(5 + C.verdict * 3);
            Game.S.sentences++;
            Sfx.Play(Sfx.Seal, 1f);
            Music.Stinger("victory");

            UI.Clear(root);
            UI.Summary(root, "Sentencia ejecutada", Pal.Sealed, new[]
            {
                $"<b>{CaseData.VerdictNames[C.verdict]}</b>",
                $"Expediente de {Game.Inspector}.",
                $"{inds} de 11 indicadores de la OIT probados. Salida: {CaseData.ExitNames[C.exitUsed]}.",
                $"<color=#FFD27A>Embargo: {C.embargo:N0} ¢. Al Fondo de Decomiso: {Mathf.RoundToInt(C.embargo * .15f):N0} ¢ (15 %).</color>",
                C.verdict == 3
                    ? "El beneficiario final de Tidewater Austral es condenado. Es la primera grieta en Tidewater Holdings: la cúpula sigue en el Índico."
                    : "La red pierde al Hai Feng 77, pero Tidewater Holdings sigue operando. Con más indicadores habrías llegado más lejos.",
            }, "Qué sigue: decidir cómo vuelve a casa cada persona rescatada.", "Atender a las personas rescatadas", () => Game.GoToPhase(5));
            Game.Write();
        }

        void TrialFailed() => CaseLost("Se acabó el reloj del juicio");

        void CaseLost(string title)
        {
            C.trialDays = 0;
            Game.S.networkReorganized = true;
            Game.AddReputation(-5);
            UI.Clear(root);
            UI.Summary(root, title, Pal.Danger, new[]
            {
                "El buque cambia de nombre y bandera. La red se reorganiza con mejores defensas: en tu próxima incursión, los guardias verán más lejos.",
            }, "Quienes rescataste siguen siendo tu responsabilidad.", "Atender a las personas rescatadas", () => Game.GoToPhase(5));
            Game.Write();
        }

        // ---------- Repatriación ----------
        void ShowRepatriation()
        {
            UI.Clear(root);
            UI.Fullscreen(root, new Color(.02f, .05f, .07f, 1f));
            var top = UI.Panel(root, new Vector2(0, .9f), Vector2.one);
            var head = UI.Text(top.transform, "", 24, Pal.Text, TextAlignmentOptions.Left);
            head.margin = new Vector4(24, 0, 24, 0);
            var body = UI.Scroll(root, new Vector2(.08f, .1f), new Vector2(.92f, .9f));

            int rescued = 0;
            foreach (var st in C.persons)
            {
                if (!st.rescued) continue;
                rescued++;
                var d = CaseData.PersonDefOf(st.id);
                UI.Line(body, $"{d.name} · {d.origin}", 28, Pal.Sonar, true);
                for (int n = 0; n < CaseData.Needs.Length; n++)
                {
                    int need = n;
                    var row = UI.HStack(body, 42);
                    UI.Line(row, CaseData.Needs[n], 20).gameObject.AddComponent<LayoutElement>().preferredWidth = 1; // columnas iguales
                    string basicLabel = CaseData.NeedCostBasic[n] > 0 ? $"Básica: {CaseData.NeedCostBasic[n]:N0} ¢" : "Sin cubrir: 0 ¢";
                    var b1 = UI.Btn(row, basicLabel, null, st.dignified[n] ? Pal.Dim : Pal.DeckLight, 18, 40);
                    var b2 = UI.Btn(row, $"Digna: {CaseData.NeedCostDignified[n]:N0} ¢", null, st.dignified[n] ? Pal.Sealed : Pal.Dim, 18, 40);
                    b1.onClick.AddListener(() => { st.dignified[need] = false; ShowRepatriation(); });
                    b2.onClick.AddListener(() =>
                    {
                        st.dignified[need] = true;
                        if (TotalCost() > Game.S.budget) { st.dignified[need] = false; }
                        ShowRepatriation();
                    });
                }
            }
            if (rescued == 0) UI.Line(body, "No hay personas rescatadas en este caso.", 24, Pal.Dim);
            head.text = $"<b>REPATRIACIÓN</b>     Presupuesto {Game.S.budget:N0} ¢     Coste de lo elegido <color=#F2A73B>{TotalCost():N0} ¢</color>     Elegir la opción digna cuesta más, da reputación y cambia su epílogo.";
            UI.Btn(body, "Confirmar y ver qué fue de cada persona", ConfirmRepatriation, Pal.Sealed, 24, 56);
        }

        int TotalCost()
        {
            int sum = 0;
            foreach (var st in C.persons)
            {
                if (!st.rescued || st.repatriated) continue;
                for (int n = 0; n < 5; n++) sum += st.dignified[n] ? CaseData.NeedCostDignified[n] : CaseData.NeedCostBasic[n];
            }
            return sum;
        }

        void ConfirmRepatriation()
        {
            Game.AddBudget(-TotalCost());
            foreach (var st in C.persons)
            {
                if (!st.rescued || st.repatriated) continue;
                st.repatriated = true;
                foreach (var dig in st.dignified) if (dig) Game.AddReputation(1);
            }
            ShowEpilogue();
        }

        void ShowEpilogue()
        {
            bool success = C.verdict > 0;
            if (success) Game.AddBudget(50000);
            C.phase = 6;
            Game.Write();
            Music.Play("transmission");

            UI.Clear(root);
            UI.Fullscreen(root, new Color(.02f, .05f, .07f, 1f));
            var s = UI.Scroll(root, new Vector2(.16f, .04f), new Vector2(.84f, .96f));
            UI.Line(s, "Epílogo", 44, Pal.Sonar, true);
            foreach (var st in C.persons)
            {
                var d = CaseData.PersonDefOf(st.id);
                UI.Line(s, d.name, 26, Pal.ThermHot, true);
                string text = !st.rescued
                    ? "Sigue a bordo. KAIROS mantiene abierta su ficha: el caso no termina hasta que vuelva."
                    : CaseData.Epilogue(d, st, C.verdict);
                UI.Line(s, text, 22);
            }
            UI.Line(s, " ", 10);
            UI.Line(s, success ? $"Caso cerrado: +50.000 ¢. Reputación {Game.S.reputation}/100. Sentencias: {Game.S.sentences}." : "Caso sin sentencia. Puedes repetirlo desde la base.", 22, Pal.Sealed);
            UI.Line(s, "Expediente de " + Game.Inspector + ". Tidewater Holdings sigue ahí fuera.", 20, Pal.Dim);
            UI.Line(s, "Ningún océano es tan vasto ni ninguna noche tan oscura como para ocultar la dignidad humana.", 26, Pal.Sonar, false, TextAlignmentOptions.Center);
            UI.Btn(s, "Volver a la base", Game.GoToMenu, Pal.Sealed, 24, 56);
        }
    }
}
