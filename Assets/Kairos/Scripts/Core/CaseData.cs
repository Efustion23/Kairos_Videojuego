using System.Collections.Generic;
using UnityEngine;

namespace Kairos
{
    public class LayerDef { public string name, shows; public int cost; }
    public class Anomaly { public string name; public float weight, conf; public int layer; public float minHours; public string detail; }
    public class ShipDef { public string id, name, type, flag, optical; public float lat, lon, mRoute; public bool ais, target; public float lastLat, lastLon; public Anomaly[] anomalies; }
    public class Compartment
    {
        public string name; public Vector3 center, size; public int kind; // 0 bodega, 1 personas, 2 máquinas
        public string temp, density, cycle, water, sound; public float densityValue;
    }
    public class PersonDef { public string id, name, origin, voice, intro, plan, testimony, testimonySafe, help; public int startTrust; public bool startsFree, willTestify; public string testimonyEvidence; }
    public class EvidenceDef { public string id, title, desc, descSafe; public char cat; public int[] indicators; public string[] edges; }
    public class NodeDef { public string name, role; public Vector2 pos; }
    public class SectorDef { public string name, ship, novelty, scenario, nodes; public int cases; public float lat, lon; }
    public class CodexEntry { public string title, body, source, action; }

    /// Datos del caso de apertura. Todos los buques y empresas son ficticios.
    public static class CaseData
    {
        public const int MaxEvidence = 15; // caben todas: nadie debe descartar pruebas
        public const int IrToScan = 60, ConfidenceToBoard = 70;

        // ---------- Fase 1 ----------
        public const int LayerAIS = 0, LayerVMS = 1, LayerVIIRS = 2, LayerSAR = 3, LayerOptical = 4;
        public static readonly LayerDef[] Layers =
        {
            new LayerDef { name = "AIS", shows = "Posición e identidad declarada", cost = 0 },
            new LayerDef { name = "VMS", shows = "Seguimiento oficial del Estado del pabellón", cost = 5 },
            new LayerDef { name = "Luces VIIRS", shows = "Buques con focos potentes", cost = 8 },
            new LayerDef { name = "Radar SAR", shows = "Cascos reales, con o sin AIS", cost = 15 },
            new LayerDef { name = "Óptica", shows = "Foto detallada de cubierta", cost = 25 },
        };

        static Anomaly A(string n, float w, float c, int layer, string detail, float minHours = 0) =>
            new Anomaly { name = n, weight = w, conf = c, layer = layer, detail = detail, minHours = minHours };

        // Las confianzas c están ajustadas para que Hai Feng 77 dé IR 72 con todas las capas.
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

        public static readonly ShipDef[] ShipsSector2 =
        {
            new ShipDef { id = "kestrel", name = "Kestrel Explorer", type = "Arrastrero industrial, 85 m", flag = "Bandera de conveniencia", target = true, ais = false,
                lat = 2.3f, lon = 2.4f, lastLat = 1.7f, lastLon = 1.5f, mRoute = 1.25f,
                optical = "Foto de cubierta: redes de arrastre activas en zona vedada y licencia pintada sobre otro nombre.",
                anomalies = new[] {
                    A("Apagón masivo de AIS", 20, 1f, LayerAIS, "Señal perdida hace 42 h en zona crítica del Golfo de Guinea."),
                    A("Licencia local falsificada", 15, .6f, LayerVMS, "El código de permiso no figura en el registro oficial."),
                    A("Arrastre nocturno sin luces", 10, .5f, LayerVIIRS, "Brillo tenue de maniobra nocturna en reserva."),
                    A("Barco oscuro", 20, .8f, LayerSAR, "Casco de 85 m detectado sin señal AIS."),
                    A("Transbordo a frigorífico", 25, .5f, LayerOptical, "Carga descargada a Atlantic Reef en altamar."),
                } },
            new ShipDef { id = "atlantic_reef", name = "Atlantic Reef", type = "Frigorífico", flag = "Bandera de conveniencia", ais = true,
                lat = 1.5f, lon = 2.9f, mRoute = 1.2f,
                optical = "Frigorífico de apoyo. Se rastrea la conexión financiera, no se aborda.",
                anomalies = new[] {
                    A("Merodeo de frigorífico", 10, .8f, LayerAIS, "Esperando carga en el límite marítimo."),
                    A("Bandera de conveniencia", 5, 1f, LayerAIS, "Registro opaco."),
                } },
            new ShipDef { id = "tema_star", name = "Tema Star", type = "Arrastrero", flag = "Registro con convenio", ais = true,
                lat = 2.8f, lon = 1.2f, mRoute = 1f,
                optical = "Faena autorizada con observador a bordo.",
                anomalies = new Anomaly[0] },
        };

