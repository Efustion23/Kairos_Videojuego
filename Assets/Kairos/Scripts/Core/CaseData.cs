using System.Collections.Generic;
using UnityEngine;

namespace Kairos
{
    public class LayerDef { public string name, shows, limit; public int cost; }
    public class Anomaly { public string name; public float weight, conf; public int layer; public float minHours; public string detail; }
    public class ShipDef { public string id, name, type, flag, optical; public float lat, lon, mRoute; public bool ais, target; public float lastLat, lastLon; public Anomaly[] anomalies; }
    public class Compartment
    {
        public string name; public Vector3 center, size; public int kind; // 0 bodega, 1 personas, 2 máquinas
        public string temp, density, cycle, water, sound; public float densityValue;
    }
    public class PersonDef { public string id, name, origin, voice, intro, plan, testimony, testimonySafe, help, epilogueDignified, epilogueBasic; public int startTrust; public bool startsFree, willTestify; public string testimonyEvidence; }
    public class EvidenceDef { public string id, title, desc, descSafe; public char cat; public int[] indicators; public string[] edges; }
    public class NodeDef { public string name, role; public Vector2 pos; }
    public class SectorDef { public string name, unlock, ship, novelty, scenario; public int cases, needSentences, needReputation; public string nodes; public float lat, lon; }
    public class CodexEntry { public string title, body, source, action; }

    /// Datos del caso de apertura. Todos los buques y empresas son ficticios (ver GDD).
    public static class CaseData
    {
        public const int MaxEvidence = 13; // caben todas: nadie debe descartar pruebas
        public const int IrToScan = 60, ConfidenceToBoard = 70;

        // ---------- Fase 1 ----------
        public const int LayerAIS = 0, LayerVMS = 1, LayerVIIRS = 2, LayerSAR = 3, LayerOptical = 4;
        public static readonly LayerDef[] Layers =
        {
            new LayerDef { name = "AIS", shows = "Posición e identidad declarada", cost = 0, limit = "El buque puede apagarlo o falsearlo" },
            new LayerDef { name = "VMS", shows = "Seguimiento oficial del Estado del pabellón", cost = 5, limit = "Solo países con convenio" },
            new LayerDef { name = "Luces VIIRS", shows = "Buques con focos potentes", cost = 8, limit = "Solo de noche y sin nubes" },
            new LayerDef { name = "Radar SAR", shows = "Cascos reales, con o sin AIS", cost = 15, limit = "Pasadas cada 6 a 12 h; pierde precisión con mar gruesa" },
            new LayerDef { name = "Óptica", shows = "Foto detallada de cubierta", cost = 25, limit = "Bloqueada por nubes y tormentas" },
        };

        static Anomaly A(string n, float w, float c, int layer, string detail, float minHours = 0) =>
            new Anomaly { name = n, weight = w, conf = c, layer = layer, detail = detail, minHours = minHours };

