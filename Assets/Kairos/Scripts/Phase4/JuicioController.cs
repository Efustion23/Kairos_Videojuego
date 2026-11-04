using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kairos
{
    /// Fase 4: grafo de pruebas hasta el beneficiario final, sentencia y repatriación.
    public class JuicioController : MonoBehaviour
    {
        static CaseState C => Game.C;
        Canvas canvas;
        RectTransform root, board;
        TextMeshProUGUI clock, detail, indicatorText, verdictText, hint;
        UI.Toasts toast;
        Button sentenceBtn, presentBtn, reboardBtn;
        string selected;
        int nodeA = -1;
        readonly List<Button> nodeBtns = new List<Button>();
        RectTransform cards;
        readonly List<Image> lines = new List<Image>();
        Vector2 boardSize = new Vector2(960, 620);

        void Start()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            canvas = UI.NewCanvas();
            root = UI.Rect(canvas.transform, "Root", Vector2.zero, Vector2.one);
            UI.Fullscreen(root, new Color(.02f, .05f, .07f, .9f));
            Sfx.Loop(gameObject, Sfx.Drone, .3f);
            if (C.trialDays < 0) C.trialDays = Random.Range(9, 15);
            if (C.phase >= 5) ShowRepatriation(); else BuildBoard();
        }

        // ---------- Tablero ----------
        void BuildBoard()
        {
            var top = UI.Panel(root, new Vector2(0, .93f), Vector2.one);
            clock = UI.Text(top.transform, "", 24, Pal.Text, TextAlignmentOptions.Left);
            clock.margin = new Vector4(24, 0, 24, 0);

            var left = UI.Panel(root, new Vector2(0, 0), new Vector2(.25f, .93f));
            cards = UI.Scroll(left.transform, Vector2.zero, Vector2.one);

            var center = UI.Panel(root, new Vector2(.25f, .3f), new Vector2(.75f, .93f), default, default, new Color(.02f, .06f, .09f, .95f));
            board = (RectTransform)center.transform;

            for (int i = 0; i < CaseData.Nodes.Length; i++)
            {
                int k = i;
                var n = CaseData.Nodes[i];
                var slot = UI.Rect(board, "Node" + i, n.pos, n.pos, new Vector2(-95, -44), new Vector2(95, 44));
                var col = i == 5 ? Pal.Danger : i == 0 ? Pal.DeckLight : Pal.Sonar;
                nodeBtns.Add(UI.Btn(slot, $"<b>{n.name}</b>\n<size=15>{n.role}</size>", () => ClickNode(k), col, 18, 80));
            }

            var det = UI.Panel(root, new Vector2(.25f, 0), new Vector2(.75f, .3f));
            var ds = UI.VStack(det.transform, 16, 6);
            detail = UI.Line(ds, "", 19);
            hint = UI.Line(ds, "", 18, Pal.Dim);
            presentBtn = UI.Btn(ds, "Presentar ante el tribunal (0,5 días)", Present, Pal.ThermHot, 20, 42);

            var right = UI.Panel(root, new Vector2(.75f, 0), new Vector2(1, .93f));
            var rs = UI.VStack(right.transform, 14, 4);
            UI.Line(rs, "Indicadores OIT probados", 22, Pal.Sonar, true);
            indicatorText = UI.Line(rs, "", 16);
            verdictText = UI.Line(rs, "", 19, Pal.ThermHot);
            sentenceBtn = UI.Btn(rs, "Ejecutar sentencia", ExecuteSentence, Pal.Sealed, 22, 52);
            reboardBtn = UI.Btn(rs, "Volver a abordar en busca de más pruebas (-3 días)", () => { C.trialDays -= 3; Game.GoToPhase(3); }, Pal.DeckLight, 17, 44);
            UI.Btn(rs, "Volver a la base", Game.GoToMenu, Pal.Dim, 18, 40);

            toast = UI.Toasts.Create(canvas.transform, new Vector2(.25f, .02f), new Vector2(.75f, .12f));
            Refresh();
            DrawLinks();
        }

        Vector2 NodeLocal(int i)
        {
            var r = board.rect;
            return new Vector2((CaseData.Nodes[i].pos.x - .5f) * r.width, (CaseData.Nodes[i].pos.y - .5f) * r.height);
        }

        void DrawLinks()
        {
            foreach (var l in lines) if (l != null) Destroy(l.gameObject);
            lines.Clear();
            Canvas.ForceUpdateCanvases();
            foreach (var key in C.links)
            {
                var p = key.Split('-');
                var seg = UI.Segment(board, NodeLocal(int.Parse(p[0])), NodeLocal(int.Parse(p[1])), Pal.Sealed, 4);
                seg.transform.SetAsFirstSibling();
                lines.Add(seg);
            }
        }

        void ClickNode(int i)
        {
            if (selected == null) { toast.Show("Primero elige una prueba del maletín.", Pal.DeckLight, 2f); return; }
            if (nodeA < 0) { nodeA = i; Refresh(); return; }
            if (nodeA == i) { nodeA = -1; Refresh(); return; }
            string key = CaseData.EdgeKey(nodeA, i);
            string aName = CaseData.Nodes[nodeA].name, bName = CaseData.Nodes[i].name;
            nodeA = -1;
            if (C.links.Contains(key)) { toast.Show("Ese enlace ya está probado.", Pal.Dim, 2f); Refresh(); return; }
            var ev = CaseData.Evidence(selected);
            if (System.Array.IndexOf(ev.edges, key) >= 0)
            {
                C.links.Add(key);
                C.trialDays -= 1;
                Sfx.Play(Sfx.Seal, .8f);
                toast.Show($"Enlace probado: {aName} ↔ {bName}. -1 día.", Pal.Sealed);
                DrawLinks();
            }
            else
            {
                C.trialDays -= 2;
                Sfx.Play(Sfx.Alarm, .3f);
                toast.Show($"El tribunal no admite «{ev.title}» para ese enlace. -2 días.", Pal.Danger);
            }
            AfterTime();
        }

        void Present()
        {
            if (selected == null || C.presented.Contains(selected)) return;
            C.presented.Add(selected);
            C.trialDays -= .5f;
            Sfx.Play(Sfx.Seal, .6f);
            AfterTime();
        }

        void AfterTime()
        {
            Game.Write();
            if (C.trialDays <= 0) { TrialFailed(); return; }
            Refresh();
        }

        void Refresh()
        {
            UI.Clear(cards);
            UI.Line(cards, $"Maletín sellado ({C.evidence.Count})", 24, Pal.Sealed, true);
            foreach (var e in C.evidence)
            {
                var d = CaseData.Evidence(e.id);
                string mark = selected == e.id ? "> " : "";
                string pres = C.presented.Contains(e.id) ? " (presentada)" : "";
                var col = d.cat == 'A' ? Pal.Sealed : d.cat == 'B' ? Pal.Sonar : Pal.Dim;
                UI.Btn(cards, $"{mark}[{d.cat}] {d.title}{pres}", () => { selected = e.id; nodeA = -1; Refresh(); }, col, 17, 44);
            }
            if (C.evidence.Count == 0) UI.Line(cards, "No hay pruebas.", 18, Pal.Dim);

            int inds = CaseData.CountIndicators(C.presented);
            bool chain = CaseData.ChainComplete(C.links);
            clock.text = $"<b>FASE 4 · JUICIO FINANCIERO</b>     Antes de que cambie de nombre y bandera: <color={(C.trialDays <= 3 ? "#FF5940" : "#F2A73B")}>{C.trialDays:0.#} días</color>     Salida: {CaseData.ExitNames[C.exitUsed]}";

            if (selected != null)
            {
                var d = CaseData.Evidence(selected);
                var seal = C.evidence.Find(x => x.id == selected);
                var indNames = new List<string>();
                foreach (var i in d.indicators) indNames.Add(CaseData.Indicators[i]);
                string edgeHint = "";
                if (Game.Has(3, 4) && d.edges.Length > 0)
                {
                    var names = new List<string>();
                    foreach (var k in d.edges) { var p = k.Split('-'); names.Add(CaseData.Nodes[int.Parse(p[0])].name + " ↔ " + CaseData.Nodes[int.Parse(p[1])].name); }
                    edgeHint = "\n<color=#3FE0D0>Desencriptador: admite " + string.Join(", ", names) + "</color>";
                }
                detail.text = $"<b>{d.title}</b>  [{d.cat}]  {(Game.S.omitSensitive ? d.descSafe : d.desc)}\nPrueba indicadores: {(indNames.Count > 0 ? string.Join(", ", indNames) : "ninguno; sirve para enlazar")}\n<size=14><color=#55666E>{seal.stamp} · SHA-256 {seal.sha.Substring(0, 32)}…</color></size>{edgeHint}";
                hint.text = nodeA >= 0 ? $"Origen: <b>{CaseData.Nodes[nodeA].name}</b>. Ahora elige el otro extremo." : "Conecta con esta prueba: haz clic en dos nodos del grafo.";
                presentBtn.interactable = d.indicators.Length > 0 && !C.presented.Contains(selected);
                UI.SetLabel(presentBtn, C.presented.Contains(selected) ? "Ya presentada" : d.indicators.Length == 0 ? "No prueba indicadores: úsala en el grafo" : "Presentar ante el tribunal (0,5 días)");
            }
            else
            {
                detail.text = "Elige una prueba del maletín. Conecta el buque con su beneficiario final y presenta pruebas para demostrar los indicadores de la OIT.";
                hint.text = "Un enlace correcto cuesta 1 día; uno incorrecto, 2. Presentar una prueba cuesta medio día.";
                presentBtn.interactable = false;
            }

            var proved = new HashSet<int>();
            foreach (var id in C.presented) foreach (var i in CaseData.Evidence(id).indicators) proved.Add(i);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < CaseData.Indicators.Length; i++)
                sb.AppendLine((proved.Contains(i) ? "<color=#6CFF8A>■ " : "<color=#55666E>□ ") + CaseData.Indicators[i] + "</color>");
            indicatorText.text = sb.ToString();

            int verdict = CaseData.VerdictFor(inds);
            verdictText.text = $"<b>{inds} de 11</b>. {CaseData.VerdictNames[verdict]}.\n" +
                (chain ? "<color=#6CFF8A>Cadena completa hasta el beneficiario final.</color>" : "<color=#F2A73B>Falta conectar el buque con el beneficiario final.</color>") +
                "\n4 de 11: Retención · 7 de 11: Congelación · 11 de 11: Condena";
            sentenceBtn.interactable = chain && verdict > 0;
            reboardBtn.interactable = C.trialDays > 3;
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

            UI.Clear(root);
            UI.Fullscreen(root, new Color(.02f, .05f, .07f, 1f));
            var p = UI.Panel(root, new Vector2(.22f, .18f), new Vector2(.78f, .82f));
            var s = UI.VStack(p.transform, 34, 14);
            UI.Line(s, "Sentencia ejecutada", 42, Pal.Sealed, true);
            UI.Line(s, CaseData.VerdictNames[C.verdict], 30, Pal.Text, true);
            UI.Line(s, $"{inds} de 11 indicadores de la OIT probados. Salida: {CaseData.ExitNames[C.exitUsed]}.", 23);
            UI.Line(s, $"Embargo: {C.embargo:N0} ¢. Al Fondo de Decomiso: {Mathf.RoundToInt(C.embargo * .15f):N0} ¢ (15 %).", 23, Pal.ThermHot);
            UI.Line(s, C.verdict == 3
                ? "El beneficiario final de Tidewater Austral es condenado. Es la primera grieta en Tidewater Holdings: la cúpula sigue en el Índico."
                : "La red pierde al Hai Feng 77, pero Tidewater Holdings sigue operando. Con más indicadores habrías llegado más lejos.", 22, Pal.Dim);
            UI.Btn(s, "Atender a las personas rescatadas", () => Game.GoToPhase(5), Pal.Sealed, 24, 56);
            Game.Write();
        }

        void TrialFailed()
        {
            C.failed = true;
            C.trialDays = 0;
            Game.S.networkReorganized = true;
            Game.AddReputation(-5);
            UI.Clear(root);
            UI.Fullscreen(root, new Color(.02f, .05f, .07f, 1f));
            var p = UI.Panel(root, new Vector2(.22f, .22f), new Vector2(.78f, .78f));
            var s = UI.VStack(p.transform, 34, 14);
            UI.Line(s, "Se acabó el reloj del juicio", 40, Pal.Danger, true);
            UI.Line(s, "El buque cambia de nombre y bandera. La red se reorganiza con mejores defensas: en tu próxima incursión, los guardias verán más lejos.", 24);
            UI.Line(s, "Quienes rescataste siguen siendo tu responsabilidad.", 22, Pal.Dim);
            UI.Btn(s, "Atender a las personas rescatadas", () => Game.GoToPhase(5), Pal.Sealed, 24, 56);
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
                    UI.Line(row, CaseData.Needs[n], 20).gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 1; // columnas iguales
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
            if (success) { Game.AddBudget(50000); Game.S.casesClosed++; }
            C.phase = 6;
            Game.Write();

            UI.Clear(root);
            UI.Fullscreen(root, new Color(.02f, .05f, .07f, 1f));
            var s = UI.Scroll(root, new Vector2(.16f, .04f), new Vector2(.84f, .96f));
            UI.Line(s, "Epílogo", 44, Pal.Sonar, true);
            foreach (var st in C.persons)
            {
                var d = CaseData.PersonDefOf(st.id);
                int dignified = 0;
                foreach (var x in st.dignified) if (x) dignified++;
                UI.Line(s, d.name, 26, Pal.ThermHot, true);
                string text = !st.rescued
                    ? "Sigue a bordo. KAIROS mantiene abierta su ficha: el caso no termina hasta que vuelva."
                    : dignified >= 3 ? d.epilogueDignified : d.epilogueBasic;
                UI.Line(s, text, 22);
            }
            UI.Line(s, " ", 10);
            UI.Line(s, success ? $"Caso cerrado: +50.000 ¢. Reputación {Game.S.reputation}/100. Sentencias: {Game.S.sentences}." : "Caso sin sentencia. Puedes reabrirlo desde la base.", 22, Pal.Sealed);
            UI.Line(s, "Golfo de Guinea e Índico siguen bloqueados en este build. Tidewater Holdings sigue ahí fuera.", 20, Pal.Dim);
            UI.Line(s, "Ningún océano es tan vasto ni ninguna noche tan oscura como para ocultar la dignidad humana.", 26, Pal.Sonar, false, TextAlignmentOptions.Center);
            UI.Btn(s, "Volver a la base", Game.GoToMenu, Pal.Sealed, 24, 56);
        }
    }
}
