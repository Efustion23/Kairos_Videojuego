using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kairos
{
    /// Fase 1: globo 3D con capas de satélite. Condición de paso: IR ≥ 60.
    public class RastreoController : MonoBehaviour
    {
        public Camera cam;

        Transform globe;
        Material cloudMat;
        Quaternion targetRot;
        float dist = 3.9f, targetDist = 3.9f;
        bool zoomed;
        ShipDef selected;
        Vector2 dragLast;
        bool wasDown;

        class Marker { public ShipDef ship; public GameObject go, halo, ring; public TextMeshProUGUI label; public LineRenderer track; }
        readonly List<Marker> markers = new List<Marker>();
        readonly List<(GameObject go, int sector, TextMeshProUGUI label)> pins = new List<(GameObject, int, TextMeshProUGUI)>();

        Canvas canvas;
        TextMeshProUGUI topBar, objective, dossier, irText;
        UI.Bar irBar;
        Button[] layerBtns;
        TextMeshProUGUI[] layerInfo;
        Button deploy;
        UI.Toasts toast;
        RectTransform rightPanel, labels;

        static CaseState C => Game.C;
        float Decay => Game.Has(3, 2) ? 2f : 5f;

        void Start()
        {
            if ((C.revealed & 1) == 0) C.revealed |= 1;
            BuildGlobe();
            BuildUI();
            Sfx.Loop(gameObject, Sfx.Drone, .3f);
            foreach (var s in CaseData.Ships) AddMarker(s);
            for (int i = 0; i < CaseData.Sectors.Length; i++) AddPin(i);
            var q = FaceRotation(CaseData.Sectors[0].lat, CaseData.Sectors[0].lon + 25);
            globe.rotation = q; targetRot = q;
            Refresh();
        }

        // ---------- Globo ----------
        static Vector3 LatLon(float lat, float lon, float r = 1f)
        {
            float la = lat * Mathf.Deg2Rad, lo = lon * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(la) * Mathf.Sin(lo), Mathf.Sin(la), -Mathf.Cos(la) * Mathf.Cos(lo)) * r;
        }

        /// Rotación que deja (lat, lon) mirando a la cámara con el norte arriba.
        static Quaternion FaceRotation(float lat, float lon)
        {
            var dir = LatLon(lat, lon);
            var east = Vector3.Cross(Vector3.up, dir).normalized;
            var north = Vector3.Cross(dir, east).normalized;
            return Quaternion.LookRotation(Vector3.back, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(dir, north));
        }

        void BuildGlobe()
        {
            globe = new GameObject("Globo").transform;
            var earth = Sphere("Tierra", 1f, EarthMap.Build(), true);
            earth.SetParent(globe, false);
            var clouds = Sphere("Nubes", 1.012f, EarthMap.Clouds(), false);
            clouds.SetParent(globe, false);
            cloudMat = clouds.GetComponent<MeshRenderer>().material;
            var atm = Sphere("Atmósfera", 1.05f, null, false);
            atm.SetParent(globe, false);
            atm.GetComponent<MeshRenderer>().material = Mats.Glow(new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .035f));
        }

        Transform Sphere(string name, float r, Texture tex, bool opaque)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = UvSphere(r, 96, 192);
            go.GetComponent<MeshRenderer>().material = Mats.Glow(Color.white, false, false, false, tex, opaque);
            return go.transform;
        }

        static Mesh UvSphere(float r, int rows, int cols)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= cols; x++)
                {
                    float lat = -90 + 180f * y / rows, lon = -180 + 360f * x / cols;
                    v.Add(LatLon(lat, lon, r));
                    uv.Add(new Vector2((float)x / cols, (float)y / rows));
                }
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    int a = y * (cols + 1) + x, b = a + 1, c = a + cols + 1, d = c + 1;
                    t.AddRange(new[] { a, c, b, b, c, d });
                }
            var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }

        GameObject Quad(string name, Color c, float size, bool round = true, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            if (collider) go.AddComponent<SphereCollider>().radius = 2f; // más grande que el punto: fácil de acertar
            go.transform.SetParent(globe, false);
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<MeshRenderer>().material = Mats.Glow(c, true, false, round);
            return go;
        }

        void AddMarker(ShipDef s)
        {
            var m = new Marker { ship = s };
            m.go = Quad(s.id, Pal.Sonar, .0048f, true, true);
            m.halo = Quad(s.id + "_viirs", new Color(Pal.DeckLight.r, Pal.DeckLight.g, Pal.DeckLight.b, .6f), .014f);
            m.ring = Quad(s.id + "_sar", new Color(1, 1, 1, .85f), .0026f, false);
            m.ring.transform.SetParent(m.go.transform.parent);
            var lr = new GameObject(s.id + "_track").AddComponent<LineRenderer>();
            lr.transform.SetParent(globe, false);
            lr.useWorldSpace = false; lr.widthMultiplier = .00045f;
            lr.material = Mats.Glow(new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .55f));
            float endLat = s.ais ? s.lat : s.lastLat, endLon = s.ais ? s.lon : s.lastLon;
            lr.positionCount = 7;
            for (int i = 0; i < 7; i++)
            {
                float u = i / 6f;
                lr.SetPosition(i, LatLon(endLat - .9f * (1 - u) + .15f * Mathf.Sin(u * 6), endLon + 1.1f * (1 - u), 1.0015f));
            }
            m.track = lr;
            m.label = UI.Text(labels, s.name, 20, Pal.Text);
            m.label.rectTransform.anchorMin = m.label.rectTransform.anchorMax = Vector2.zero;
            m.label.rectTransform.sizeDelta = new Vector2(320, 30);
            m.label.rectTransform.pivot = new Vector2(0, .5f);
            m.go.AddComponent<MarkerTag>().ship = s;
            markers.Add(m);
        }

        void AddPin(int i)
        {
            var d = CaseData.Sectors[i];
            var go = Quad("sector" + i, i == 0 ? Pal.Sonar : Pal.Dim, .06f, true, true);
            go.transform.localPosition = LatLon(d.lat, d.lon, 1.02f);
            go.AddComponent<SectorTag>().sector = i;
            var label = UI.Text(labels, d.name + (i == 0 ? "" : "  [BLOQUEADO]"), 22, i == 0 ? Pal.Sonar : Pal.Dim);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(460, 30);
            label.rectTransform.pivot = new Vector2(0, .5f);
            pins.Add((go, i, label));
        }

        public class MarkerTag : MonoBehaviour { public ShipDef ship; }
        public class SectorTag : MonoBehaviour { public int sector; }

        // ---------- UI ----------
        void BuildUI()
        {
            canvas = UI.NewCanvas();
            labels = UI.Rect(canvas.transform, "Etiquetas", Vector2.zero, Vector2.one); // primero: quedan bajo los paneles
            var top = UI.Panel(canvas.transform, new Vector2(0, .94f), Vector2.one);
            topBar = UI.Text(top.transform, "", 22, Pal.Text, TextAlignmentOptions.Left);
            topBar.margin = new Vector4(20, 0, 20, 0);

            var obj = UI.Panel(canvas.transform, new Vector2(0, .72f), new Vector2(.3f, .94f));
            objective = UI.Text(obj.transform, "", 21);
            objective.margin = new Vector4(16, 12, 16, 12);

            var r = UI.Panel(canvas.transform, new Vector2(.68f, 0), new Vector2(1, .94f));
            rightPanel = UI.VStack(r.transform, 14, 6);
            UI.Line(rightPanel, "Capas de satélite", 26, Pal.Sonar, true);
            layerBtns = new Button[CaseData.Layers.Length];
            layerInfo = new TextMeshProUGUI[CaseData.Layers.Length];
            for (int i = 0; i < CaseData.Layers.Length; i++)
            {
                int k = i;
                layerBtns[i] = UI.Btn(rightPanel, CaseData.Layers[i].name, () => BuyLayer(k), null, 21, 42);
                layerInfo[i] = UI.Line(rightPanel, "", 17, Pal.Dim);
            }
            UI.Btn(rightPanel, "Volver a base", Game.GoToMenu, Pal.Dim, 19, 36);

            UI.Line(rightPanel, "Índice de Riesgo", 26, Pal.Sonar, true);
            irText = UI.Line(rightPanel, "Selecciona un buque en el mapa.", 22);
            irBar = UI.NewBar(rightPanel, Pal.DeckLight, 12);
            dossier = UI.Line(rightPanel, "", 18);
            deploy = UI.Btn(rightPanel, "Desplegar lancha: Fase 2", () =>
            {
                C.ir = selected != null ? IR(selected) : 0;
                UI.Summary(canvas.transform, $"{selected.name}: IR {C.ir}", Pal.Sealed, new[]
                {
                    "El índice de riesgo justifica acercarse. Sigues trabajando de noche, más allá de la milla 200.",
                }, "Qué sigue: te acercas en lancha y escaneas el casco para saber dónde están las personas.", "Salir en lancha", () => Game.GoToPhase(2));
            }, Pal.Sealed, 23, 54);
            toast = UI.Toasts.Create(canvas.transform, new Vector2(.05f, .04f), new Vector2(.65f, .14f));
        }

        int IR(ShipDef s) => CaseData.ComputeIR(s, C.revealed, C.hours, C.sarDegraded, Decay);
        int LayerCost(int i) => i == CaseData.LayerSAR && Game.Has(3, 3) ? 8 : CaseData.Layers[i].cost;

        string LayerBlock(int i) => LayerBlockAt(i, C.hours, C.abLeft);

        string LayerBlockAt(int i, float h, int ab)
        {
            if ((C.revealed & (1 << i)) != 0) return null;
            if (ab < LayerCost(i)) return "Sin ancho de banda hasta mañana";
            if (i == CaseData.LayerVIIRS && !CaseData.Night(h)) return "Solo de noche";
            if ((i == CaseData.LayerVIIRS || i == CaseData.LayerOptical) && CaseData.Cloudy(h)) return "Bloqueada por nubes";
            if (i == CaseData.LayerSAR && !CaseData.SarNow(h)) return "Satélite fuera de pasada";
            return null;
        }

        /// Horas hasta que se pueda comprar la capa. Así nadie calcula horas ni se pierde una pasada.
        float HoursUntilOpen(int i)
        {
            int today = Mathf.FloorToInt(C.hours / 24f);
            for (float t = 0; t <= 96f; t += .5f)
            {
                float h = C.hours + t;
                if (LayerBlockAt(i, h, Mathf.FloorToInt(h / 24f) > today ? 60 : C.abLeft) == null) return t;
            }
            return 0;
        }

        void BuyLayer(int i)
        {
            float wait = HoursUntilOpen(i);
            if (wait > 0) Advance(wait);
            if (LayerBlock(i) != null) return;
            C.abLeft -= LayerCost(i);
            C.revealed |= 1 << i;
            if (i == CaseData.LayerSAR && CaseData.DouglasAt(C.hours) >= 6) { C.sarDegraded = true; toast.Show("Mar gruesa: la pasada SAR pierde precisión.", Pal.DeckLight); }
            Sfx.Play(Sfx.PingHollow, .25f);
            Game.Write();
            Refresh();
        }

        void Advance(float hours)
        {
            int before = Mathf.FloorToInt(C.hours / 24f);
            C.hours += hours;
            int after = Mathf.FloorToInt(C.hours / 24f);
            if (after > before)
            {
                C.abLeft = 60;
                toast.Show($"Nuevo día: 60 AB. El buque se aleja y el rastro pierde {Decay:0} puntos de IR.", Pal.DeckLight);
            }
            Refresh();
        }

        void Refresh()
        {
            float h = C.hours;
            string clouds = CaseData.Cloudy(h) ? "Nublado" : "Despejado";
            string night = CaseData.Night(h) ? "Noche" : "Día";
            topBar.text = $"<b>FASE 1 · RASTREO</b>     {Game.FormatClock(h)}     Ancho de banda <color=#3FE0D0>{C.abLeft} AB</color>     {night}, {clouds}     Douglas {CaseData.DouglasAt(h)}     Próx. SAR: {(CaseData.SarNow(h) ? "AHORA" : (CaseData.NextSarPass(h) - h).ToString("0.#") + " h")}";

            for (int i = 0; i < layerBtns.Length; i++)
            {
                var d = CaseData.Layers[i];
                bool owned = (C.revealed & (1 << i)) != 0;
                string block = LayerBlock(i);
                float wait = owned ? 0 : HoursUntilOpen(i);
                layerBtns[i].interactable = !owned;
                UI.SetLabel(layerBtns[i], owned ? $"{d.name}: activa" : wait > 0 ? $"{d.name}: esperar {wait:0.#} h y usar ({LayerCost(i)} AB)" : $"{d.name}: {LayerCost(i)} AB");
                layerInfo[i].text = d.shows + (block != null ? $"  <color=#F2A73B>{block}</color>" : "");
            }

            var parts = new List<string>();
            int ir = selected != null ? IR(selected) : 0;
            if (selected == null)
            {
                irText.text = zoomed ? "Selecciona un buque en el mapa." : "Abre el Sector 1 en el globo.";
                dossier.text = "";
            }
            else
            {
                irText.text = $"<b>{selected.name}</b>: IR <color={(ir >= CaseData.IrToScan ? "#6CFF8A" : "#F2A73B")}>{ir}</color> / {CaseData.IrToScan} para escanear";
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"{selected.type}. {selected.flag}.");
                foreach (var a in selected.anomalies)
                {
                    if (CaseData.LayerVisible(selected, a, C.revealed, h))
                        sb.AppendLine($"<color=#F2A73B>+{a.weight * a.conf * selected.mRoute:0}</color> {a.name}: {a.detail}");
                }
                if (sb.Length == 0 || selected.anomalies.Length == 0) sb.AppendLine("Sin anomalías detectadas.");
                if ((C.revealed & (1 << CaseData.LayerOptical)) != 0) sb.AppendLine("<color=#3FE0D0>Óptica:</color> " + selected.optical);
                sb.Append($"<color=#55666E>IR = min(100, Σ w·c × M_ruta {selected.mRoute:0.0})</color>");
                dossier.text = sb.ToString();
            }
            irBar.Set(ir / 100f);
            deploy.interactable = selected != null && ir >= CaseData.IrToScan;

            // Objetivo guiado para el tutorial del Sector 1.
            string o;
            if (!zoomed) o = "<b>Lo que sabemos</b>\nUn potero de 90 m ha desaparecido del AIS frente a Argentina.\n\nHaz clic en el <color=#3FE0D0>Sector 1</color> del globo.";
            else if (selected == null) o = "<b>Busca al sospechoso</b>\nUn buque ha apagado su señal. Haz clic en <color=#3FE0D0>Hai Feng 77 (última señal)</color>, o investiga otro.";
            else if (ir < CaseData.IrToScan && !selected.target) o = $"<b>{selected.name}</b>\nNo parece nuestro objetivo. Prueba con Hai Feng 77.";
            else if (ir < CaseData.IrToScan) o = $"<b>Sube el IR a {CaseData.IrToScan}</b>\nUsa capas de satélite para confirmar sospechas. Prueba <color=#3FE0D0>{NextLayerTip()}</color>. Si hay que esperar, el botón lo hace por ti.";
            else o = "<b>Índice suficiente</b>\nPulsa <color=#6CFF8A>Desplegar lancha</color>.";
            objective.text = o;

            foreach (var m in markers) UpdateMarker(m);
            cloudMat.SetColor("_Color", new Color(1, 1, 1, CaseData.Cloudy(h) ? .85f : .18f));
        }

        string NextLayerTip()
        {
            foreach (var i in new[] { CaseData.LayerSAR, CaseData.LayerVMS, CaseData.LayerVIIRS, CaseData.LayerOptical })
                if ((C.revealed & (1 << i)) == 0) return CaseData.Layers[i].name;
            return "esperar";
        }

        void UpdateMarker(Marker m)
        {
            var s = m.ship;
            bool sar = (C.revealed & (1 << CaseData.LayerSAR)) != 0;
            bool viirs = (C.revealed & (1 << CaseData.LayerVIIRS)) != 0;
            bool visible = s.ais || s.target || sar;
            float lat = (s.ais || sar) ? s.lat : s.lastLat, lon = (s.ais || sar) ? s.lon : s.lastLon;
            var pos = LatLon(lat, lon, 1.002f);
            m.go.transform.localPosition = pos; m.halo.transform.localPosition = pos * 1.0002f; m.ring.transform.localPosition = pos * 1.0004f;
            bool show = zoomed && visible;
            m.go.SetActive(show);
            m.track.gameObject.SetActive(show && (s.ais || s.target));
            m.halo.SetActive(show && viirs && (s.type.StartsWith("Potero")));
            m.ring.SetActive(show && sar);
            int ir = IR(s);
            Color c = ir >= CaseData.IrToScan ? Pal.Danger : ir >= 25 ? Pal.DeckLight : Pal.Sonar;
            if (!s.ais && !sar) c = Pal.Dim;
            m.go.GetComponent<MeshRenderer>().material.SetColor("_Color", c);
            m.label.text = !s.ais && !sar ? s.name + " (última señal)" : s.name;
            m.label.color = c;
            m.label.fontStyle = selected == s ? FontStyles.Bold | FontStyles.Underline : FontStyles.Normal;
        }

        // ---------- Bucle ----------
        void Update()
        {
            dist = Mathf.Lerp(dist, targetDist, Time.deltaTime * 2.5f);
            cam.transform.position = new Vector3(0, 0, -dist);
            cam.transform.rotation = Quaternion.identity;
            cam.rect = new Rect(0, 0, .68f, .94f); // centra el globo en el hueco que dejan los paneles

            var mouse = Mouse.current;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse != null)
            {
                bool down = mouse.leftButton.isPressed, justDown = down && !wasDown;
                wasDown = down;
                if (!zoomed && !overUI && down && !justDown)
                {
                    var d = mouse.position.ReadValue() - dragLast;
                    targetRot = Quaternion.Euler(d.y * .15f, -d.x * .15f, 0) * targetRot;
                }
                dragLast = mouse.position.ReadValue();
                if (justDown && !overUI) Click(mouse.position.ReadValue());
            }
            globe.rotation = Quaternion.Slerp(globe.rotation, targetRot, Time.deltaTime * 4f);

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && zoomed) ZoomOut();
            UpdateLabels();
        }

        void Click(Vector2 screen)
        {
            if (!Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 10f)) return;
            var ms = hit.collider.GetComponent<MarkerTag>();
            if (ms != null) { selected = ms.ship; Sfx.Play(Sfx.Click, .5f); Refresh(); return; }
            var st = hit.collider.GetComponent<SectorTag>();
            if (st == null || zoomed) return;
            if (st.sector == 0) ZoomIn();
            else
            {
                var d = CaseData.Sectors[st.sector];
                toast.Show($"{d.name}: bloqueado. Pide {d.unlock}. Este build incluye solo el caso del Sector 1.", Pal.DeckLight, 5);
            }
        }

        void ZoomIn()
        {
            zoomed = true; targetDist = 1.13f;
            targetRot = FaceRotation(CaseData.Sectors[0].lat, CaseData.Sectors[0].lon);
            foreach (var p in pins) { p.go.SetActive(false); p.label.gameObject.SetActive(false); }
            Refresh();
        }

        void ZoomOut()
        {
            zoomed = false; selected = null; targetDist = 3.9f;
            foreach (var p in pins) { p.go.SetActive(true); p.label.gameObject.SetActive(true); }
            Refresh();
        }

        void UpdateLabels()
        {
            float scale = canvas.scaleFactor;
            foreach (var p in pins)
            {
                if (!p.label.gameObject.activeSelf) continue;
                p.go.transform.rotation = cam.transform.rotation;
                bool front = Vector3.Dot(p.go.transform.position, Vector3.back) > .2f;
                p.go.GetComponent<MeshRenderer>().enabled = front;
                p.label.enabled = front;
                p.label.rectTransform.anchoredPosition = (Vector2)cam.WorldToScreenPoint(p.go.transform.position) / scale + new Vector2(24, 0);
            }
            foreach (var m in markers)
            {
                bool on = m.go.activeSelf;
                m.label.enabled = on;
                if (!on) continue;
                m.go.transform.rotation = cam.transform.rotation;
                m.halo.transform.rotation = cam.transform.rotation;
                m.ring.transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, 45);
                m.label.rectTransform.anchoredPosition = (Vector2)cam.WorldToScreenPoint(m.go.transform.position) / scale + new Vector2(16, 0);
            }
        }
    }

    /// Mapa estilizado generado a partir de polígonos gruesos (lat, lon) de los continentes.
    public static class EarthMap
    {
        static readonly float[][] Land =
        {
            // Sudamérica
            new float[] { 12,-72, 11,-62, 6,-52, 0,-50, -5,-35, -13,-38, -23,-42, -25,-48, -33,-52, -38,-57, -42,-63, -47,-66, -52,-68, -55,-68, -53,-74, -45,-74, -37,-73, -28,-71, -18,-70, -14,-76, -5,-81, 1,-80, 7,-78 },
            // Norte y Centroamérica
            new float[] { 8,-78, 9,-83, 15,-92, 19,-105, 28,-112, 33,-118, 40,-124, 48,-125, 57,-135, 60,-147, 56,-160, 66,-165, 71,-157, 70,-130, 68,-110, 73,-90, 66,-85, 60,-94, 54,-82, 58,-77, 62,-72, 52,-56, 47,-60, 44,-66, 40,-74, 32,-80, 25,-80, 30,-90, 26,-97, 19,-96, 21,-87, 16,-88, 11,-84 },
            // Groenlandia
            new float[] { 60,-44, 70,-22, 81,-15, 83,-40, 78,-70, 70,-55 },
            // África
            new float[] { 36,-6, 37,10, 32,20, 31,32, 22,37, 12,44, 11,51, 2,46, -8,40, -16,40, -25,35, -34,26, -34,18, -22,14, -12,13, -5,12, 4,9, 5,-4, 5,-10, 10,-15, 15,-17, 21,-17, 28,-13 },
            // Madagascar
            new float[] { -13,49, -18,50, -25,47, -24,44, -17,44 },
            // Europa y Asia
            new float[] { 36,-9, 43,-9, 44,-1, 48,-4, 51,2, 54,8, 58,6, 62,5, 70,20, 71,30, 68,42, 73,70, 76,105, 72,130, 70,160, 65,178, 60,165, 56,162, 52,157, 58,140, 50,140, 42,132, 38,127, 34,127, 38,118, 30,122, 22,114, 18,106, 10,106, 9,100, 2,104, 8,98, 16,95, 22,90, 16,81, 8,77, 20,72, 25,62, 25,56, 17,55, 13,45, 28,34, 36,36, 37,27, 40,26, 36,23, 40,19, 44,13, 38,16, 42,10, 43,4, 38,0 },
            // Indonesia y Filipinas (masas simplificadas)
            new float[] { 5,95, 2,102, -5,105, -8,115, -8,125, -3,120, 1,119, 7,117, 2,109, -3,106, -5,100 },
            new float[] { -2,131, -3,141, -9,148, -8,138, -5,133 },
            new float[] { 18,121, 14,124, 8,126, 7,122, 13,120 },
            // Japón
            new float[] { 45,142, 40,142, 35,140, 33,131, 35,133, 39,139 },
            // Australia y Nueva Zelanda
            new float[] { -11,132, -11,142, -19,147, -28,154, -38,149, -38,140, -32,133, -34,120, -32,115, -22,113, -15,124 },
            new float[] { -35,173, -41,176, -46,168, -42,171 },
            // Gran Bretaña e Islandia
            new float[] { 58,-5, 53,0, 50,-5, 54,-5 },
            new float[] { 66,-22, 66,-14, 63,-18, 64,-22 },
            // Antártida
            new float[] { -68,-180, -66,-120, -70,-60, -64,-58, -72,-20, -68,30, -66,90, -67,140, -70,180, -90,180, -90,-180 },
        };

        public static Texture2D Build()
        {
            const int W = 2048, H = 1024;
            var px = new Color32[W * H];
            var ocean = (Color32)Color.Lerp(Pal.Ocean, Color.black, .25f);
            for (int i = 0; i < px.Length; i++) px[i] = ocean;
            var land = (Color32)new Color(.13f, .24f, .27f);
            var dot = (Color32)new Color(.22f, .55f, .55f);
            var mask = new bool[W * H];
            var crossings = new List<float>();
            foreach (var poly in Land)
                for (int y = 0; y < H; y++)
                {
                    float lat = -90 + 180f * (y + .5f) / H;
                    crossings.Clear();
                    int n = poly.Length / 2;
                    for (int i = 0; i < n; i++)
                    {
                        float la0 = poly[i * 2], lo0 = poly[i * 2 + 1], la1 = poly[(i + 1) % n * 2], lo1 = poly[(i + 1) % n * 2 + 1];
                        if ((la0 <= lat) != (la1 <= lat)) crossings.Add(lo0 + (lat - la0) / (la1 - la0) * (lo1 - lo0));
                    }
                    crossings.Sort();
                    for (int c = 0; c + 1 < crossings.Count; c += 2)
                    {
                        int x0 = Mathf.Clamp((int)((crossings[c] + 180) / 360 * W), 0, W - 1);
                        int x1 = Mathf.Clamp((int)((crossings[c + 1] + 180) / 360 * W), 0, W - 1);
                        for (int x = x0; x <= x1; x++) mask[y * W + x] = true;
                    }
                }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    if (mask[idx])
                    {
                        px[idx] = land;
                        // Sin puntos cerca de los polos: la proyección los convierte en rayas.
                        if (x % 6 < 2 && y % 6 < 2 && y > H / 6 && y < H - H / 10) px[idx] = dot;
                    }
                    else if (x > 0 && y > 0 && (mask[idx - 1] || mask[idx - W])) px[idx] = dot;
                    if (x % (W / 36) == 0 || y % (H / 18) == 0) px[idx] = Color32.Lerp(px[idx], new Color32(63, 224, 208, 255), .12f);
                }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp, anisoLevel = 4 };
            t.SetPixels32(px); t.Apply();
            return t;
        }

        public static Texture2D Clouds()
        {
            const int W = 512, H = 256;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float n = Mathf.PerlinNoise(x * .022f, y * .04f) * .6f + Mathf.PerlinNoise(x * .07f + 9, y * .12f + 3) * .4f;
                    byte a = (byte)(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.48f, .75f, n)) * 170);
                    px[y * W + x] = new Color32(190, 205, 215, a);
                }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp };
            t.SetPixels32(px); t.Apply();
            return t;
        }
    }
}