        // Los pesos son los del GDD; las confianzas c están elegidas para que Hai Feng 77 dé exactamente IR 72.
        public static readonly ShipDef[] Ships =
        {
            new ShipDef { id = "hf77", name = "Hai Feng 77", type = "Potero, unos 90 m", flag = "Bandera de conveniencia", target = true, ais = false,
                lat = -45.4f, lon = -59.2f, lastLat = -46.5f, lastLon = -60.3f, mRoute = 1.2f,
                optical = "Foto de cubierta: racks de luces apagados y un frigorífico abarloado. Pocas figuras en cubierta para un buque de este tamaño.",
                anomalies = new[] {
                    A("Apagón de AIS", 20, 1f, LayerAIS, "Señal perdida hace 31 h, a 1 milla del borde de la milla 200."),
                    A("Bandera de conveniencia", 5, 1f, LayerAIS, "Pabellón sin capacidad real de inspección."),
                    A("Identidad anómala", 15, .4f, LayerVMS, "El pabellón declarado no tiene registro VMS para este casco."),
                    A("Giro en espiral", 10, .3f, LayerVIIRS, "Patrón de luces que no coincide con la faena declarada."),
                    A("Barco oscuro", 20, .8f, LayerSAR, "Casco de 90 m sin ninguna señal de AIS asociada."),
                    A("Transbordo", 25, .4f, LayerOptical, "Carga pasada a un frigorífico en altamar, sin tocar puerto."),
                    A("Denuncia externa", 15, 1f, -1, "Una ONG laboral reenvía el mensaje de un familiar: hace meses que no sabe de él.", 30f),
                } },
            new ShipDef { id = "polar", name = "Polar Meridian", type = "Frigorífico", flag = "Bandera de conveniencia", ais = true,
                lat = -46.1f, lon = -58.2f, mRoute = 1.2f,
                optical = "Buque frigorífico con grúas de transbordo. Sin indicios a bordo: se rastrea, no se aborda.",
                anomalies = new[] {
                    A("Merodeo de frigorífico", 10, .8f, LayerAIS, "Lleva días girando alrededor de la flota."),
                    A("Bandera de conveniencia", 5, 1f, LayerAIS, "Pabellón sin capacidad real de inspección."),
                    A("Transbordo", 25, .5f, LayerSAR, "Dos cascos abarloados en la pasada de radar."),
                } },
            new ShipDef { id = "bravo", name = "Austral Bravo", type = "Potero", flag = "Registro con convenio", ais = true,
                lat = -44.9f, lon = -60.8f, mRoute = 1f,
                optical = "Faena normal, tripulación visible en cubierta.",
                anomalies = new Anomaly[0] },
            new ShipDef { id = "nube", name = "Nube del Sur 12", type = "Potero", flag = "Bandera de conveniencia", ais = true,
                lat = -47.0f, lon = -59.0f, mRoute = 1f,
                optical = "Faena normal. El giro se explica por la corriente.",
                anomalies = new[] {
                    A("Bandera de conveniencia", 5, 1f, LayerAIS, "Pabellón sin capacidad real de inspección."),
                    A("Giro en espiral", 10, .5f, LayerVIIRS, "Patrón irregular, probablemente sigue el calamar."),
                } },
            new ShipDef { id = "s2", name = "Contacto S-2", type = "Casco sin identificar, unos 30 m", flag = "Desconocida", ais = false,
                lat = -44.6f, lon = -58.0f, mRoute = 1.2f,
                optical = "Embarcación auxiliar pequeña. No hay indicios de personas retenidas.",
                anomalies = new[] { A("Barco oscuro", 20, .6f, LayerSAR, "Pequeño casco sin AIS.") } },
        };

        public static ShipDef Target => Ships[0];

        public static bool LayerVisible(ShipDef ship, Anomaly a, int mask, float hours)
        {
            if (a.layer < 0) return hours >= a.minHours;
            int layer = a.layer;
            if (a.name == "Transbordo" && Game.Has(3, 1)) layer = LayerAIS; // Detector de encuentros
            return (mask & (1 << layer)) != 0;
        }

        /// IR = min(100, Σ w·c × M_ruta), menos el rastro perdido (5 por día).
        public static int ComputeIR(ShipDef ship, int mask, float hours, bool sarDegraded = false, float decayPerDay = 5f)
        {
            float sum = 0;
            foreach (var a in ship.anomalies)
                if (LayerVisible(ship, a, mask, hours))
                    sum += a.weight * a.conf * (a.layer == LayerSAR && sarDegraded ? .7f : 1f);
            float ir = Mathf.Min(100f, sum * ship.mRoute);
            ir -= decayPerDay * Mathf.Floor(hours / 24f);
            return Mathf.Clamp(Mathf.RoundToInt(ir), 0, 100);
        }

        // Nubes sobre el Sector 1: el caso arranca nublado, despeja a las 02:00.
        public static bool Cloudy(float h) { float t = h % 24f; return t < 4f || (t >= 16f && t < 20f); }
        public static bool Night(float h) { float t = (h + 22f) % 24f; return t >= 20f || t < 6f; }
        // Pasadas SAR cada 6-12 h: ventana de 1 h cuando toca.
        static readonly float[] SarPasses = { 2, 11, 17, 26, 35, 41, 50, 59, 65, 74, 83, 89 };
        public static float NextSarPass(float h) { foreach (var p in SarPasses) if (p + 1f > h) return p; return float.MaxValue; }
        public static bool SarNow(float h) { var n = NextSarPass(h); return h >= n && h < n + 1f; }
        public static int DouglasAt(float h) => h < 36 ? 3 : h < 60 ? 6 : 4;

