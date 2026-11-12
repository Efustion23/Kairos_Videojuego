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

            // Brújula táctica náutica superior (Alexander - Tarea S12-1)
            var compassP = UI.Panel(canvas.transform, new Vector2(.30f, .93f), new Vector2(.70f, .99f));
            compassText = UI.Text(compassP.transform, "RUMBO: 000°  |  BLANCO: 000°", 17, Pal.Sonar, TextAlignmentOptions.Center);
            compassText.margin = new Vector4(6, 2, 6, 2);
            var left = UI.Panel(canvas.transform, new Vector2(0, .62f), new Vector2(.27f, 1));
            var ls = UI.VStack(left.transform, 14, 7);
            UI.Line(ls, "FASE 2 · ESCANEO", 26, Pal.Sonar, true);
            status = UI.Line(ls, "", 20);
            UI.Line(ls, "Sospecha del buque", 18, Pal.Dim);
            suspBar = UI.NewBar(ls, Pal.Danger, 12);
            crestText = UI.Line(ls, "Ola", 18, Pal.Dim);
            crestBar = UI.NewBar(ls, Pal.Sonar, 18);
            UI.Line(ls, "W/S marcha · A/D timón · Espacio pulso\nH hidrófono · T dron · Clic der. cámara · Esc pausa", 18, Pal.Dim);

            var right = UI.Panel(canvas.transform, new Vector2(.64f, .3f), new Vector2(1, 1));
            var rs = UI.VStack(right.transform, 12, 5);
            UI.Line(rs, "Lectura del casco", 24, Pal.Sonar, true);
            UI.Line(rs, "Encuentra dónde hay personas. La carga da una masa densa y homogénea; un espacio habitado suena hueco y con objetos sueltos.", 18, Pal.Dim);
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                var row = UI.HStack(rs, 36);
                selectBtn[i] = UI.Btn(row, CaseData.Compartments[i].name, () => { selected = k; RefreshPanel(); }, null, 18, 36);
                classBtn[i] = UI.Btn(row, "Aquí hay personas", () => MarkPersons(k), Pal.DeckLight, 18, 36);
                scanBars[i] = UI.NewBar(rs, Pal.Sonar, 5);
                cardText[i] = UI.Line(rs, "", 17);
            }
            UI.Line(rs, "Opcional: H escucha el compartimento elegido y T lanza el dron térmico. Suben la confianza.", 17, Pal.Dim);
            listenBar = UI.NewBar(rs, Pal.ThermHot, 6);
            confidenceText = UI.Line(rs, "", 22, Pal.Text, true);
            confBar = UI.NewBar(rs, Pal.Sealed, 12);
            var bottom = UI.HStack(rs, 46);
            board = UI.Btn(bottom, "Abordar: Fase 3", () => UI.Summary(canvas.transform, "Compartimento localizado", Pal.Sealed, new[]
            {
                $"Confianza: <b>{C.confidence:0} %</b>. {CaseData.Compartments[CaseData.PersonsCompartment].name}: {CaseData.Compartments[CaseData.PersonsCompartment].density}.",
                C.dumped ? "<color=#F2A73B>Te detectaron y tiraron pruebas al mar. El caso sigue, pero la Condena queda fuera de alcance.</color>" : "El buque no se ha enterado de que estás aquí.",
            }, "Qué sigue: subes a bordo, reúnes pruebas y hablas con las personas retenidas.", "Lanzar el garfio", () => Game.GoToPhase(3)), Pal.Sealed, 21, 46);
            UI.Btn(bottom, "Volver a base", Game.GoToMenu, Pal.Dim, 19, 46);
            toast = UI.Toasts.Create(canvas.transform, new Vector2(.05f, .03f), new Vector2(.62f, .13f));

            var hintP = UI.Panel(canvas.transform, new Vector2(.08f, .15f), new Vector2(.62f, .24f), default, default, new Color(.02f, .16f, .17f, .92f));
            hintText = UI.Text(hintP.transform, "", 23, Pal.Sonar, TextAlignmentOptions.Center);
            hintText.margin = new Vector4(12, 4, 12, 4);
            UI.SceneIntro(canvas.transform, "FASE 2", "Escaneo con sonar");
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
            board.interactable = C.confidence >= CaseData.ConfidenceToBoard;
        }
    }
}
