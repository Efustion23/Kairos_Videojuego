using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kairos
{
    /// Menú principal y base de operaciones con estilo limpio tipo tarjeta/app moderna.
    public class MenuController : MonoBehaviour
    {
        public Camera cam;
        Canvas canvas;
        RectTransform root, tabBody, navBar;
        TextMeshProUGUI resources;
        float orbit;
        int activeTab, upgradeTab, optionTab, codexIndex;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            canvas = UI.NewCanvas();
            root = UI.Rect(canvas.transform, "Root", Vector2.zero, Vector2.one);
            Sfx.Loop(gameObject, Sfx.Wind, .25f);
            Music.Play("title");
            if (Game.HasSave && Game.S.warningAccepted && Game.C.phase > 1) ShowBase(); else ShowTitle();
            UI.SceneIntro(canvas.transform, "", "");
        }

        void Update()
        {
            if (cam == null) return;
            orbit += Time.deltaTime * .012f;
            cam.transform.position = new Vector3(Mathf.Sin(orbit + 2.2f) * 115, 9 + Ocean.Height(0, 0) * .3f, Mathf.Cos(orbit + 2.2f) * 115);
            cam.transform.LookAt(new Vector3(0, 11, 0));
            cam.lensShift = new Vector2(-.14f, 0);
        }

        void Screen() => UI.Clear(root);

        /// Tarjeta redondeada con altura automática según su contenido.
        RectTransform Card(Transform parent, Color? accent = null, float pad = 18, float space = 8, Color? bg = null)
        {
            var c = accent ?? Pal.Sonar;
            var p = UI.Panel(parent, Vector2.zero, Vector2.one, default, default, bg ?? new Color(.035f, .095f, .135f, .92f));
            var o = p.GetComponent<Outline>();
            if (o != null) o.effectColor = new Color(c.r, c.g, c.b, .40f);
            var filo = p.transform.Find("Filo")?.GetComponent<Image>();
            if (filo != null) filo.color = new Color(c.r, c.g, c.b, .85f);
            var v = p.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            v.spacing = space;
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return (RectTransform)p.transform;
        }

        // ---------- Título ----------
        void ShowTitle()
        {
            Screen();
            UI.SideShade(root, .62f);
            var col = UI.VStack(root, 64, 12, new Vector2(0, 0), new Vector2(.40f, 1));
            col.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            UI.Line(col, "KAIROS", 104, Pal.Sonar, true).characterSpacing = 20;
            UI.Line(col, "SOMBRAS EN ALTAMAR", 28, Pal.Text).characterSpacing = 5;
            UI.Line(col, "Investigación marítima, sigilo sin armas y justicia documental.", 19, Pal.Dim);
            UI.Line(col, " ", 18);
            if (Game.HasSave) UI.Btn(col, "Continuar partida", () => ShowBase(), Pal.Sealed, 24, 56);
            UI.Btn(col, "Nueva partida", () => { Game.NewGame(); ShowWarning(); }, Pal.Sonar, 24, 56);
            UI.Btn(col, "Cómo jugar", ShowTitleGuide, Pal.DeckLight, 22, 50);
            UI.Btn(col, "Salir", Application.Quit, Pal.Dim, 20, 46);
            UI.Line(col, " ", 12);
            UI.Line(col, "Alexander Benites · Diego Tornero · Christopher Veliz", 14, Pal.Dim);
        }

        void ShowTitleGuide()
        {
            Screen();
            UI.Fullscreen(root, new Color(.01f, .03f, .05f, .86f));
            var p = UI.Panel(root, new Vector2(.06f, .06f), new Vector2(.94f, .94f), default, default, new Color(.025f, .07f, .10f, .96f));

            var header = UI.Rect(p.transform, "Header", new Vector2(0, .90f), Vector2.one, new Vector2(24, 6), new Vector2(-24, -10));
            var hl = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = false;
            var title = UI.Line(header, "CÓMO JUGAR A KAIROS", 28, Pal.Sonar, true);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var back = UI.Btn(header, "Volver al título", ShowTitle, Pal.Dim, 20, 44);
            back.gameObject.AddComponent<LayoutElement>().minWidth = 200;

            tabBody = UI.Rect(p.transform, "GuideBody", new Vector2(0, 0), new Vector2(1, .89f), new Vector2(16, 12), new Vector2(-16, -4));
            TabGuide();
        }

        void ShowWarning()
        {
            Screen();
            UI.Fullscreen(root, new Color(.01f, .03f, .05f, .76f));
            var p = UI.Panel(root, new Vector2(.24f, .20f), new Vector2(.76f, .80f));
            var s = UI.VStack(p.transform, 34, 14);
            s.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            UI.Line(s, "Aviso de contenido", 34, Pal.DeckLight, true);
            UI.Line(s, "• Trata sobre pesca ilegal y trabajo forzado en altamar.\n• Sin violencia gráfica: todo se investiga con pruebas y testimonios.\n• Buques, empresas y personajes son ficticios.", 21);
            var toggle = UI.Btn(s, "", null, Pal.Sonar, 21, 50);
            void Label() => UI.SetLabel(toggle, "Omitir descripciones sensibles: " + (Game.S.omitSensitive ? "SÍ" : "NO"));
            toggle.onClick.AddListener(() => { Game.S.omitSensitive = !Game.S.omitSensitive; Label(); });
            Label();
            UI.Btn(s, "Entendido, continuar", () => { Game.S.warningAccepted = true; ShowCharacter(); }, Pal.Sealed, 23, 54);
        }

        static readonly string[] Genders = { "No especificar", "Mujer", "Hombre", "No binario" };
        static readonly string[] Origins = { "Perú", "Argentina", "Filipinas", "Senegal", "Indonesia", "España", "México", "Noruega" };

        void ShowCharacter()
        {
            Screen();
            UI.Fullscreen(root, new Color(.01f, .03f, .05f, .76f));
            var p = UI.Panel(root, new Vector2(.30f, .18f), new Vector2(.70f, .82f));
            var s = UI.VStack(p.transform, 32, 12);
            s.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            UI.Line(s, "Tu inspector o inspectora", 32, Pal.Sonar, true);
            UI.Line(s, "Nombre", 19, Pal.Dim);
            UI.Input(s, Game.S.playerName, "Nombre", v => Game.S.playerName = string.IsNullOrWhiteSpace(v) ? "Inspector" : v);
            UI.Line(s, "Género", 19, Pal.Dim);
            CycleButton(s, Genders, () => Game.S.gender, v => Game.S.gender = v);
            UI.Line(s, "Origen", 19, Pal.Dim);
            CycleButton(s, Origins, () => Game.S.origin, v => Game.S.origin = v);
            UI.Line(s, "Equipo: visor acústico-térmico, cámara forense y herramientas. Sin armas.", 18, Pal.Dim);
            UI.Btn(s, "Entrar a la base KAIROS", () => { Game.Write(); ShowBase(); }, Pal.Sealed, 23, 54);
        }

        void CycleButton(Transform parent, string[] options, System.Func<string> get, System.Action<string> set)
        {
            var b = UI.Btn(parent, get(), null);
            b.onClick.AddListener(() =>
            {
                int i = System.Array.IndexOf(options, get());
                set(options[(i + 1) % options.Length]);
                UI.SetLabel(b, get());
            });
        }

        // ---------- Base (Navegación superior limpia tipo app / videojuego) ----------
        void ShowBase(int initialTab = 0)
        {
            activeTab = initialTab;
            Screen();
            UI.Fullscreen(root, new Color(.01f, .03f, .05f, .54f));

            // Barra de estado superior
            var top = UI.Panel(root, new Vector2(.03f, .915f), new Vector2(.97f, .982f), default, default, new Color(.025f, .07f, .10f, .94f));
            var th = UI.Rect(top.transform, "TopRow", Vector2.zero, Vector2.one, new Vector2(20, 4), new Vector2(-20, -4));
            var hl = th.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = false;
            var brand = UI.Line(th, $"<b>BASE KAIROS</b>   <color=#8DA2AB>·  {Game.Inspector}</color>", 20, Pal.Sonar, false, TextAlignmentOptions.MidlineLeft);
            brand.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            resources = UI.Line(th, "", 19, Pal.Text, false, TextAlignmentOptions.MidlineRight);
            resources.gameObject.AddComponent<LayoutElement>().minWidth = 680;
            RefreshResources();

            // Barra de 4 pestañas principales (imposible perderse)
            navBar = UI.Rect(root, "NavBar", new Vector2(.03f, .835f), new Vector2(.97f, .902f));
            var nh = navBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            nh.spacing = 10; nh.childControlHeight = true; nh.childControlWidth = true; nh.childForceExpandWidth = true;

            var body = UI.Panel(root, new Vector2(.03f, .025f), new Vector2(.97f, .820f), default, default, new Color(.025f, .07f, .10f, .88f));
            tabBody = (RectTransform)body.transform;

            OpenTab(activeTab);
        }

        void RebuildNav()
        {
            UI.Clear(navBar);
            string[] names = { "MISIÓN ACTUAL", "CÓMO JUGAR", "MEJORAS Y TALLER", "OPCIONES Y CÓDICE" };
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                bool sel = i == activeTab;
                UI.Btn(navBar, names[i], () => OpenTab(idx), sel ? Pal.Sealed : Pal.Sonar, 20, 50);
            }
        }

        void OpenTab(int tab)
        {
            activeTab = tab;
            RebuildNav();
            switch (tab)
            {
                case 0: TabMission(); break;
                case 1: TabGuide(); break;
                case 2: TabUpgrades(upgradeTab); break;
                case 3: TabOptions(optionTab); break;
            }
        }

        void RefreshResources()
        {
            var s = Game.S;
            resources.text = $"Presupuesto <color=#3FE0D0><b>{s.budget:N0} ¢</b></color>   |   Combustible <color=#3FE0D0><b>{s.fuel:N0} L</b></color>   |   Drones <color=#3FE0D0><b>{s.droneParts}</b></color>   |   Rep <color=#3FE0D0><b>{s.reputation}/100</b></color>";
        }

        static readonly string[] PhaseNames = { "", "Fase 1: Rastreo satelital", "Fase 2: Escaneo con sonar", "Fase 3: Infiltración", "Fase 4: Juicio financiero", "Repatriación", "Caso cerrado" };
        static readonly string[] PhaseTips =
        {
            "",
            "Objetivo ahora: abre el Sector 1 en el globo, analiza capas satelitales y alcanza IR ≥ 60.",
            "Objetivo ahora: acércate en lancha, lanza pulsos en la barra verde y marca dónde hay personas (≥ 70 %).",
            "Objetivo ahora: aborda en el valle verde, recoge al menos 1 prueba A y rescata a la tripulación.",
            "Objetivo ahora: conecta las pruebas en el grafo hasta Tidewater Austral y ejecuta la sentencia.",
            "Objetivo ahora: asigna recursos de repatriación digna para cerrar el caso.",
            "Caso cerrado. Puedes repetirlo conservando tus mejoras y recursos."
        };

        // ---------- 1. MISIÓN ACTUAL ----------
        void TabMission()
        {
            UI.Clear(tabBody);
            var s = UI.Scroll(tabBody, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
            var c = Game.C;

            var hero = Card(s, Pal.Sonar, 24, 14);
            UI.Line(hero, $"{CaseData.Sectors[c.sector].name.ToUpperInvariant()}", 16, Pal.Dim, true);
            UI.Line(hero, CaseData.TargetFor(c.sector).name, 38, Pal.Sonar, true);
            UI.Line(hero, CaseData.CaseSummaryFor(c.sector), 20, Pal.Text);

            // 4 pasos visuales
            var steps = UI.HStack(hero, 78, 12);
            string[] stepTitles = { "1. RASTREO", "2. SONAR", "3. INFILTRACIÓN", "4. JUICIO" };
            string[] stepGoals = { "IR ≥ 60", "Confianza ≥ 70 %", "≥ 1 prueba A", "Sentencia" };
            for (int i = 0; i < 4; i++)
            {
                bool done = c.phase > i + 1;
                bool now = c.phase == i + 1;
                Color col = done ? Pal.Sealed : now ? Pal.Sonar : Pal.Dim;
                string badge = done ? "LISTO" : now ? "AHORA" : "SIGUIENTE";
                var sc = UI.Panel(steps, Vector2.zero, Vector2.one, default, default, new Color(col.r * .13f, col.g * .13f, col.b * .13f, .92f));
                var o = sc.GetComponent<Outline>();
                if (o != null) o.effectColor = new Color(col.r, col.g, col.b, now ? .85f : .45f);
                var txt = UI.Text(sc.transform, $"<b>{stepTitles[i]}</b>\n<size=17>{stepGoals[i]}</size>\n<size=14>[{badge}]</size>", 19, col, TextAlignmentOptions.Center);
                txt.margin = new Vector4(6, 6, 6, 6);
            }

            UI.Line(hero, PhaseTips[Mathf.Clamp(c.phase, 1, 6)], 20, Pal.DeckLight);
            if (c.phase >= 6)
                UI.Line(hero, $"Sentencia: {CaseData.VerdictNames[c.verdict]}  ·  Embargo: {c.embargo:N0} ¢", 21, Pal.Sealed, true);

            var actions = UI.HStack(hero, 58, 14);
            if (c.phase >= 6)
                UI.Btn(actions, "REPETIR CASO (conservas mejoras)", () => { Game.StartCase(c.sector); Game.GoToPhase(1); }, Pal.Sonar, 22, 58);
            else
                UI.Btn(actions, "JUGAR · " + PhaseNames[c.phase].ToUpperInvariant(), () => Game.GoToPhase(c.phase), Pal.Sealed, 23, 58);
            UI.Btn(actions, "Ver guía y controles", () => OpenTab(1), Pal.DeckLight, 20, 58);

            // Sectores en fila horizontal: todos seleccionables para jugar la campaña completa
            UI.Line(s, "SECTORES DE CAMPAÑA (ELIGE UN SECTOR)", 20, Pal.Sonar, true);
            var secRow = UI.HStack(s, 118, 12);
            for (int i = 0; i < CaseData.Sectors.Length; i++)
            {
                int sec = i;
                var d = CaseData.Sectors[i];
                bool current = c.sector == i;
                bool done = Game.S.completedSectors != null && i < Game.S.completedSectors.Length && Game.S.completedSectors[i];
                Color col = current ? Pal.Sealed : done ? Pal.DeckLight : Pal.Sonar;
                string badge = current ? "<color=#6CFF8A>ACTIVO</color>" : done ? "<color=#FFD27A>COMPLETADO</color>" : "<color=#3FE0D0>DISPONIBLE</color>";
                var sp = Card(secRow, col, 12, 6, new Color(col.r * .10f, col.g * .10f, col.b * .10f, .90f));
                UI.Line(sp, $"<b>{d.name}</b>\n<size=16>{d.ship} · {badge}</size>", 17, Pal.Text, false, TextAlignmentOptions.Center);
                UI.Btn(sp, current ? "En curso" : "Seleccionar sector", () => { if (!current) { Game.StartCase(sec); Game.Write(); TabMission(); } }, col, 17, 36).interactable = !current;
            }
        }

        // ---------- 2. CÓMO JUGAR (Visual, 4 tarjetas + Controles) ----------
        void TabGuide()
        {
            UI.Clear(tabBody);
            var s = UI.Scroll(tabBody, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));

            var intro = Card(s, Pal.Sonar, 16, 6);
            UI.Line(intro, "KAIROS es una investigación sin armas: ganas documentando pruebas y rescatando a la tripulación.", 21, Pal.Text, true);

            void PairRow(string n1, string t1, string g1, string b1, Color c1, string n2, string t2, string g2, string b2, Color c2)
            {
                var row = UI.HStack(s, 175, 14);
                void One(string num, string title, string goal, string body, Color col)
                {
                    var p = UI.Panel(row, Vector2.zero, Vector2.one, default, default, new Color(.035f, .095f, .135f, .92f));
                    var o = p.GetComponent<Outline>();
                    if (o != null) o.effectColor = new Color(col.r, col.g, col.b, .5f);
                    var filo = p.transform.Find("Filo")?.GetComponent<Image>();
                    if (filo != null) filo.color = new Color(col.r, col.g, col.b, .9f);
                    var v = UI.VStack(p.transform, 16, 6);
                    UI.Line(v, $"{num} · {title}", 22, col, true);
                    UI.Line(v, goal, 18, Pal.DeckLight, true);
                    UI.Line(v, body, 18, Pal.Text);
                }
                One(n1, t1, g1, b1, c1);
                One(n2, t2, g2, b2, c2);
            }

            PairRow(
                "01", "Rastreo Satelital", "Meta: IR ≥ 60",
                "• Clic en Sector 1 y luego en Hai Feng 77.\n• Compra capas (VMS, VIIRS, SAR, Óptica) con tus 60 AB/día.\n• Al llegar a 60 de IR, despliega la lancha.",
                Pal.Sonar,
                "02", "Escaneo con Sonar", "Meta: Confianza ≥ 70 %",
                "• Acércate a <350 m y pulsa Espacio en la barra verde.\n• Usa H (hidrófono) y T (dron térmico).\n• Marca «Aquí hay personas» en el compartimento hueco.",
                Pal.Sonar);

            PairRow(
                "03", "Infiltración Sigilosa", "Meta: ≥ 1 prueba A y rescate",
                "• Aborda con E cuando la barra esté en verde.\n• Evita focos ámbar y conos de guardias; usa F (visor).\n• Mantén E para cortar/fotografiar y habla con respeto.",
                Pal.Sealed,
                "04", "Juicio y Rescate", "Meta: Sentencia",
                "• Conecta pruebas en el grafo hasta Tidewater Austral.\n• Indicadores OIT: 4+ Retención · 7+ Congelación · 11 Condena.\n• Usa el 15 % del embargo para repatriar a la tripulación.",
                Pal.DeckLight);

            var ctrl = Card(s, Pal.DeckLight, 16, 6);
            UI.Line(ctrl, "CONTROLES RÁPIDOS", 21, Pal.DeckLight, true);
            UI.Line(ctrl,
                "<b>Fase 2 (Lancha):</b>  W/S avanzar  ·  A/D girar  ·  Espacio pulso sonar  ·  H hidrófono  ·  T dron  ·  Clic der. cámara\n" +
                "<b>Fase 3 (A bordo):</b>  WASD mover  ·  Shift correr  ·  E usar/cortar/hablar  ·  F visor  ·  Q herramienta  ·  Tab maletín  ·  Esc pausa",
                18, Pal.Text);
        }

        // ---------- 3. MEJORAS Y TALLER ----------
        void TabUpgrades(int tab)
        {
            upgradeTab = Mathf.Clamp(tab, 0, 4);
            UI.Clear(tabBody);

            var bar = UI.Rect(tabBody, "UpTabs", new Vector2(0, .89f), new Vector2(1, 1), new Vector2(18, 6), new Vector2(-18, -10));
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = true;
            for (int b = 0; b < 4; b++)
            {
                int idx = b;
                UI.Btn(bar, CaseData.Branches[b], () => TabUpgrades(idx), b == upgradeTab ? Pal.Sealed : Pal.Sonar, 18, 44);
            }
            UI.Btn(bar, "Taller (Suministros)", () => TabUpgrades(4), upgradeTab == 4 ? Pal.Sealed : Pal.DeckLight, 18, 44);

            var list = UI.Scroll(tabBody, new Vector2(0, 0), new Vector2(1, .88f), new Vector2(10, 10), new Vector2(-10, -4));

            if (upgradeTab == 4)
            {
                var c = Card(list, Pal.DeckLight, 22, 12);
                UI.Line(c, "Taller y suministros", 28, Pal.DeckLight, true);
                var fuel = UI.Btn(c, "Comprar 1.000 L de combustible  —  3.000 ¢", () => { Game.AddBudget(-3000); Game.S.fuel += 1000; Game.Write(); RefreshResources(); TabUpgrades(4); }, Pal.Sonar, 20, 52);
                fuel.interactable = Game.S.budget >= 3000;
                var part = UI.Btn(c, "Comprar repuesto de dron  —  8.000 ¢", () => { Game.AddBudget(-8000); Game.S.droneParts++; Game.Write(); RefreshResources(); TabUpgrades(4); }, Pal.Sonar, 20, 52);
                part.interactable = Game.S.budget >= 8000;
                UI.Line(c, "• Sin combustible te remolcan (-24 h y -10.000 ¢). Sin drones, la Fase 2 sigue con sonar e hidrófono.", 19, Pal.Dim);
                return;
            }

            UI.Line(list, "Costes: 40.000 · 90.000 · 180.000 · 350.000 ¢   |   Reputación mínima: 0 · 30 · 55 · 80", 18, Pal.Dim);
            for (int l = 0; l < 4; l++)
            {
                int level = l;
                bool owned = Game.S.upgrades[upgradeTab] > level;
                bool next = Game.S.upgrades[upgradeTab] == level;
                int cost = CaseData.UpgradeCost[level], rep = CaseData.UpgradeRep[level];
                bool can = next && Game.S.budget >= cost && Game.S.reputation >= rep;
                Color accent = owned ? Pal.Sealed : can ? Pal.Sonar : Pal.Dim;
                string state = owned ? "INSTALADA" : next ? $"{cost:N0} ¢ · Rep {rep}" : $"Requiere nivel {level}";

                var c = Card(list, accent, 16, 6);
                UI.Line(c, $"<b>Nivel {level + 1} · {CaseData.Upgrades[upgradeTab, level]}</b>   <color={(owned ? "#6CFF8A" : can ? "#3FE0D0" : "#8DA2AB")}>[{state}]</color>", 20, Pal.Text);
                UI.Line(c, CaseData.UpgradeEffects[upgradeTab, level], 18, Pal.Dim);
                if (next)
                {
                    var btn = UI.Btn(c, can ? $"Instalar ({cost:N0} ¢)" : $"Bloqueada ({cost:N0} ¢ · Rep {rep})", () =>
                    {
                        Game.AddBudget(-cost);
                        Game.S.upgrades[upgradeTab] = level + 1;
                        Game.Write();
                        RefreshResources();
                        TabUpgrades(upgradeTab);
                    }, can ? Pal.Sealed : Pal.Dim, 19, 42);
                    btn.interactable = can;
                }
            }
        }

        // ---------- 4. OPCIONES Y CÓDICE ----------
        void TabOptions(int tab)
        {
            optionTab = Mathf.Clamp(tab, 0, 2);
            UI.Clear(tabBody);

            var bar = UI.Rect(tabBody, "OptTabs", new Vector2(0, .89f), new Vector2(1, 1), new Vector2(18, 6), new Vector2(-18, -10));
            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10; hl.childControlHeight = true; hl.childControlWidth = true; hl.childForceExpandWidth = true;
            UI.Btn(bar, "Ajustes y ayuda", () => TabOptions(0), optionTab == 0 ? Pal.Sealed : Pal.Sonar, 19, 44);
            UI.Btn(bar, "Códice documental", () => TabOptions(1), optionTab == 1 ? Pal.Sealed : Pal.Sonar, 19, 44);
            UI.Btn(bar, "Créditos", () => TabOptions(2), optionTab == 2 ? Pal.Sealed : Pal.Sonar, 19, 44);
            UI.Btn(bar, "Volver al título", ShowTitle, Pal.Dim, 19, 44);

            var s = UI.Scroll(tabBody, new Vector2(0, 0), new Vector2(1, .88f), new Vector2(10, 10), new Vector2(-10, -4));

            if (optionTab == 0)
            {
                var c = Card(s, Pal.Sonar, 20, 10);
                var toggle = UI.Btn(c, "", null, null, 20, 48);
                void Label() => UI.SetLabel(toggle, "Omitir descripciones sensibles: " + (Game.S.omitSensitive ? "SÍ" : "NO"));
                toggle.onClick.AddListener(() => { Game.S.omitSensitive = !Game.S.omitSensitive; Game.Write(); Label(); });
                Label();

                var sens = UI.Btn(c, "", null, null, 20, 48);
                void SensLabel() => UI.SetLabel(sens, $"Sensibilidad del ratón: {Mathf.RoundToInt(Game.S.mouseSens * 100)} %");
                sens.onClick.AddListener(() => { Game.S.mouseSens = Game.S.mouseSens >= 2f ? .5f : Game.S.mouseSens + .25f; Game.Write(); SensLabel(); });
                SensLabel();

                var vol = UI.Btn(c, "", null, null, 20, 48);
                void VolLabel() => UI.SetLabel(vol, $"Volumen general: {Mathf.RoundToInt(Game.S.masterVolume * 100)} %");
                vol.onClick.AddListener(() =>
                {
                    Game.S.masterVolume = Game.S.masterVolume <= .25f ? 1f : Game.S.masterVolume - .25f;
                    AudioListener.volume = Game.S.masterVolume;
                    Game.Write(); VolLabel();
                });
                VolLabel();

                var music = UI.Btn(c, "", null, null, 20, 48);
                void MusicLabel() => UI.SetLabel(music, $"Música: {Mathf.RoundToInt(Game.S.musicVolume * 100)} %");
                music.onClick.AddListener(() =>
                {
                    Game.S.musicVolume = Game.S.musicVolume <= .01f ? 1f : Mathf.Max(0, Game.S.musicVolume - .2f);
                    Game.Write(); MusicLabel();
                });
                MusicLabel();

                var full = UI.Btn(c, "", null, null, 20, 48);
                void FullLabel() => UI.SetLabel(full, "Pantalla completa: " + (UnityEngine.Screen.fullScreen ? "SÍ" : "NO"));
                full.onClick.AddListener(() => { UnityEngine.Screen.fullScreen = !UnityEngine.Screen.fullScreen; FullLabel(); });
                FullLabel();

                var help = Card(s, Pal.DeckLight, 18, 8);
                UI.Line(help, "Si este tema te afecta", 22, Pal.DeckLight, true);
                UI.Btn(help, "Organización Internacional del Trabajo (ilo.org)", () => Application.OpenURL("https://www.ilo.org"), Pal.Sonar, 19, 44);
                UI.Btn(help, "Organización Internacional para las Migraciones (iom.int)", () => Application.OpenURL("https://www.iom.int"), Pal.Sonar, 19, 44);
                UI.Btn(help, "Borrar partida y empezar de cero", () => { Game.NewGame(); Game.Write(); ShowWarning(); }, Pal.Danger, 19, 44);
            }
            else if (optionTab == 1)
            {
                var row1 = UI.HStack(s, 44, 8);
                var row2 = UI.HStack(s, 44, 8);
                for (int i = 0; i < CaseData.Codex.Length; i++)
                {
                    int idx = i;
                    UI.Btn(i < 3 ? row1 : row2, CaseData.Codex[i].title, () => { codexIndex = idx; TabOptions(1); }, i == codexIndex ? Pal.Sealed : Pal.Sonar, 17, 44);
                }
                var e = CaseData.Codex[codexIndex];
                var card = Card(s, Pal.DeckLight, 22, 10);
                UI.Line(card, e.title, 26, Pal.DeckLight, true);
                UI.Line(card, e.body, 20, Pal.Text);
                UI.Line(card, $"Fuente: {e.source} · {CaseData.CodexReviewed}", 18, Pal.Dim);
                UI.Line(card, "Qué puedes hacer: " + e.action, 19, Pal.Sealed);
            }
            else
            {
                var head = Card(s, Pal.Sonar, 18, 6);
                UI.Line(head, "KAIROS: Sombras en Altamar", 26, Pal.Sonar, true);
                UI.Line(head, "Alexander Benites · Diego Tornero · Christopher Veliz", 20, Pal.Text);
                foreach (var (group, license, lines) in Credits.Groups)
                {
                    var c = Card(s, Pal.Sonar, 14, 4);
                    UI.Line(c, group, 20, Pal.DeckLight, true);
                    UI.Line(c, license, 17, Pal.Dim);
                    UI.Line(c, string.Join("\n", lines), 18);
                }
            }
        }
    }
}