        // ---------- Fase 2 ----------
        // Coordenadas en el espacio del buque (ver KairosBuilder). Lecturas del GDD.
        public static readonly Compartment[] Compartments =
        {
            new Compartment { name = "K1 Popa estribor", center = new Vector3(3.9f, 1.5f, -21f), size = new Vector3(5.2f, 8f, 14f), kind = 2,
                temp = "45 °C, estable", density = "0,60, maquinaria", densityValue = .6f, cycle = "Sigue al motor", water = "Circuito cerrado", sound = "Motor y compresores" },
            new Compartment { name = "K2 Centro estribor", center = new Vector3(3.9f, 1.5f, -5f), size = new Vector3(5.2f, 8f, 18f), kind = 0,
                temp = "-24 °C, uniforme", density = "0,75, homogénea", densityValue = .75f, cycle = "Constante 24 h", water = "Sin consumo", sound = "Zumbido de compresores" },
            new Compartment { name = "K3 Proa estribor", center = new Vector3(3.9f, 1.5f, 13f), size = new Vector3(5.2f, 8f, 18f), kind = 0,
                temp = "-21 °C, uniforme", density = "0,75, homogénea", densityValue = .75f, cycle = "Constante 24 h", water = "Sin consumo", sound = "Zumbido de compresores" },
            new Compartment { name = "K4 Centro babor", center = new Vector3(-3.9f, 1.5f, 2f), size = new Vector3(5.2f, 8f, 16f), kind = 1,
                temp = "24-34 °C, focos de 36-37 °C", density = "0,35, objetos dispersos", densityValue = .35f, cycle = "Sube de noche", water = "Unos 3,5 L por persona/día", sound = "Voces, golpes y pasos" },
            new Compartment { name = "K5 Proa babor", center = new Vector3(-3.9f, 1.5f, 16f), size = new Vector3(5.2f, 8f, 12f), kind = 0,
                temp = "-28 °C, uniforme", density = "0,75, homogénea", densityValue = .75f, cycle = "Constante 24 h", water = "Sin consumo", sound = "Zumbido de compresores" },
        };
        public const int PersonsCompartment = 3;

        /// Basta con encontrar el compartimento con personas: 40 por marcarlo, 40 por su lectura de sonar,
        /// y 10 por cada ayuda opcional (dron e hidrófono).
        public static float ComputeConfidence(int marked, int[] signals)
        {
            if (marked != PersonsCompartment) return 0;
            int s = signals[PersonsCompartment];
            return 40f + ((s & 1) != 0 ? 40f : 0) + ((s & 2) != 0 ? 10f : 0) + ((s & 4) != 0 ? 10f : 0);
        }

        // ---------- Fase 3 ----------
        public static readonly PersonDef[] People =
        {
            new PersonDef { id = "rodel", name = "Rodel Santos", origin = "Filipinas", voice = "Habla bajo, sin dejar de limpiar.", startTrust = 30, startsFree = true, willTestify = true, testimonyEvidence = "t_rodel",
                intro = "—Aquí nadie pregunta cómo estoy. ¿Quién eres tú?",
                plan = "—Si de verdad hay una salida, te doy la clave de la escotilla de babor. Es 4-7-1-9.",
                testimony = "«Firmé en una agencia por seis meses. Llevo veintidós. Nunca vi un puerto.»",
                testimonySafe = "Describe el engaño en el contrato y el aislamiento a bordo.",
                help = "Rodel conoce las rondas: te marca cuándo va a pasar el siguiente guardia.",
                epilogueDignified = "Rodel volvió a Iloilo con sus documentos y el salario adeudado. Hoy forma a otros marineros sobre sus derechos.",
                epilogueBasic = "Rodel volvió a casa, pero sigue reclamando sus salarios por su cuenta." },
            new PersonDef { id = "ari", name = "Ari Wibowo", origin = "Indonesia", voice = "Te mira a los ojos antes de contestar.", startTrust = 20, willTestify = true, testimonyEvidence = "t_ari",
                intro = "—No vengas a salvarme. Dime qué piensas hacer y yo decido.",
                plan = "—Si hay plan, quiero saberlo entero. Los demás me escuchan a mí.",
                testimony = "«Cuando alguien pedía parar, lo encerraban aquí. Nos decían que nadie nos buscaba.»",
                testimonySafe = "Describe amenazas y violencia física a bordo. Descripción omitida por tus ajustes.",
                help = "Ari golpea el mamparo a propósito: atrae al guardia más cercano lejos de ti.",
                epilogueDignified = "Ari declaró ante el tribunal por decisión propia y hoy dirige una asociación de pescadores en Bitung.",
                epilogueBasic = "Ari volvió a Bitung. Prefiere no hablar todavía de lo ocurrido, y esa decisión también es suya." },
            new PersonDef { id = "moussa", name = "Moussa Diallo", origin = "Senegal", voice = "Se sostiene la mano herida, sin quejarse.", startTrust = 15, willTestify = true, testimonyEvidence = "t_moussa",
                intro = "—Me dijeron que descontarían el viaje de mi sueldo. Sigo debiendo. ¿Tú también vienes a cobrarme?",
                plan = "—Hay una puerta de servicio detrás de ese mamparo, va al pañol. Te la abro desde aquí.",
                testimony = "«Cada mes la deuda crecía. Comida, guantes, hasta el agua. Nunca vi un papel.»",
                testimonySafe = "Describe la deuda impuesta y el abuso de su situación.",
                help = "Moussa abre la puerta de servicio: tienes un atajo hasta el pañol.",
                epilogueDignified = "Moussa recibió atención médica en la mano y volvió a Mbour con su familia. La deuda quedó anulada por sentencia.",
                epilogueBasic = "Moussa volvió a Mbour. Su mano necesita un tratamiento que aún está intentando costear." },
            new PersonDef { id = "thant", name = "Thant Zin", origin = "Myanmar", voice = "Habla despacio, eligiendo cada palabra.", startTrust = 25, willTestify = false,
                intro = "—Quiero irme a casa. Solo eso.",
                plan = "—Iré contigo. Pero no voy a contar nada a ningún juez. Tengo familia allí.",
                testimony = "", testimonySafe = "",
                help = "Thant vigila la puerta mientras los demás salen.",
                epilogueDignified = "Thant está con su familia. Eligió no testificar, y el proceso respetó esa decisión.",
                epilogueBasic = "Thant está con su familia. Eligió no testificar, y volvió sin apoyo psicosocial." },
        };
        public static PersonDef PersonDefOf(string id) => System.Array.Find(People, p => p.id == id);

