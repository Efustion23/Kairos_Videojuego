using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kairos
{
    public partial class InfiltrationController
    {
        void BuildUI()
        {
            canvas = UI.NewCanvas();
            canvasRect = (RectTransform)canvas.transform;
            visorTint = UI.Fullscreen(canvas.transform, new Color(Pal.Sealed.r, Pal.Sealed.g, Pal.Sealed.b, 0));
            visorTint.raycastTarget = false;
            marksRoot = UI.Rect(canvas.transform, "Marcas", Vector2.zero, Vector2.one);

            var tl = UI.Panel(canvas.transform, new Vector2(0, .85f), new Vector2(.34f, 1));
            objectives = UI.Text(tl.transform, "", 21);
            objectives.margin = new Vector4(14, 10, 14, 10);

            var tr = UI.Panel(canvas.transform, new Vector2(.8f, .85f), new Vector2(1, 1));
            var trs = UI.VStack(tr.transform, 12, 5);
            stateText = UI.Line(trs, "", 22, Pal.Sonar, true);
            suspBar = UI.NewBar(trs, Pal.DeckLight, 12);
            envText = UI.Line(trs, "", 17, Pal.Dim);
            breathText = UI.Line(trs, "Aliento", 17, Pal.Dim);
            staminaBar = UI.NewBar(trs, Pal.Sonar, 6);

            var bl = UI.Panel(canvas.transform, new Vector2(0, 0), new Vector2(.5f, .06f));
            tools = UI.Text(bl.transform, "", 18, Pal.Text, TextAlignmentOptions.MidlineLeft);
            tools.margin = new Vector4(14, 2, 14, 2);

            compass = UI.Text(UI.Rect(canvas.transform, "Brujula", new Vector2(.4f, .95f), new Vector2(.6f, 1)), "", 22, Pal.Sonar, TextAlignmentOptions.Center);
            lockText = UI.Text(UI.Rect(canvas.transform, "Lock", new Vector2(.28f, .86f), new Vector2(.72f, .95f)), "", 28, Pal.Danger, TextAlignmentOptions.Center, true);
            prompt = UI.Text(UI.Rect(canvas.transform, "Prompt", new Vector2(.25f, .17f), new Vector2(.75f, .24f)), "", 25, Pal.Text, TextAlignmentOptions.Center);

            var holdR = UI.Rect(canvas.transform, "Hold", new Vector2(.4f, .14f), new Vector2(.6f, .16f));
            holdRoot = holdR.gameObject;
            var hs = UI.VStack(holdR, 0, 0);
            holdBar = UI.NewBar(hs, Pal.Sonar, 14);

            var hintP = UI.Panel(canvas.transform, new Vector2(.28f, .065f), new Vector2(.72f, .135f), default, default, new Color(.02f, .16f, .17f, .92f));
            hintRoot = hintP.gameObject;
            hintText = UI.Text(hintP.transform, "", 22, Pal.Sonar, TextAlignmentOptions.Center);
            hintText.margin = new Vector4(12, 4, 12, 4);

            // Marcador del objetivo
            objMarker = UI.Rect(canvas.transform, "Objetivo", Vector2.zero, Vector2.zero, new Vector2(-70, -30), new Vector2(70, 30));
            var dia = UI.Rect(objMarker, "Rombo", new Vector2(.5f, .62f), new Vector2(.5f, .62f), new Vector2(-9, -9), new Vector2(9, 9));
            dia.localRotation = Quaternion.Euler(0, 0, 45);
            var diaImg = dia.gameObject.AddComponent<Image>(); diaImg.color = Pal.Sonar; diaImg.raycastTarget = false;
            objDistance = UI.Text(UI.Rect(objMarker, "Dist", Vector2.zero, new Vector2(1, .4f)), "", 19, Pal.Sonar, TextAlignmentOptions.Center, true);

            var ret = UI.Rect(canvas.transform, "Reticula", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-180, -130), new Vector2(180, 130));
            reticle = ret.gameObject;
            var frame = ret.gameObject.AddComponent<Outline>();
            var im = ret.gameObject.AddComponent<Image>(); im.color = new Color(1, 1, 1, .02f); im.raycastTarget = false;
            frame.effectColor = Pal.Sealed; frame.effectDistance = new Vector2(2, -2);
            framedText = UI.Text(UI.Rect(ret, "Txt", new Vector2(0, -.25f), new Vector2(1, -.02f)), "", 19, Pal.Sealed, TextAlignmentOptions.Center);
            var fb = UI.Rect(ret, "FocusBar", new Vector2(0, 1.02f), new Vector2(1, 1.06f));
            focusBar = UI.NewBar(fb, Pal.Sealed, 8);

            var bR = UI.Panel(canvas.transform, new Vector2(.28f, .05f), new Vector2(.72f, .34f));
            boardingRoot = bR.gameObject;
            var bs = UI.VStack(bR.transform, 18, 8);
            boardingText = UI.Line(bs, "", 23);
            boardBar = UI.NewBar(bs, Pal.DeckLight, 22);

            toast = UI.Toasts.Create(canvas.transform, new Vector2(.2f, .68f), new Vector2(.8f, .84f));
            saveToast = UI.Toasts.Create(canvas.transform, new Vector2(.72f, .78f), new Vector2(1, .84f));
            fade = UI.Fullscreen(canvas.transform, new Color(0, 0, 0, 0));
            fade.raycastTarget = false;
            overlay = UI.Rect(canvas.transform, "Overlay", Vector2.zero, Vector2.one);
            overlay.gameObject.SetActive(false);

            hudPanels = new[] { tl.gameObject, tr.gameObject, bl.gameObject };
            reticle.SetActive(false); holdRoot.SetActive(false); hintRoot.SetActive(false); objMarker.gameObject.SetActive(false);

            UI.SceneIntro(canvas.transform, "FASE 3", "Infiltración");

            foreach (var g in guards) AddMark(g.transform, g.displayName, Pal.Danger, 2.2f, g);
            foreach (var p in people) AddMark(p.transform, p.Def.name, Pal.ThermHot, 2.1f);
            foreach (var e in evidenceProps) AddMark(e.transform, e.Def.title, Pal.Sealed, .4f);
        }

        void AddMark(Transform t, string name, Color c, float y, GuardAI guard = null)
        {
            var r = UI.Rect(marksRoot, "Marca", Vector2.zero, Vector2.zero, new Vector2(-110, -24), new Vector2(110, 24));
            var d = UI.Rect(r, "Rombo", new Vector2(.5f, .25f), new Vector2(.5f, .25f), new Vector2(-6, -6), new Vector2(6, 6));
            d.localRotation = Quaternion.Euler(0, 0, 45);
            var img = d.gameObject.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
            var label = UI.Text(UI.Rect(r, "Txt", new Vector2(0, .45f), Vector2.one), name, 17, c, TextAlignmentOptions.Center, true);
            r.gameObject.SetActive(false);
            marks.Add(new Mark { t = t, r = r, dot = img, label = label, name = name, color = c, guard = guard, y = y });
        }

        void UpdateMarks()
        {
            bool visor = visorT > 0 && mode == Mode.Play;
            var tint = visorTint.color; tint.a = visor ? .02f : 0; visorTint.color = tint;
            foreach (var m in marks)
            {
                bool show = m.t.gameObject.activeInHierarchy && mode == Mode.Play;
                bool alarm = false;
                float dist2 = (m.t.position - player.transform.position).sqrMagnitude;
                m.label.enabled = dist2 < 225f; // de lejos solo el punto: los nombres se amontonarían
                if (show)
                {
                    if (m.guard != null)
                    {
                        alarm = (int)m.guard.state >= (int)GuardState.Sospecha && !m.guard.IsStunned;
                        show = visor || alarm;
                        var col = m.guard.state == GuardState.Sospecha ? Pal.DeckLight : Pal.Danger;
                        if (!alarm) col = Pal.Sonar;
                        m.dot.color = col; m.label.color = col;
                        m.label.text = !alarm ? m.name : m.guard.state == GuardState.Sospecha ? "?" : "!";
                        m.label.fontSize = alarm ? 34 : 17;
                        if (alarm) m.label.enabled = true;
                    }
                    else show = visor && dist2 < 2500f;
                }
                // Un guardia alerta se ve siempre, aunque esté a tu espalda.
                if (show) show = Project(m.r, m.t.position + Vector3.up * m.y, alarm);
                if (m.r.gameObject.activeSelf != show) m.r.gameObject.SetActive(show);
            }
        }

        // ---------- Tutorial ----------
        void MarkHint(string id) { if (!Game.S.hints.Contains(id)) Game.S.hints.Add(id); }

        void UpdateHint()
        {
            string id = null, text = null;
            void Offer(string i, string t, bool when = true) { if (id == null && when && !Game.S.hints.Contains(i)) { id = i; text = t; } }

            var near = player.Near;
            float nearestGuard = 1e9f;
            foreach (var g in guards) nearestGuard = Mathf.Min(nearestGuard, Vector3.Distance(g.transform.position, player.transform.position));

            if (player.Moved) MarkHint("move");
            if (player.Running) MarkHint("run");
            Offer("move", "Muévete con WASD y mira con el ratón. Sigue la línea del suelo.");
            Offer("photo", "Mantén pulsado E sin moverte para fotografiar la prueba. Queda sellada en tu maletín.", near is EvidenceProp);
            Offer("hold", "Mantén pulsado E para usar lo que tienes delante.", near != null && near.Hold > 0);
            Offer("talk", "Pulsa E para hablar. Cada persona decide por sí misma.", near is PersonNpc);
            Offer("tool", "Guardia al alcance: pulsa Q para aturdirlo. Solo una vez, y sin heridas.", nearestGuard < 3f && !C.stunUsed);
            Offer("cone", "Las zonas de color son lo que ve cada guardia. Evítalas: si no te ven, no pasa nada.", nearestGuard < 14f);
            Offer("visor", "Pulsa F: el visor muestra guardias, personas y pruebas a través de las paredes.");
            Offer("run", "Mantén Shift para correr. Es más rápido, pero hace ruido.");

            if (id != hintId) { hintId = id; hintT = 0; }
            if (id != null) { hintT += Time.deltaTime; if (hintT > 9f) MarkHint(id); }
            bool show = id != null && mode == Mode.Play;
            if (hintRoot.activeSelf != show) hintRoot.SetActive(show);
            if (show) hintText.text = text;
        }

        static readonly string[] StateNames = { "Nadie te ha visto", "Sospechan algo", "Te están buscando", "¡Te han visto! Huye", "Bloqueo de Evidencias" };

        void UpdateHud()
        {
            GuardState worst = GuardState.Tranquilo; float susp = 0;
            foreach (var g in guards)
            {
                if ((int)g.state > (int)worst) worst = g.state;
                if (g.suspicion > susp) susp = g.suspicion;
            }
            if (Lockdown && worst != GuardState.Alerta) worst = GuardState.Bloqueo;
            Music.Play(worst == GuardState.Alerta || worst == GuardState.Bloqueo ? "urgent" : "passages");
            stateText.text = StateNames[(int)worst];
            stateText.color = worst == GuardState.Tranquilo ? Pal.Sonar : worst == GuardState.Sospecha ? Pal.DeckLight : Pal.Danger;
            suspBar.Set(susp);
            suspBar.img.color = stateText.color;
            envText.text = StepsMasked ? "<color=#3FE0D0>Crujido del casco: pasos tapados</color>" : Distracted ? "<color=#3FE0D0>Tripulación distraída</color>" : $"Douglas {Ocean.Douglas:0.0} · Viento {WindKnots:0} kn";
            staminaBar.Set(player.Stamina);
            staminaBar.img.color = player.Stamina < .25f ? Pal.Danger : Pal.Sonar;
            breathText.text = player.Stamina < .25f ? "<color=#FF5940>Respiración alta: descansa</color>" : "Aliento";

            string tool = !C.stunUsed && GuardInReach() != null ? "<color=#3FE0D0>[Q] Aturdir</color>"
                : JamActive ? $"<color=#3FE0D0>Bloqueador {jamT:0} s</color>" : $"[Q] Bloqueador ({C.jammerCharges})";
            string visorLabel = visorT > 0 ? $"<color=#6CFF8A>Visor {visorT:0} s</color>" : visorCd > 0 ? $"<color=#7F9098>Visor {visorCd:0} s</color>" : "[F] Visor";
            tools.text = $"[E] Usar    {visorLabel}    {tool}    [Tab] Maletín {C.evidence.Count}/{CaseData.MaxEvidence}";

            compass.text = Where(player.transform.position);

            lockText.text = Lockdown ? $"BLOQUEO DE EVIDENCIAS {lockT:0} s" : patrolT >= 0 ? $"PATRULLERA EN {patrolT:0} s" : "";
            if (patrolT >= 0 && Lockdown) lockText.text = $"BLOQUEO {lockT:0} s  ·  PATRULLERA EN {patrolT:0} s";

            bool playing = mode == Mode.Play;
            var near = player.Near;
            prompt.text = playing && near != null && !player.Aiming ? near.Prompt : "";
            bool holding = playing && near != null && near.Hold > 0 && near.progress > 0;
            holdRoot.SetActive(holding && !player.Aiming);
            if (holding) holdBar.Set(near.progress / near.Hold);

            reticle.SetActive(playing && player.Aiming);
            if (player.Aiming)
            {
                focusBar.Set(player.Focus);
                focusBar.img.color = player.Focus >= .8f ? Pal.Sealed : Pal.DeckLight;
                framedText.color = player.FrameBlock != null ? Pal.Danger : Pal.Sealed;
                framedText.text = player.FrameBlock ?? (player.Framed != null ? $"{player.Framed.Def.title}  [{player.Framed.Def.cat}]" : "Sin objetivo");
            }

            int a = Game.CountCategoryA();
            objectives.text = $"<color=#3FE0D0><b>OBJETIVO</b></color>\n<b>{objective}</b>\n<size=18><color=#8A9AA0>Pruebas A: {a}   ·   Te siguen: {Followers.Count}</color></size>";

            // Al llegar sobra el marcador: manda el aviso de lo que puedes hacer.
            bool showMarker = playing && objTarget != null && !player.Aiming && (routeLine.positionCount == 0 || RouteLength() > 2.5f);
            if (showMarker) showMarker = Project(objMarker, objTarget.position + Vector3.up * 1.2f, true);
            if (objMarker.gameObject.activeSelf != showMarker) objMarker.gameObject.SetActive(showMarker);
            if (showMarker) objDistance.text = $"{RouteLength():0} m";

            var fc = fade.color; fc.a = Mathf.MoveTowards(fc.a, 0, Time.unscaledDeltaTime * .9f); fade.color = fc;

            UpdateMarks();
            UpdateHint();
        }

        static string Where(Vector3 p)
        {
            if (p.y < 5.5f) return p.x < -1.3f ? "Cubierta inferior, babor" : p.x > 1.3f ? "Cubierta inferior, estribor" : "Pasillo inferior";
            if (p.y > 8.5f) return "Puente";
            if (Mathf.Abs(p.x) < 5 && p.z > -20 && p.z < 2) return "Habilitación";
            return p.z < -20 ? "Cubierta de popa" : p.z > 2 ? "Cubierta de proa" : "Pasarela";
        }
    }
}
