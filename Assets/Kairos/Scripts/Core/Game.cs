using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kairos
{
    /// Paleta del GDD: el color da información de juego, no solo ambiente.
    public static class Pal
    {
        public static readonly Color Ocean = Hex("#0B1A24"), Storm = Hex("#2E3A44"), Rust = Hex("#8A3B12"),
            HullPaint = Hex("#5E1F1C"), DeckLight = Hex("#F2A73B"), Sonar = Hex("#3FE0D0"),
            ThermCold = Hex("#2050FF"), ThermHot = Hex("#FFD27A"), Sealed = Hex("#6CFF8A"),
            Text = new Color(.86f, .92f, .94f), Dim = new Color(.55f, .65f, .7f), Danger = new Color(1f, .35f, .25f),
            PanelBg = new Color(.03f, .08f, .11f, .93f), Line = new Color(.25f, .4f, .45f, .8f);

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }

    [Serializable] public class SealedEvidence { public string id, sha, stamp; }

    [Serializable]
    public class PersonState
    {
        public string id;
        public int trust;
        public bool talked, explained, medkit, ration, freed, following, rescued, repatriated, helped;
        public int testimony; // 0 sin pedir, 1 dado, 2 decidió no darlo
        public bool[] dignified = new bool[5];
    }

    [Serializable]
    public class CaseState
    {
        public int phase = 1; // 1-4 fases, 5 repatriación, 6 cerrado
        public float hours;
        public int abLeft = 60, abDay;
        public int revealed;     // máscara de capas compradas
        public bool sarDegraded; // pasada SAR comprada con mar gruesa
        public int ir;
        public float suspicion, confidence;
        public bool dumped;      // el buque tiró pruebas al mar tras detectarnos
        public int[] scanSignals = new int[5];  // bits: 1 densidad, 2 térmica, 4 sonido
        public int marked = -1;  // compartimento que marcaste como "aquí hay personas"
        public bool boarded;
        public bool hasCheckpoint; // último punto seguro a bordo
        public float cpX, cpY, cpZ;
        public List<SealedEvidence> evidence = new List<SealedEvidence>();
        public List<string> burned = new List<string>();
        public List<string> openDoors = new List<string>();
        public List<PersonState> persons = new List<PersonState>();
        public bool stunUsed;
        public int kits = 3, rations = 4, jammerCharges = 3;
        public int exitUsed; // 1 patrullera, 2 escolta, 3 puente
        public float trialDays = -1;
        public List<string> presented = new List<string>(), links = new List<string>();
        public int verdict; // 1 retención, 2 congelación, 3 condena
        public int embargo;
        public bool failed;
    }

    [Serializable]
    public class Save
    {
        public string playerName = "Inspector", gender = "No especificar", origin = "Perú";
        public bool omitSensitive, warningAccepted;
        public int budget = 500000, droneParts = 6, reputation = 20, sentences, casesClosed;
        public float fuel = 4000;
        public int[] upgrades = new int[4];
        public bool networkReorganized;
        public List<string> hints = new List<string>(); // tutorial ya visto
        public CaseState c = new CaseState();
    }

    public static class Game
    {
        public const string MenuScene = "00_Menu";
        public static readonly string[] PhaseScenes = { MenuScene, "01_Rastreo", "02_Sonar", "03_Infiltracion", "04_Juicio", "04_Juicio", MenuScene };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SpanishNumbers() => // 3.000.000 ¢ y 3,5 días
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-ES");

        static Save s;
        static string SavePath => Path.Combine(Application.persistentDataPath, "kairos_save.json");
        public static bool HasSave => File.Exists(SavePath);

        public static Save S
        {
            get
            {
                if (s == null)
                {
                    try { s = HasSave ? JsonUtility.FromJson<Save>(File.ReadAllText(SavePath)) : null; }
                    catch (Exception e) { Debug.LogWarning("Partida ilegible, se empieza de cero: " + e.Message); }
                    if (s == null) NewGame();
                }
                return s;
            }
        }
        public static CaseState C => S.c;

        public static void NewGame() { s = new Save(); StartCase(); }

        public static void StartCase()
        {
            S.c = new CaseState { trialDays = -1 };
            foreach (var p in CaseData.People)
                S.c.persons.Add(new PersonState { id = p.id, trust = p.startTrust, freed = p.startsFree });
        }

        public static void Write()
        {
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(S, true)); }
            catch (Exception e) { Debug.LogError("No se pudo guardar: " + e.Message); }
        }

        public static bool Has(int branch, int level) => S.upgrades[branch] >= level;

        public static void GoToPhase(int phase)
        {
            C.phase = phase;
            Write();
            Time.timeScale = 1;
            SceneManager.LoadScene(PhaseScenes[phase]);
        }

        public static void GoToMenu()
        {
            Write();
            Time.timeScale = 1;
            SceneManager.LoadScene(MenuScene);
        }

        public static PersonState Person(string id) => C.persons.Find(p => p.id == id);
        public static bool HasEvidence(string id) => C.evidence.Exists(e => e.id == id);

        /// Sella una prueba con hash real. Devuelve false si el maletín (máx. 12) está lleno.
        public static bool SealEvidence(string id, string gps)
        {
            if (HasEvidence(id)) return true;
            if (C.evidence.Count >= CaseData.MaxEvidence) return false;
            var stamp = $"{gps} · {FormatClock(C.hours)}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(id + "|" + stamp + "|" + S.playerName));
            C.evidence.Add(new SealedEvidence { id = id, stamp = stamp, sha = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant() });
            return true;
        }

        public static int CountCategoryA()
        {
            int n = 0;
            foreach (var e in C.evidence) if (CaseData.Evidence(e.id).cat == 'A') n++;
            return n;
        }

        public static void AddBudget(int amount) => S.budget = Mathf.Max(0, S.budget + amount);
        public static void AddReputation(int amount) => S.reputation = Mathf.Clamp(S.reputation + amount, 0, 100);

        /// El caso arranca a las 22:00 del día 1.
        public static string FormatClock(float hours)
        {
            float h = hours + 22f;
            return $"Día {Mathf.FloorToInt(hours / 24f) + 1} · {Mathf.FloorToInt(h % 24f):00}:{Mathf.FloorToInt((h % 1f) * 60f):00}";
        }
    }
}