        public static readonly string[] Needs = { "Atención médica", "Documentos", "Traslado", "Salarios adeudados", "Apoyo psicosocial" };
        public static readonly int[] NeedCostBasic = { 2000, 1000, 2000, 0, 0 };
        public static readonly int[] NeedCostDignified = { 12000, 6000, 10000, 8000, 9000 };

        // ---------- Fase 4 ----------
        public static readonly string[] Indicators =
        {
            "Abuso de vulnerabilidad", "Engaño", "Restricción de movimiento", "Aislamiento", "Violencia física",
            "Intimidación y amenazas", "Retención de documentos", "Retención de salarios", "Servidumbre por deudas",
            "Condiciones abusivas de vida y trabajo", "Horas extra excesivas",
        };

        static EvidenceDef E(string id, string title, char cat, string desc, int[] ind, string[] edges, string safe = null) =>
            new EvidenceDef { id = id, title = title, cat = cat, desc = desc, descSafe = safe ?? desc, indicators = ind, edges = edges };

        static readonly int[] None = new int[0];
        static readonly string[] NoEdge = new string[0];

        public static readonly EvidenceDef[] EvidenceDefs =
        {
            E("e_celda", "Compartimento oculto", 'A', "Literas soldadas tras un mamparo falso, sin ventilación legal ni salida interior.", new[] { 2, 9 }, NoEdge),
            E("e_pasaportes", "Pasaportes retenidos", 'A', "Pasaportes de la tripulación guardados bajo llave en el camarote del capitán.", new[] { 6 }, new[] { "0-3" }),
            E("e_comms", "Comunicaciones del puente", 'A', "Órdenes por radio de Cormorán Trade a un enlace de Tidewater Austral.", new[] { 3, 5 }, new[] { "4-5" }),
            E("e_deudas", "Libro de deudas", 'B', "Cuentas por persona, con cargos por comida y equipo pagados a Cormorán Trade.", new[] { 7, 8 }, new[] { "3-4" }),
            E("e_contratos", "Contratos falsos", 'B', "Contratos de seis meses de la Agencia Puerto Claro, con cláusulas en un idioma que no leían.", new[] { 0, 1 }, new[] { "0-2" }),
            E("e_manifiesto", "Manifiesto de transbordo", 'B', "Carga traspasada al Polar Meridian en altamar.", None, new[] { "0-1" }),
            E("e_registro", "Registro societario", 'B', "Marea Austral Shipping figura como armador; su único socio es Cormorán Trade Ltd.", None, new[] { "0-3", "3-4" }),
            E("e_turnos", "Planilla de turnos", 'C', "Turnos de hasta 20 horas, firmados por el contramaestre.", new[] { 10 }, NoEdge),
            E("e_candados", "Candados exteriores", 'C', "La escotilla solo se cierra desde fuera.", new[] { 2 }, NoEdge),
            E("e_facturas", "Facturas de venta", 'C', "Ventas del Polar Meridian a una filial de Tidewater Austral.", None, new[] { "1-5" }),
            E("t_rodel", "Testimonio de Rodel Santos", 'B', "Testimonio con consentimiento y lector biométrico.", new[] { 1, 3 }, new[] { "0-2" }),
            E("t_ari", "Testimonio de Ari Wibowo", 'A', "Testimonio con consentimiento y lector biométrico.", new[] { 4, 5 }, NoEdge, "Testimonio con consentimiento. Descripción omitida por tus ajustes."),
            E("t_moussa", "Testimonio de Moussa Diallo", 'A', "Testimonio con consentimiento y lector biométrico.", new[] { 0, 8 }, new[] { "0-2" }),
        };
        public static EvidenceDef Evidence(string id) => System.Array.Find(EvidenceDefs, e => e.id == id);
        // Lo que se puede perder nunca incluye lo necesario para llegar al beneficiario final.
        public static readonly string[] DumpedWhenDetected = { "e_turnos", "e_candados" };
        public static readonly string[] BurnOrder = { "e_candados", "e_turnos", "e_contratos", "e_deudas" };

