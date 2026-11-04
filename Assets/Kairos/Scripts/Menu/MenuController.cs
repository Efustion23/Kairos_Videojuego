using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Kairos
{
    /// Título, aviso de contenido, personaje y base de operaciones (mejoras, taller, códice, ajustes).
    public class MenuController : MonoBehaviour
    {
        public Camera cam;
        Canvas canvas;
        RectTransform root;
        float orbit;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            canvas = UI.NewCanvas();
            root = UI.Rect(canvas.transform, "Root", Vector2.zero, Vector2.one);
            gameObject.AddComponent<AudioSource>();
            Sfx.Loop(gameObject, Sfx.Wind, .25f);
            Sfx.Loop(gameObject, Sfx.Drone, .22f);
            if (Game.HasSave && Game.S.warningAccepted && Game.C.phase > 1) ShowBase(); else ShowTitle();
        }

        void Update()
        {
            if (cam == null) return;
            orbit += Time.deltaTime * .012f;
            cam.transform.position = new Vector3(Mathf.Sin(orbit + 2.2f) * 150, 12 + Ocean.Height(0, 0) * .3f, Mathf.Cos(orbit + 2.2f) * 150);
            cam.transform.LookAt(new Vector3(0, 9, 0));
        }

        void Screen() => UI.Clear(root);

        void ShowTitle()
        {
            Screen();
            var col = UI.VStack(root, 60, 10, new Vector2(0, 0), new Vector2(.42f, 1));
            col.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            UI.Line(col, "KAIROS", 110, Pal.Sonar, true);
            UI.Line(col, "Sombras en Altamar", 38, Pal.Text);
            UI.Line(col, "Ningún océano es tan vasto ni ninguna noche tan oscura como para ocultar la dignidad humana.", 22, Pal.Dim);
            UI.Line(col, " ", 20);
            if (Game.HasSave) UI.Btn(col, "Continuar", ShowBase, Pal.Sealed, 26, 56);
            UI.Btn(col, "Nueva partida", () => { Game.NewGame(); ShowWarning(); }, Pal.Sonar, 26, 56);
            UI.Btn(col, "Salir", Application.Quit, Pal.Dim, 22, 46);
        }

        void ShowWarning()
        {
            Screen();
            var p = UI.Panel(root, new Vector2(.24f, .18f), new Vector2(.76f, .82f));
            var s = UI.VStack(p.transform, 34, 14);
            UI.Line(s, "Aviso de contenido", 38, Pal.DeckLight, true);
            UI.Line(s, "Este juego trata sobre trata de personas y trabajo forzado en la pesca. El abuso se entiende por huellas y testimonios: nunca se juega ni se muestra en primer plano. No hay violencia gráfica.", 24);
            UI.Line(s, "Puedes omitir las descripciones sensibles ahora o más adelante en Ajustes. Allí también encontrarás enlaces de ayuda.", 24, Pal.Dim);
            UI.Line(s, "Todos los buques y empresas del juego son ficticios.", 24, Pal.Dim);
            var toggle = UI.Btn(s, "", null, Pal.Sonar);
            void Label() => UI.SetLabel(toggle, "Omitir descripciones sensibles: " + (Game.S.omitSensitive ? "SÍ" : "NO"));
            toggle.onClick.AddListener(() => { Game.S.omitSensitive = !Game.S.omitSensitive; Label(); });
            Label();
            UI.Btn(s, "Entendido, continuar", () => { Game.S.warningAccepted = true; ShowCharacter(); }, Pal.Sealed, 24, 54);
        }

        static readonly string[] Genders = { "No especificar", "Mujer", "Hombre", "No binario" };
        static readonly string[] Origins = { "Perú", "Argentina", "Filipinas", "Senegal", "Indonesia", "España", "México", "Noruega" };

        void ShowCharacter()
        {
            Screen();
            var p = UI.Panel(root, new Vector2(.3f, .2f), new Vector2(.7f, .8f));
            var s = UI.VStack(p.transform, 34, 12);
            UI.Line(s, "Tu inspector o inspectora", 34, Pal.Sonar, true);
            UI.Line(s, "Nombre", 20, Pal.Dim);
            UI.Input(s, Game.S.playerName, "Nombre", v => Game.S.playerName = string.IsNullOrWhiteSpace(v) ? "Inspector" : v);
            UI.Line(s, "Género", 20, Pal.Dim);
            CycleButton(s, Genders, () => Game.S.gender, v => Game.S.gender = v);
            UI.Line(s, "Origen", 20, Pal.Dim);
            CycleButton(s, Origins, () => Game.S.origin, v => Game.S.origin = v);
            UI.Line(s, "Traje táctico, visor acústico-térmico, cámara forense y herramientas de precisión. Nunca armas letales.", 20, Pal.Dim);
            UI.Btn(s, "Entrar a la base KAIROS", () => { Game.Write(); ShowBase(); }, Pal.Sealed, 24, 54);
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

        // ---------- Base ----------
        RectTransform tabBody;
        TextMeshProUGUI resources;

        void ShowBase()
        {
            Screen();
            var top = UI.Panel(root, new Vector2(0, .93f), Vector2.one);
            resources = UI.Text(top.transform, "", 22, Pal.Text, TextAlignmentOptions.Left);
            resources.margin = new Vector4(24, 0, 24, 0);
            RefreshResources();

            var tabs = UI.Panel(root, new Vector2(0, 0), new Vector2(.18f, .93f));
            var ts = UI.VStack(tabs.transform, 14, 10);
            UI.Line(ts, "BASE KAIROS", 28, Pal.Sonar, true);
            UI.Line(ts, Game.S.playerName, 20, Pal.Dim);
            UI.Btn(ts, "Caso activo", TabCase, Pal.Sealed, 22, 54);
            UI.Btn(ts, "Mejoras", TabUpgrades, null, 22, 54);
            UI.Btn(ts, "Taller y suministros", TabWorkshop, null, 22, 54);
            UI.Btn(ts, "Códice", TabCodex, null, 22, 54);
            UI.Btn(ts, "Ajustes y ayuda", TabSettings, null, 22, 54);
            UI.Btn(ts, "Volver al título", ShowTitle, Pal.Dim, 20, 46);

            var body = UI.Panel(root, new Vector2(.18f, 0), new Vector2(1, .93f), default, default, new Color(.03f, .08f, .11f, .78f));
            tabBody = (RectTransform)body.transform;
            TabCase();
        }

        void RefreshResources()
        {
            var s = Game.S;
            resources.text = $"Presupuesto <color=#3FE0D0>{s.budget:N0} ¢</color>     Combustible <color=#3FE0D0>{s.fuel:N0} L</color>     Repuestos de dron <color=#3FE0D0>{s.droneParts}</color>     Ancho de banda <color=#3FE0D0>60 AB/día</color>     Reputación <color=#3FE0D0>{s.reputation}/100</color>     Sentencias <color=#3FE0D0>{s.sentences}</color>";
        }

        static readonly string[] PhaseNames = { "", "Fase 1: Rastreo satelital", "Fase 2: Escaneo con sonar", "Fase 3: Infiltración", "Fase 4: Juicio financiero", "Repatriación", "Caso cerrado" };

        void TabCase()
        {
            UI.Clear(tabBody);
            var s = UI.Scroll(tabBody, Vector2.zero, Vector2.one);
            var c = Game.C;
            UI.Line(s, "Caso 1: Hai Feng 77", 38, Pal.Sonar, true);
            UI.Line(s, "2031, de noche y más allá de la milla 200, donde terminan las patrullas costeras. Un potero de unos 90 m desaparece del AIS frente a Argentina. Detrás de él hay una red que rota nombres y banderas cada 9 a 14 meses.", 23);
            UI.Line(s, "Estado: <b>" + PhaseNames[Mathf.Clamp(c.phase, 1, 6)] + "</b>", 24, Pal.DeckLight);

            string[] gates = { "1. Rastrear → IR ≥ 60", "2. Escanear → Confianza ≥ 70 %", "3. Infiltrarse → Al menos 1 prueba A", "4. Rescatar y juzgar → Sentencia ejecutada" };
            for (int i = 0; i < 4; i++)
                UI.Line(s, gates[i], 22, c.phase > i + 1 ? Pal.Sealed : c.phase == i + 1 ? Pal.Text : Pal.Dim);

            if (c.phase >= 6)
            {
                UI.Line(s, $"Sentencia: {CaseData.VerdictNames[c.verdict]}. Embargo: {c.embargo:N0} ¢.", 24, Pal.Sealed);
                UI.Btn(s, "Reabrir el caso (conservas mejoras y recursos)", () => { Game.StartCase(); Game.GoToPhase(1); }, Pal.Sonar, 24, 56);
            }
            else
                UI.Btn(s, "Continuar: " + PhaseNames[c.phase], () => Game.GoToPhase(c.phase), Pal.Sealed, 24, 60);

            UI.Line(s, " ", 8);
            UI.Line(s, "Sectores de campaña", 28, Pal.Sonar, true);
            for (int i = 0; i < CaseData.Sectors.Length; i++)
            {
                var d = CaseData.Sectors[i];
                bool open = i == 0;
                UI.Line(s, $"<b>{d.name}</b>{(open ? "" : "   [BLOQUEADO]")}\n{d.scenario}. Buque: {d.ship}. {d.novelty}. {d.cases} casos, nodos de la red: {d.nodes}. Desbloqueo: {d.unlock}.", 21, open ? Pal.Text : Pal.Dim);
            }
            UI.Line(s, "Este build incluye el caso del Sector 1; los Sectores 2 y 3 se muestran bloqueados.", 19, Pal.Dim);
        }

        void TabUpgrades()
        {
            UI.Clear(tabBody);
            var s = UI.Scroll(tabBody, Vector2.zero, Vector2.one);
            UI.Line(s, "Árbol de mejoras", 36, Pal.Sonar, true);
            UI.Line(s, "Cada nivel cuesta 40.000, 90.000, 180.000 y 350.000 ¢, y pide reputación 0, 30, 55 y 80.", 21, Pal.Dim);
            for (int b = 0; b < 4; b++)
            {
                int branch = b;
                UI.Line(s, CaseData.Branches[b], 26, Pal.DeckLight, true);
                for (int l = 0; l < 4; l++)
                {
                    int level = l;
                    bool owned = Game.S.upgrades[branch] > level;
                    bool next = Game.S.upgrades[branch] == level;
                    int cost = CaseData.UpgradeCost[level], rep = CaseData.UpgradeRep[level];
                    bool can = next && Game.S.budget >= cost && Game.S.reputation >= rep;
                    string state = owned ? "INSTALADA" : $"{cost:N0} ¢ · reputación {rep}";
                    var btn = UI.Btn(s, $"{CaseData.Upgrades[branch, level]}: {CaseData.UpgradeEffects[branch, level]}  —  {state}", () =>
                    {
                        Game.AddBudget(-cost);
                        Game.S.upgrades[branch] = level + 1;
                        Game.Write();
                        RefreshResources();
                        TabUpgrades();
                    }, owned ? Pal.Sealed : can ? Pal.Sonar : Pal.Dim, 20, 46);
                    btn.interactable = can;
                }
            }
        }

        void TabWorkshop()
        {
            UI.Clear(tabBody);
            var s = UI.Scroll(tabBody, Vector2.zero, Vector2.one);
            UI.Line(s, "Taller y suministros", 36, Pal.Sonar, true);
            void Refresh() { Game.Write(); RefreshResources(); TabWorkshop(); }
            var fuel = UI.Btn(s, "Comprar 1.000 L de combustible (3 ¢/L): 3.000 ¢", () => { Game.AddBudget(-3000); Game.S.fuel += 1000; Refresh(); }, null, 22, 54);
            fuel.interactable = Game.S.budget >= 3000;
            var part = UI.Btn(s, "Comprar repuesto de dron: 8.000 ¢", () => { Game.AddBudget(-8000); Game.S.droneParts++; Refresh(); }, null, 22, 54);
            part.interactable = Game.S.budget >= 8000;
            UI.Line(s, "Quedarte sin recursos limita tus opciones, pero no bloquea la partida. Sin combustible te remolcan (-24 h y -10.000 ¢). Sin repuestos, la Fase 2 se hace solo con sonar e hidrófono desde la lancha.", 21, Pal.Dim);
            UI.Line(s, "Nunca recibes dinero por persona rescatada. El presupuesto llega del 15 % de cada embargo y 50.000 ¢ por caso cerrado.", 21, Pal.Dim);
        }

        void TabCodex()
        {
            UI.Clear(tabBody);
            var s = UI.Scroll(tabBody, Vector2.zero, Vector2.one);
            UI.Line(s, "Códice", 36, Pal.Sonar, true);
            UI.Line(s, "Cada dato real del juego lleva su fuente. La ficción nunca inventa cifras del mundo real.", 21, Pal.Dim);
            foreach (var e in CaseData.Codex)
            {
                UI.Line(s, e.title, 26, Pal.DeckLight, true);
                UI.Line(s, e.body, 22);
                UI.Line(s, $"Fuente: {e.source}. {CaseData.CodexReviewed}.", 19, Pal.Dim);
                UI.Line(s, "Qué puedes hacer: " + e.action, 20, Pal.Sealed);
            }
        }

        void TabSettings()
        {
            UI.Clear(tabBody);
            var s = UI.Scroll(tabBody, Vector2.zero, Vector2.one);
            UI.Line(s, "Ajustes y ayuda", 36, Pal.Sonar, true);
            var toggle = UI.Btn(s, "", null, null, 22, 52);
            void Label() => UI.SetLabel(toggle, "Omitir descripciones sensibles: " + (Game.S.omitSensitive ? "SÍ" : "NO"));
            toggle.onClick.AddListener(() => { Game.S.omitSensitive = !Game.S.omitSensitive; Game.Write(); Label(); });
            Label();
            var vol = UI.Btn(s, "", null, null, 22, 52);
            void VolLabel() => UI.SetLabel(vol, $"Volumen general: {Mathf.RoundToInt(AudioListener.volume * 100)} %");
            vol.onClick.AddListener(() => { AudioListener.volume = AudioListener.volume <= .25f ? 1f : AudioListener.volume - .25f; VolLabel(); });
            VolLabel();
            var full = UI.Btn(s, "", null, null, 22, 52);
            void FullLabel() => UI.SetLabel(full, "Pantalla completa: " + (UnityEngine.Screen.fullScreen ? "SÍ" : "NO"));
            full.onClick.AddListener(() => { UnityEngine.Screen.fullScreen = !UnityEngine.Screen.fullScreen; FullLabel(); });
            FullLabel();

            UI.Line(s, "Si este tema te afecta", 26, Pal.DeckLight, true);
            UI.Line(s, "Estas organizaciones trabajan contra el trabajo forzado y la trata, y orientan sobre apoyo en cada país.", 21);
            UI.Btn(s, "Organización Internacional del Trabajo (ilo.org)", () => Application.OpenURL("https://www.ilo.org"), Pal.Sonar, 22, 50);
            UI.Btn(s, "Organización Internacional para las Migraciones (iom.int)", () => Application.OpenURL("https://www.iom.int"), Pal.Sonar, 22, 50);

            UI.Line(s, "Partida", 26, Pal.DeckLight, true);
            UI.Btn(s, "Borrar partida y empezar de cero", () => { Game.NewGame(); Game.Write(); ShowWarning(); }, Pal.Danger, 22, 50);

            UI.Line(s, "Controles", 26, Pal.DeckLight, true);
            UI.Line(s, "Lancha: W/S marcha, A/D timón, Espacio pulso de sonar. Opcional: H hidrófono, T dron, clic derecho girar cámara.\nA bordo: WASD mover, ratón mirar, Shift correr, E usar lo que tienes delante, F visor, Q herramienta, Tab maletín, Esc pausa. Opcional: clic derecho y clic izquierdo para fotografiar de lejos.", 21, Pal.Dim);
        }
    }
}