        public static readonly ShipDef[] ShipsSector3 =
        {
            new ShipDef { id = "oceanic9", name = "Oceanic Crown 9", type = "Palangrero de atún, 95 m", flag = "Bandera de conveniencia", target = true, ais = false,
                lat = -10.2f, lon = 58.3f, lastLat = -11.0f, lastLon = 57.1f, mRoute = 1.3f,
                optical = "Foto de cubierta: palangrero de larga distancia sin tocar puerto hace 24 meses, vinculado a Tidewater Holdings.",
                anomalies = new[] {
                    A("Apagón prolongado de AIS", 20, 1f, LayerAIS, "Más de 60 h sin transpondedor en aguas internacionales."),
                    A("Sin escala en puerto (2 años)", 15, .6f, LayerVMS, "Tripulación retenida en altamar sin relevo."),
                    A("Faena continua 24 h", 10, .5f, LayerVIIRS, "Luces de cubierta activas toda la noche."),
                    A("Casco oscuro de gran porte", 20, .85f, LayerSAR, "Palangrero de 95 m confirmado por radar."),
                    A("Transbordo a nodriza", 25, .5f, LayerOptical, "Reabastecimiento y descarga con Mombasa Reefer."),
                } },
            new ShipDef { id = "mombasa", name = "Mombasa Reefer", type = "Buque nodriza frigorífico", flag = "Bandera de conveniencia", ais = true,
                lat = -9.4f, lon = 58.9f, mRoute = 1.2f,
                optical = "Buque nodriza de Tidewater Holdings. Provee combustible y recoge pesca.",
                anomalies = new[] {
                    A("Encuentro prolongado", 15, .8f, LayerAIS, "Permanece junto a palangreros en altamar."),
                } },
            new ShipDef { id = "seychelles", name = "Seychelles Drift", type = "Palangrero", flag = "Registro con convenio", ais = true,
                lat = -10.8f, lon = 56.8f, mRoute = 1f,
                optical = "Faena declarada rumbo a puerto.",
                anomalies = new Anomaly[0] },
        };

        public static ShipDef[] ShipsFor(int sector) => sector == 1 ? ShipsSector2 : sector == 2 ? ShipsSector3 : Ships;
        public static ShipDef TargetFor(int sector) => ShipsFor(sector)[0];
        public static string CaseTitleFor(int sector) => sector == 1 ? "Caso 2: Kestrel Explorer" : sector == 2 ? "Caso 3: Oceanic Crown 9" : "Caso 1: Hai Feng 77";
        public static string CaseSummaryFor(int sector) => sector == 1
            ? "Arrastrero industrial de 85 m con licencia falsificada y apagón de AIS en el Golfo de Guinea."
            : sector == 2
            ? "Palangrero de atún de 95 m que lleva 2 años sin tocar puerto bajo la red de Tidewater Holdings."
            : "Potero industrial de 90 m con señal AIS apagada fuera de la zona económica exclusiva.";
        public static int DouglasFor(int sector, float hours) => Mathf.Min(9, DouglasAt(hours) + (sector == 1 ? 1 : sector == 2 ? 2 : 0));