        public static readonly NodeDef[] Nodes =
        {
            new NodeDef { name = "Hai Feng 77", role = "Buque", pos = new Vector2(.12f, .5f) },
            new NodeDef { name = "Polar Meridian", role = "Frigorífico", pos = new Vector2(.42f, .85f) },
            new NodeDef { name = "Agencia Puerto Claro", role = "Reclutadora", pos = new Vector2(.42f, .15f) },
            new NodeDef { name = "Marea Austral Shipping", role = "Armador registrado", pos = new Vector2(.4f, .5f) },
            new NodeDef { name = "Cormorán Trade Ltd", role = "Empresa fachada", pos = new Vector2(.64f, .5f) },
            new NodeDef { name = "Tidewater Austral", role = "Beneficiario final", pos = new Vector2(.875f, .5f) },
        };

        public static string EdgeKey(int a, int b) => a < b ? a + "-" + b : b + "-" + a;

        /// ¿Hay camino de pruebas del buque (0) al beneficiario final (5)?
        public static bool ChainComplete(List<string> links)
        {
            var seen = new HashSet<int> { 0 };
            for (bool grew = true; grew;)
            {
                grew = false;
                foreach (var l in links)
                {
                    var p = l.Split('-');
                    int a = int.Parse(p[0]), b = int.Parse(p[1]);
                    if (seen.Contains(a) != seen.Contains(b)) { seen.Add(a); seen.Add(b); grew = true; }
                }
            }
            return seen.Contains(5);
        }

        public static int CountIndicators(List<string> presented)
        {
            var set = new HashSet<int>();
            foreach (var id in presented) foreach (var i in Evidence(id).indicators) set.Add(i);
            return set.Count;
        }

        public static int VerdictFor(int indicators) => indicators >= 11 ? 3 : indicators >= 7 ? 2 : indicators >= 4 ? 1 : 0;
        public static readonly string[] VerdictNames = { "Sin sentencia", "Orden de Retención: bloqueo en aduana", "Congelación de cuentas y cierre de puertos", "Condena penal del beneficiario final" };
        public static readonly int[] VerdictEmbargo = { 0, 400000, 1200000, 3000000 };
        public static readonly float[] ExitEmbargoMul = { 1f, .8f, 1f, 1.5f };
        public static readonly string[] ExitNames = { "Retirada", "Patrullera aliada", "Escolta en sigilo", "Puente asegurado" };

