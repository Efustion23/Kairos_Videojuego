using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kairos
{
    public partial class SonarController
    {
        void BuildUI()
        {
            var canvas = UI.NewCanvas();
            canvasRoot = canvas.transform;

            // Brújula táctica náutica superior (entre panel izquierdo y panel derecho, con margen seguro)
            var compassP = UI.Panel(canvas.transform, new Vector2(.278f, .928f), new Vector2(.645f, .988f));
            compassPanelObj = compassP.gameObject;
            compassRect = (RectTransform)compassP.transform;
            compassText = UI.Text(compassP.transform, "RUMBO: 000°  |  BLANCO: 000°", 18, Pal.Sonar, TextAlignmentOptions.Center);
            compassText.margin = new Vector4(8, 2, 8, 2);
            compassText.enableAutoSizing = true; compassText.fontSizeMin = 12; compassText.fontSizeMax = 18;

            // --- Panel izquierdo (Estado de la lancha) con botón de minimizar ---
            var left = UI.Panel(canvas.transform, new Vector2(.008f, .665f), new Vector2(.268f, .988f)); // a la altura de su contenido (incluida la línea de estado del dron)
            leftPanelObj = left.gameObject;
            var ls = UI.VStack(left.transform, 12, 5);
            var leftHead = UI.HStack(ls, 30, 6);
            leftHead.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var leftTitle = UI.Line(leftHead, $"FASE 2 · {CaseData.TargetFor(C.sector).name.ToUpperInvariant()}", 19, Pal.Sonar, true, TextAlignmentOptions.MidlineLeft);
            leftTitle.enableAutoSizing = true; leftTitle.fontSizeMin = 13; leftTitle.fontSizeMax = 19;
            leftTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var leftMinBtn = UI.Btn(leftHead, "–", () => SetLeftCollapsed(true), Pal.Dim, 18, 28);
            var leftMinLe = leftMinBtn.gameObject.GetComponent<LayoutElement>();
            leftMinLe.minWidth = 34; leftMinLe.preferredWidth = 34;

            status = UI.Line(ls, "", 17);
            UI.Line(ls, "Sospecha del buque", 15, Pal.Dim);
            suspBar = UI.NewBar(ls, Pal.Danger, 10);
            crestText = UI.Line(ls, "Ola", 15, Pal.Dim);
            crestBar = UI.NewBar(ls, Pal.Sonar, 14);
            UI.Line(ls, "W/S marcha · A/D timón · Espacio pulso · Tab casco\nH hidrófono · T dron · V térmica · Clic der. cámara · Esc pausa", 13.5f, Pal.Dim);

            // Pastilla minimizada del panel izquierdo
            var leftMini = UI.Panel(canvas.transform, new Vector2(.008f, .928f), new Vector2(.268f, .988f));
            leftMiniObj = leftMini.gameObject;
            var lmRow = UI.Rect(leftMini.transform, "Row", Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-8, -4));
            var lmH = lmRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            lmH.spacing = 6; lmH.childControlHeight = true; lmH.childControlWidth = true; lmH.childForceExpandWidth = false;
            leftMiniText = UI.Line(lmRow, CaseData.TargetFor(C.sector).name, 15, Pal.Sonar, true, TextAlignmentOptions.MidlineLeft);
            leftMiniText.enableAutoSizing = true; leftMiniText.fontSizeMin = 12; leftMiniText.fontSizeMax = 15;
            leftMiniText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var leftExpBtn = UI.Btn(lmRow, "+", () => SetLeftCollapsed(false), Pal.Sonar, 17, 28);
            var leftExpLe = leftExpBtn.gameObject.GetComponent<LayoutElement>();
            leftExpLe.minWidth = 34; leftExpLe.preferredWidth = 34;
            leftMiniObj.SetActive(false);

            // --- Panel derecho (Lectura del casco) con scroll interno, pie fijo y botón Minimizar / Tab ---
            var right = UI.Panel(canvas.transform, new Vector2(.655f, .175f), new Vector2(.992f, .988f));
            rightPanelObj = right.gameObject;

            var rHead = UI.Rect(right.transform, "Header", new Vector2(0, .925f), Vector2.one, new Vector2(14, 4), new Vector2(-12, -6));
            var rHeadH = rHead.gameObject.AddComponent<HorizontalLayoutGroup>();
            rHeadH.spacing = 8; rHeadH.childControlHeight = true; rHeadH.childControlWidth = true; rHeadH.childForceExpandWidth = false;
            var rTitle = UI.Line(rHead, "Lectura del casco", 21, Pal.Sonar, true, TextAlignmentOptions.MidlineLeft);
            rTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var rMinBtn = UI.Btn(rHead, "Minimizar [Tab]", () => SetRightCollapsed(true), Pal.Dim, 14, 30);
            var rMinLe = rMinBtn.gameObject.GetComponent<LayoutElement>();
            rMinLe.minWidth = 148; rMinLe.preferredWidth = 148;

            // Cuerpo con desplazamiento limpio: ningún texto se recorta ni se aplasta al acumular lecturas
            var rs = UI.Scroll(right.transform, new Vector2(0, .235f), new Vector2(1, .92f), new Vector2(6, 0), new Vector2(-6, 0));
            var rsVlg = rs.GetComponent<VerticalLayoutGroup>();
            if (rsVlg != null) { rsVlg.padding = new RectOffset(8, 12, 4, 6); rsVlg.spacing = 5; }
            UI.Line(rs, "Encuentra dónde hay personas. La carga da una masa densa y homogénea; un espacio habitado suena hueco y con objetos sueltos.", 14.5f, Pal.Dim);
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                var row = UI.HStack(rs, 32, 6);
                selectBtn[i] = UI.Btn(row, CaseData.Compartments[i].name, () => { selected = k; RefreshPanel(); }, null, 15, 32);
                classBtn[i] = UI.Btn(row, "Aquí hay personas", () => MarkPersons(k), Pal.DeckLight, 15, 32);
                scanBars[i] = UI.NewBar(rs, Pal.Sonar, 5);
                cardText[i] = UI.Line(rs, "", 14.5f);
            }
            UI.Line(rs, "Opcional: H escucha el compartimento elegido (hidrófono) y T lanza el dron térmico. Suben la confianza.", 14, Pal.Dim);

            // Pie fijo del panel derecho: hidrófono, confianza y botones de abordaje siempre visibles
            var rFoot = UI.VStack(right.transform, 10, 5, Vector2.zero, new Vector2(1, .232f));
            listenBar = UI.NewBar(rFoot, Pal.ThermHot, 6);
            confidenceText = UI.Line(rFoot, "", 19, Pal.Text, true);
            confBar = UI.NewBar(rFoot, Pal.Sealed, 10);
            var bottom = UI.HStack(rFoot, 42, 8);
            board = UI.Btn(bottom, "Abordar: Fase 3", () => UI.Summary(canvas.transform, "Compartimento localizado", Pal.Sealed, new[]
            {
                $"Confianza: <b>{C.confidence:0} %</b>. {CaseData.Compartments[CaseData.PersonsCompartment].name}: {CaseData.Compartments[CaseData.PersonsCompartment].density}.",
                C.dumped ? "<color=#F2A73B>Te detectaron y tiraron pruebas al mar. El caso sigue, pero la Condena queda fuera de alcance.</color>" : "El buque no se ha enterado de que estás aquí.",
            }, "Qué sigue: subes a bordo, reúnes pruebas y hablas con las personas retenidas.", "Lanzar el garfio", () => Game.GoToPhase(3)), Pal.Sealed, 18, 42);
            UI.Btn(bottom, "Volver a base", Game.GoToMenu, Pal.Dim, 17, 42);

            // Pastilla minimizada del panel derecho (arriba derecha: libera toda la vista del barco y el mar)
            var rightMini = UI.Panel(canvas.transform, new Vector2(.725f, .928f), new Vector2(.992f, .988f));
            rightMiniObj = rightMini.gameObject;
            var rmRow = UI.Rect(rightMini.transform, "Row", Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-8, -4));
            var rmH = rmRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rmH.spacing = 8; rmH.childControlHeight = true; rmH.childControlWidth = true; rmH.childForceExpandWidth = false;
            rightMiniText = UI.Line(rmRow, "Casco · Conf: 0%", 15, Pal.Text, true, TextAlignmentOptions.MidlineLeft);
            rightMiniText.enableAutoSizing = true; rightMiniText.fontSizeMin = 12; rightMiniText.fontSizeMax = 15;
            rightMiniText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            rightMiniBtn = UI.Btn(rmRow, "+ Casco [Tab]", () => SetRightCollapsed(false), Pal.Sonar, 14, 28);
            var rmBtnLe = rightMiniBtn.gameObject.GetComponent<LayoutElement>();
            rmBtnLe.minWidth = 132; rmBtnLe.preferredWidth = 132;
            rightMiniObj.SetActive(false);

            // Pista táctica pegada al borde inferior (sin tapar la lancha ni su estela) y toasts encima
            var hintP = UI.Panel(canvas.transform, new Vector2(.10f, .014f), new Vector2(.645f, .072f), default, default, new Color(.02f, .16f, .17f, .88f));
            hintRect = (RectTransform)hintP.transform;
            hintText = UI.Text(hintP.transform, "", 19, Pal.Sonar, TextAlignmentOptions.Center);
            hintText.margin = new Vector4(12, 2, 12, 2);
            hintText.enableAutoSizing = true; hintText.fontSizeMin = 13; hintText.fontSizeMax = 19;

            toast = UI.Toasts.Create(canvas.transform, new Vector2(.10f, .080f), new Vector2(.645f, .150f));
            toastRect = (RectTransform)toast.transform;

            UI.SceneIntro(canvas.transform, "FASE 2", "Escaneo con sonar", CaseData.SectorIntroSubtitle(C.sector));
        }

        void SetLeftCollapsed(bool collapsed)
        {
            leftCollapsed = collapsed;
            if (leftPanelObj != null) leftPanelObj.SetActive(!collapsed);
            if (leftMiniObj != null) leftMiniObj.SetActive(collapsed);
        }

        void SetRightCollapsed(bool collapsed)
        {
            rightCollapsed = collapsed;
            if (rightPanelObj != null) rightPanelObj.SetActive(!collapsed);
            if (rightMiniObj != null) rightMiniObj.SetActive(collapsed);

            // Al minimizar el panel derecho, ensanchar suavemente la brújula y centrar la barra inferior de pista
            if (compassRect != null)
            {
                compassRect.anchorMin = new Vector2(.278f, .928f);
                compassRect.anchorMax = new Vector2(collapsed ? .715f : .645f, .988f);
            }
            if (hintRect != null)
            {
                hintRect.anchorMin = new Vector2(collapsed ? .20f : .10f, .014f);
                hintRect.anchorMax = new Vector2(collapsed ? .80f : .645f, .072f);
            }
            if (toastRect != null)
            {
                toastRect.anchorMin = new Vector2(collapsed ? .20f : .10f, .080f);
                toastRect.anchorMax = new Vector2(collapsed ? .80f : .645f, .150f);
            }
        }

        void MarkPersons(int i)
        {
            var k = CaseData.Compartments[i];
            if (scan[i] < 1) { toast.Show($"{k.name}: aún sin lectura. Sigue disparando pulsos.", Pal.DeckLight); return; }
            if (k.kind == 1)
            {
                C.marked = i;
                Sfx.Play(Sfx.Seal, .8f);
                toast.Show($"{k.name}: {k.density}. Aquí hay alguien.", Pal.Sealed, 5);
            }
            else toast.Show($"{k.name}: {k.density}. {(k.kind == 0 ? "Es carga congelada" : "Es maquinaria")}, no personas. Prueba con otro.", Pal.DeckLight, 5);
            RefreshPanel();
        }

        /// Un solo paso a la vez, según lo que falte.
        string CurrentHint(float d)
        {
            if (droneActive) return "Dron térmico: W/S avanzar, A/D girar. Vuela sobre el barco y pulsa T para volver.";
            if (d > PulseRange) return $"Mantén W para acercarte: el sonar llega a {PulseRange:0} m.";
            if (scan[CaseData.PersonsCompartment] < 1 && C.marked < 0) return "Pulsa Espacio cuando la barra de ola esté en verde. Repite hasta llenar las lecturas.";
            if (C.marked != CaseData.PersonsCompartment) return "Compara las lecturas y pulsa «Aquí hay personas» en la que suene hueca.";
            return C.confidence >= CaseData.ConfidenceToBoard ? "Ya tienes confianza suficiente: pulsa «Abordar»." : "Sigue escaneando ese compartimento.";
        }

        void RefreshPanel()
        {
            for (int i = 0; i < 5; i++)
            {
                var k = CaseData.Compartments[i];
                int sig = C.scanSignals[i];
                string dens = (sig & Bit_Density) != 0 ? k.density : scan[i] > 0 ? $"{scan[i] * 100:0} %" : "—";
                string th = (sig & Bit_Thermal) != 0 ? $"{k.temp}. {k.cycle}. Agua: {k.water}" : thermal[i] > 0 ? $"{thermal[i] * 100:0} %" : "—";
                string sn = (sig & Bit_Sound) != 0 ? k.sound : "—";
                cardText[i].text = $"<color=#3FE0D0>Densidad</color> {dens}  <color=#FFD27A>Térmica</color> {th}  <color=#9fb>Sonido</color> {sn}";
                scanBars[i].Set(scan[i]);
                UI.SetLabel(classBtn[i], C.marked == i ? "Personas: marcado" : "Aquí hay personas");
                UI.SetLabel(selectBtn[i], (selected == i ? "> " : "") + k.name);
            }
            C.confidence = CaseData.ComputeConfidence(C.marked, C.scanSignals);
            confidenceText.text = $"Confianza: {C.confidence:0} % (mínimo {CaseData.ConfidenceToBoard} %)";
            confBar.Set(C.confidence / 100f);
            bool ready = C.confidence >= CaseData.ConfidenceToBoard;
            board.interactable = ready;
            if (rightMiniText != null)
                rightMiniText.text = ready
                    ? $"<b>Casco</b> · <color=#6CFF8A>LISTO ({C.confidence:0}%)</color>"
                    : $"<b>Casco</b> · Conf: <color=#3FE0D0>{C.confidence:0}%</color>";
            if (rightMiniBtn != null)
                UI.SetLabel(rightMiniBtn, ready ? "Abordar [Tab]" : "+ Casco [Tab]");
        }
    }
}