        /// Formatea en grados y minutos (DMS) las coordenadas reales del buque objetivo de cada sector.
        public static string FormatGps(int sector)
        {
            var target = TargetFor(sector);
            static string Dms(float v, char pos, char neg)
            {
                float abs = Mathf.Abs(v);
                int deg = Mathf.FloorToInt(abs);
                int min = Mathf.RoundToInt((abs - deg) * 60f);
                if (min >= 60) { deg++; min -= 60; }
                return $"{deg}°{min:00}'{(v >= 0f ? pos : neg)}";
            }
            return $"{Dms(target.lat, 'N', 'S')} {Dms(target.lon, 'E', 'O')}";
        }

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
        // Coordenadas en el espacio del buque.
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
                help = "Rodel deja caer una bandeja en el comedor: la tripulación se distrae unos segundos." },
            new PersonDef { id = "ari", name = "Ari Wibowo", origin = "Indonesia", voice = "Te mira a los ojos antes de contestar.", startTrust = 20, willTestify = true, testimonyEvidence = "t_ari",
                intro = "—No vengas a salvarme. Dime qué piensas hacer y yo decido.",
                plan = "—Si hay plan, quiero saberlo entero. Los demás me escuchan a mí.",
                testimony = "«Cuando alguien pedía parar, lo encerraban aquí. Nos decían que nadie nos buscaba.»",
                testimonySafe = "Describe amenazas y violencia física a bordo. Descripción omitida por tus ajustes.",
                help = "Ari golpea el mamparo a propósito: atrae al guardia más cercano lejos de ti." },
            new PersonDef { id = "moussa", name = "Moussa Diallo", origin = "Senegal", voice = "Se sostiene la mano herida, sin quejarse.", startTrust = 15, willTestify = true, testimonyEvidence = "t_moussa",
                intro = "—Me dijeron que descontarían el viaje de mi sueldo. Sigo debiendo. ¿Tú también vienes a cobrarme?",
                plan = "—Hay una puerta de servicio detrás de ese mamparo, va al pañol. Te la abro desde aquí.",
                testimony = "«Cada mes la deuda crecía. Comida, guantes, hasta el agua. Nunca vi un papel.»",
                testimonySafe = "Describe la deuda impuesta y el abuso de su situación.",
                help = "Moussa abre la puerta de servicio: tienes un atajo hasta el pañol." },
            new PersonDef { id = "thant", name = "Thant Zin", origin = "Myanmar", voice = "Habla despacio, eligiendo cada palabra.", startTrust = 25, willTestify = false,
                intro = "—Quiero irme a casa. Solo eso.",
                plan = "—Iré contigo. Pero no voy a contar nada a ningún juez. Tengo familia allí.",
                testimony = "", testimonySafe = "",
                help = "Thant golpea la escotilla con su ritmo de siempre: tus pasos no se oyen durante unos segundos." },
        };

        public static readonly PersonDef[] PeopleSector2 =
        {
            new PersonDef { id = "rodel", name = "Kwame Mensah", origin = "Ghana", voice = "Habla en voz baja mientras revisa las redes de arrastre.", startTrust = 30, startsFree = true, willTestify = true, testimonyEvidence = "t_rodel",
                intro = "—Apagaron el AIS antes de entrar en la reserva. ¿De verdad vienes a inspeccionar el Kestrel Explorer?",
                plan = "—La escotilla de babor tiene clave: 4-7-1-9. Si sacas a los de abajo, te cubro desde cubierta.",
                testimony = "«Pintaron una licencia falsa sobre el casco y nos obligaron a transbordar la pesca al Atlantic Reef.»",
                testimonySafe = "Describe la falsificación de licencia y los transbordos ilegales en el Golfo de Guinea.",
                help = "Kwame suelta un cabo metálico cerca del puente: la tripulación se distrae unos segundos." },
            new PersonDef { id = "ari", name = "Ibrahim Diop", origin = "Senegal", voice = "Mantiene la calma y observa cada movimiento.", startTrust = 20, willTestify = true, testimonyEvidence = "t_ari",
                intro = "—Nos prometieron faena costera con descanso. Llevamos meses encerrados en este arrastrero.",
                plan = "—Explícame la ruta de salida. Si es segura, todos saldrán detrás de ti.",
                testimony = "«Cuando preguntamos por la licencia de pesca, el oficial nos amenazó y nos encerró tras cada turno.»",
                testimonySafe = "Describe intimidación y encierro durante los apagones de AIS. Descripción omitida por tus ajustes.",
                help = "Ibrahim golpea una tubería del mamparo: atrae al guardia más cercano lejos del pasillo." },
            new PersonDef { id = "moussa", name = "Samuel Osei", origin = "Ghana", voice = "Aprieta su libreta de jornales con la mano cansada.", startTrust = 15, willTestify = true, testimonyEvidence = "t_moussa",
                intro = "—La agencia en Tema me cobró el embarque tres veces. ¿Tú también vienes de parte del armador?",
                plan = "—Detrás de este mamparo está la puerta de servicio al pañol. Te la abro desde adentro.",
                testimony = "«Descuentan cada comida y cada par de guantes; trabajamos sin ver un solo pago.»",
                testimonySafe = "Describe servidumbre por deudas impuesta por la reclutadora en África Occidental.",
                help = "Samuel abre la puerta de servicio: tienes un atajo hasta el pañol." },
            new PersonDef { id = "thant", name = "Jean-Luc Koffi", origin = "Costa de Marfil", voice = "Responde con pocas palabras, mirando hacia la puerta.", startTrust = 25, willTestify = false,
                intro = "—Solo quiero volver a Abiyán con los míos.",
                plan = "—Saldré contigo, pero no firmaré ninguna declaración ante un tribunal.",
                testimony = "", testimonySafe = "",
                help = "Jean-Luc golpea la escotilla con ritmo constante: tapa tus pasos durante unos segundos." },
        };

        public static readonly PersonDef[] PeopleSector3 =
        {
            new PersonDef { id = "rodel", name = "Budi Santoso", origin = "Indonesia", voice = "Habla con voz ronca tras dos años en altamar.", startTrust = 30, startsFree = true, willTestify = true, testimonyEvidence = "t_rodel",
                intro = "—Dos años sin pisar tierra en el Oceanic Crown 9. Creí que nadie llegaba tan lejos en el Índico.",
                plan = "—La clave de la escotilla de babor es 4-7-1-9. Úsala antes de que vuelva el contramaestre.",
                testimony = "«El buque nodriza Mombasa Reefer trae combustible y se lleva el atún; a nosotros nunca nos dejan bajar.»",
                testimonySafe = "Describe dos años de aislamiento continuo asistido por buque nodriza en el Océano Índico.",
                help = "Budi provoca un ruido seco en el comedor: la tripulación se distrae unos segundos." },
            new PersonDef { id = "ari", name = "Aung Kyaw", origin = "Myanmar", voice = "Te mira fijamente, agotado pero firme.", startTrust = 20, willTestify = true, testimonyEvidence = "t_ari",
                intro = "—Si vienes contra Tidewater Holdings, dime la verdad entera antes de movernos.",
                plan = "—Cuando confirmes la salida, convenceré al resto de seguirte en silencio.",
                testimony = "«El capitán recibe órdenes directas de Tidewater. Quien se niega a hacer doble turno queda castigado aquí abajo.»",
                testimonySafe = "Describe represalias y coerción en la flota de Tidewater Holdings. Descripción omitida por tus ajustes.",
                help = "Aung golpea el mamparo metálico: atrae al guardia más cercano lejos de tu ruta." },
            new PersonDef { id = "moussa", name = "Mateo Cruz", origin = "Filipinas", voice = "Guarda una copia arrugada de su contrato.", startTrust = 15, willTestify = true, testimonyEvidence = "t_moussa",
                intro = "—Firmé por un año y ya van dos. Dicen que debo el pasaje de regreso.",
                plan = "—Conozco la puerta de servicio que da al pañol. Te la destrabo ahora.",
                testimony = "«Retienen nuestros pasaportes en el puente y descuentan cualquier medicamento de un sueldo que nunca llega.»",
                testimonySafe = "Describe retención de pasaportes y servidumbre por deudas en el Océano Índico.",
                help = "Mateo abre la puerta de servicio: tienes un atajo hasta el pañol." },
            new PersonDef { id = "thant", name = "Hassan Juma", origin = "Tanzania", voice = "Habla en voz baja y pausada.", startTrust = 25, willTestify = false,
                intro = "—Solo quiero regresar a Zanzíbar y ver a mi familia.",
                plan = "—Me iré contigo si es seguro, pero no voy a testificar contra Tidewater.",
                testimony = "", testimonySafe = "",
                help = "Hassan golpea la escotilla en el momento justo: tus pasos quedan cubiertos unos segundos." },
        };

        public static PersonDef[] PeopleFor(int sector) => sector == 1 ? PeopleSector2 : sector == 2 ? PeopleSector3 : People;
        public static PersonDef PersonDefOf(string id) => System.Array.Find(PeopleFor(Game.S?.c?.sector ?? 0), p => p.id == id);

        /// Lo que fue de cada persona: se cuenta desde lo que ocurrió de verdad en esta partida.
        public static string Epilogue(PersonDef d, PersonState st, int verdict)
        {
            int dignified = 0;
            foreach (var x in st.dignified) if (x) dignified++;
            bool testified = st.testimony == 1;
            var t = new List<string>();
            switch (d.id)
            {
                case "rodel":
                    t.Add(st.dignified[1] ? $"{d.name} volvió a {d.origin} con todos sus documentos." : $"{d.name} volvió a {d.origin} con papeles provisionales.");
                    t.Add(st.dignified[3] ? "Cobró el salario adeudado." : "Sigue reclamando sus salarios por su cuenta.");
                    if (testified) t.Add("Su testimonio quedó en el expediente.");
                    if (dignified >= 3) t.Add("Hoy forma a otros marineros sobre sus derechos.");
                    break;
                case "ari":
                    t.Add($"{d.name} regresó a {d.origin}.");
                    t.Add(!testified ? "Prefiere no hablar todavía de lo ocurrido, y esa decisión también es suya."
                        : verdict > 0 ? "Declaró ante el tribunal por decisión propia."
                        : "Dio su testimonio por decisión propia, aunque el caso no llegó a sentencia.");
                    if (dignified >= 3) t.Add("Hoy dirige una asociación de pescadores.");
                    break;
                case "moussa":
                    t.Add($"{d.name} volvió a {d.origin} con su familia.");
                    t.Add(st.dignified[0] ? "Recibió atención médica completa." : "Necesita un tratamiento médico que aún intenta costear.");
                    t.Add(verdict > 0 ? "La deuda quedó anulada por sentencia." : "Sin sentencia, su deuda sigue sin anularse en los papeles.");
                    break;
                case "thant":
                    t.Add($"{d.name} está de vuelta en {d.origin} con su familia.");
                    t.Add(st.testimony == 2 ? "Eligió no testificar, y el proceso respetó esa decisión." : "No llegaron a pedirle que testificara.");
                    t.Add(st.dignified[4] ? "Cuenta con apoyo psicosocial." : "Volvió sin apoyo psicosocial.");
                    break;
            }
            return string.Join(" ", t);
        }

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
            E("e_radio", "Radio VHF satelital", 'A', "Emisora de banda oculta sintonizada con Cormorán Trade sin acceso para la tripulación.", new[] { 3 }, new[] { "3-4" }),
            E("e_plotter", "Plotter GPS náutico", 'B', "Registro de derrota con 72 h de apagado de AIS y desvío deliberado lejos de puerto.", new[] { 1, 2 }, new[] { "0-1" }),
            E("t_rodel", "Testimonio de Rodel Santos", 'B', "Testimonio con consentimiento y lector biométrico.", new[] { 1, 3 }, new[] { "0-2" }),
            E("t_ari", "Testimonio de Ari Wibowo", 'A', "Testimonio con consentimiento y lector biométrico.", new[] { 4, 5 }, NoEdge, "Testimonio con consentimiento. Descripción omitida por tus ajustes."),
            E("t_moussa", "Testimonio de Moussa Diallo", 'A', "Testimonio con consentimiento y lector biométrico.", new[] { 0, 8 }, new[] { "0-2" }),
        };
        public static EvidenceDef Evidence(string id)
        {
            var e = System.Array.Find(EvidenceDefs, x => x.id == id);
            if (e != null && (id == "t_rodel" || id == "t_ari" || id == "t_moussa"))
            {
                var p = PersonDefOf(id.Substring(2));
                if (p != null)
                    return new EvidenceDef { id = e.id, title = $"Testimonio de {p.name}", cat = e.cat, desc = e.desc, descSafe = e.descSafe, indicators = e.indicators, edges = e.edges };
            }
            return e;
        }
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

        public static readonly NodeDef[] NodesSector2 =
        {
            new NodeDef { name = "Kestrel Explorer", role = "Arrastrero", pos = new Vector2(.12f, .5f) },
            new NodeDef { name = "Atlantic Reef", role = "Frigorífico", pos = new Vector2(.42f, .85f) },
            new NodeDef { name = "Agencia Costa Oeste", role = "Reclutadora", pos = new Vector2(.42f, .15f) },
            new NodeDef { name = "Guinea Horizon Ltd", role = "Armador registrado", pos = new Vector2(.4f, .5f) },
            new NodeDef { name = "Gold Coast Trading", role = "Empresa fachada", pos = new Vector2(.64f, .5f) },
            new NodeDef { name = "Gulf Maritime Holdings", role = "Beneficiario final", pos = new Vector2(.875f, .5f) },
        };

        public static readonly NodeDef[] NodesSector3 =
        {
            new NodeDef { name = "Oceanic Crown 9", role = "Palangrero", pos = new Vector2(.12f, .5f) },
            new NodeDef { name = "Mombasa Reefer", role = "Buque nodriza", pos = new Vector2(.42f, .85f) },
            new NodeDef { name = "Agencia Índico Sur", role = "Reclutadora", pos = new Vector2(.42f, .15f) },
            new NodeDef { name = "Mascarene Vessels", role = "Armador registrado", pos = new Vector2(.4f, .5f) },
            new NodeDef { name = "Tidewater Logistics", role = "Empresa fachada", pos = new Vector2(.64f, .5f) },
            new NodeDef { name = "Tidewater Holdings", role = "Beneficiario final", pos = new Vector2(.875f, .5f) },
        };

        public static NodeDef[] NodesFor(int sector) => sector == 1 ? NodesSector2 : sector == 2 ? NodesSector3 : Nodes;

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

        /// Cuántos indicadores se pueden probar todavía con las pruebas que no se han perdido.
        public static int MaxIndicators(List<string> lost)
        {
            var set = new HashSet<int>();
            foreach (var e in EvidenceDefs)
                if (!lost.Contains(e.id)) foreach (var i in e.indicators) set.Add(i);
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
            new SectorDef { name = "1. Atlántico Sur (milla 201)", scenario = "Flota de poteros al borde de la zona económica exclusiva argentina", ship = "Potero", novelty = "Tutorial, luces VIIRS y caso Hai Feng 77", cases = 1, nodes = "6", lat = -46, lon = -59.5f },
            new SectorDef { name = "2. Golfo de Guinea", scenario = "Zona crítica de apagones de AIS (Welch et al., 2022)", ship = "Arrastrero", novelty = "Apagones y transbordos frecuentes", cases = 3, nodes = "Unos 15", lat = 2, lon = 2 },
            new SectorDef { name = "3. Océano Índico Occidental", scenario = "Palangreros que pasan años sin tocar puerto", ship = "Palangrero y frigorífico", novelty = "Aislamiento extremo, perros guardianes y caso final contra Tidewater", cases = 3, nodes = "25 a 30", lat = -10, lon = 58 },
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