        // ---------- Progresión ----------
        public static readonly string[] Branches = { "Sonar y sensores", "Lancha", "Traje del inspector", "Algoritmos de rastreo" };
        public static readonly string[,] Upgrades =
        {
            { "Filtro adaptativo", "Haz estabilizado", "Hidrófono direccional 3D", "Sonar de apertura sintética" },
            { "Casco silencioso", "Autonomía ampliada", "Estabilizadores activos", "Lancha semisumergible" },
            { "Suela amortiguada", "Apantallamiento térmico", "Flotador de extracción", "Traductor en casco (12 idiomas)" },
            { "Detector de encuentros", "Modelo predictivo", "Cruce SAR-AIS", "Desencriptador societario" },
        };
        public static readonly string[,] UpgradeEffects =
        {
            { "Ventana de cresta de 1 a 1,4 s", "Cada pulso avanza un 50 % más", "Hidrófono a 350 m en vez de 200 m", "Pulso a pleno rendimiento hasta 600 m" },
            { "La estela delata a 200 m en vez de 300 m", "30 % menos de combustible", "Pulso fuera de cresta rinde 85 %", "La estela delata a 100 m; menos sospecha" },
            { "Pasos 30 % más silenciosos", "Guardias te detectan 20 % más lento", "Las personas escoltadas van más rápido", "Más confianza al hablar en su idioma" },
            { "Transbordos visibles en AIS", "El IR cae 2 por día en vez de 5", "SAR cuesta 8 AB en vez de 15", "Cada acción del juicio cuesta la mitad de tiempo" },
        };
        public static readonly int[] UpgradeCost = { 40000, 90000, 180000, 350000 };
        public static readonly int[] UpgradeRep = { 0, 30, 55, 80 };

        public static readonly SectorDef[] Sectors =
        {
            new SectorDef { name = "1. Atlántico Sur (milla 201)", scenario = "Flota de poteros al borde de la zona económica exclusiva argentina", unlock = "Inicio de campaña", ship = "Potero", novelty = "Tutorial, luces VIIRS y caso Hai Feng 77", cases = 2, nodes = "6", lat = -46, lon = -59.5f },
            new SectorDef { name = "2. Golfo de Guinea", scenario = "Zona crítica de apagones de AIS (Welch et al., 2022)", unlock = "2 sentencias y reputación 30", ship = "Arrastrero", novelty = "Apagones y transbordos frecuentes", cases = 3, needSentences = 2, needReputation = 30, nodes = "Unos 15", lat = 2, lon = 2 },
            new SectorDef { name = "3. Océano Índico Occidental", scenario = "Palangreros que pasan años sin tocar puerto", unlock = "4 sentencias y reputación 45", ship = "Palangrero y frigorífico", novelty = "Aislamiento extremo, perros guardianes y caso final contra Tidewater", cases = 3, needSentences = 4, needReputation = 45, nodes = "25 a 30", lat = -10, lon = 58 },
        };

        public static readonly CodexEntry[] Codex =
        {
            new CodexEntry { title = "Trabajo forzado en la pesca", body = "La OIT estima unos 128.000 pescadores atrapados en trabajo forzado a bordo.", source = "Organización Internacional del Trabajo (OIT)", action = "Pregunta de dónde viene el pescado que compras." },
            new CodexEntry { title = "Los 11 indicadores de la OIT", body = "Abuso de vulnerabilidad, engaño, restricción de movimiento, aislamiento, violencia física, intimidación, retención de documentos, retención de salarios, servidumbre por deudas, condiciones abusivas y horas extra excesivas.", source = "OIT, indicadores de trabajo forzado", action = "Aprende a reconocerlos también en tierra." },
            new CodexEntry { title = "Apagones de AIS", body = "Los buques pueden apagar su transpondedor. El Golfo de Guinea es una de las zonas críticas de estos apagones.", source = "Welch et al., 2022", action = "Explora datos abiertos de actividad pesquera." },
            new CodexEntry { title = "ODS 8, meta 8.7", body = "Adoptar medidas para erradicar el trabajo forzoso y poner fin a la esclavitud moderna y la trata.", source = "Naciones Unidas, Objetivos de Desarrollo Sostenible", action = "Consulta qué informa tu país sobre esta meta." },
            new CodexEntry { title = "ODS 16, metas 16.2, 16.4 y 16.5", body = "Poner fin a la trata, reducir las corrientes financieras ilícitas y la corrupción: por eso aquí la justicia se persigue por el dinero.", source = "Naciones Unidas, Objetivos de Desarrollo Sostenible", action = "Apoya el periodismo que investiga beneficiarios finales." },
            new CodexEntry { title = "Sobre esta ficción", body = "Buques y empresas del juego son inventados, para no atribuir delitos a nombres reales. Benjina y Da Wang, casos reales, quedan como documentación y no como escenarios.", source = "Equipo KAIROS", action = "Si te afecta este tema, usa los enlaces de ayuda del menú de ajustes." },
        };
        public const string CodexReviewed = "Revisado: nov. 2026";
    }
}
