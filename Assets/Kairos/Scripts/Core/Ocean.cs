using UnityEngine;

namespace Kairos
{
    /// El mar como sistema: la ola que se ve (Ocean.shader) es la misma que mueve la lancha y marca las ventanas.
    [ExecuteAlways]
    public class Ocean : MonoBehaviour
    {
        /// Escala Douglas 0-9. Cambia en transiciones lentas, ver DriftDouglas.
        public static float Douglas = 4f;
        static readonly float[] Heights = { .02f, .06f, .15f, .35f, .6f, 1f, 1.5f, 2.1f, 2.8f, 3.6f };
        // dirX·k, dirZ·k, omega, amplitud relativa. La primera componente es el oleaje de fondo que marca las ventanas.
        static readonly Vector4[] Waves = Build();
        static Vector4[] Build()
        {
            float[,] def = { { .8f, .6f, 60f, .55f }, { -.5f, .85f, 33f, .25f }, { .2f, -.98f, 17f, .13f }, { .95f, -.3f, 9f, .07f } };
            var w = new Vector4[4];
            for (int i = 0; i < 4; i++)
            {
                float k = 2 * Mathf.PI / def[i, 2];
                w[i] = new Vector4(def[i, 0] * k, def[i, 1] * k, Mathf.Sqrt(9.81f * k), def[i, 3]);
            }
            return w;
        }

        public static float Amp
        {
            get
            {
                float d = Mathf.Clamp(Douglas, 0, 8.999f);
                int i = (int)d;
                return Mathf.Lerp(Heights[i], Heights[i + 1], d - i);
            }
        }
        public static float T => Application.isPlaying ? Time.time : 3f;

        public static float Height(float x, float z)
        {
            float h = 0, t = T;
            foreach (var w in Waves) h += w.w * Mathf.Sin(w.x * x + w.y * z + w.z * t);
            return h * Amp;
        }

        public static Vector3 Normal(float x, float z)
        {
            const float e = .5f;
            return new Vector3(Height(x - e, z) - Height(x + e, z), 2 * e, Height(x, z - e) - Height(x, z + e)).normalized;
        }

        static float SwellPhase(float x, float z) => Waves[0].x * x + Waves[0].y * z + Waves[0].z * T;
        /// 1 en la cresta del oleaje de fondo, -1 en el seno.
        public static float Swell(float x, float z) => Mathf.Sin(SwellPhase(x, z));
        /// Segundos que faltan o sobran hasta la cresta más cercana.
        public static float SecondsFromCrest(float x, float z)
        {
            float d = Mathf.DeltaAngle(SwellPhase(x, z) * Mathf.Rad2Deg, 90f) * Mathf.Deg2Rad;
            return Mathf.Abs(d) / Waves[0].z;
        }
        public static float SecondsFromTrough(float x, float z)
        {
            float d = Mathf.DeltaAngle(SwellPhase(x, z) * Mathf.Rad2Deg, -90f) * Mathf.Deg2Rad;
            return Mathf.Abs(d) / Waves[0].z;
        }

        /// Fija el estado del mar, por ejemplo el que marca el reloj del caso.
        public static void SetSea(float douglas) { Douglas = douglas; driftTarget = -1; }

        // El mar cambia en transiciones de minutos: va y viene entre min y max.
        static float driftTarget = -1;
        public static void DriftDouglas(float min, float max, float minutesPerStep = 5f)
        {
            if (driftTarget < 0 || Mathf.Abs(Douglas - driftTarget) < .01f) driftTarget = Mathf.Abs(driftTarget - max) < .01f ? min : max;
            Douglas = Mathf.MoveTowards(Douglas, driftTarget, Time.deltaTime / (minutesPerStep * 60f));
        }

        public int rings = 170, segments = 128;
        public float douglasAtStart = 4f;
        public Color ambient = new Color(.22f, .28f, .34f);
        Mesh mesh;
        static readonly int WavesId = Shader.PropertyToID("_OceanWaves"), AmpId = Shader.PropertyToID("_OceanAmp"), TimeId = Shader.PropertyToID("_OceanT");

        void OnEnable()
        {
            Douglas = douglasAtStart;
            driftTarget = -1;
            // Sin lighting bakeado la sonda de ambiente queda negra: se fija aquí.
            // Más luz desde el cielo que desde abajo: da volumen a lo que no toca ninguna lámpara.
            var sh = new UnityEngine.Rendering.SphericalHarmonicsL2();
            sh.AddAmbientLight(ambient * .8f);
            sh.AddDirectionalLight(Vector3.up, new Color(.34f, .45f, .6f), .55f);
            RenderSettings.ambientProbe = sh;
            Shader.SetGlobalVector("_BoatPos", Vector4.zero);
            Shader.SetGlobalFloat("_RainDim", 0);
            mesh = RadialGrid(rings, segments);
            var mf = GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = mesh;
            Push();
        }

        void Update()
        {
            Push();
            var cam = Camera.main;
            if (cam != null) transform.position = new Vector3(cam.transform.position.x, 0, cam.transform.position.z);
        }

        static void Push()
        {
            Shader.SetGlobalVectorArray(WavesId, Waves);
            Shader.SetGlobalFloat(AmpId, Amp);
            Shader.SetGlobalFloat(TimeId, T);
        }

        /// Malla radial alrededor de la cámara: mucho detalle cerca, llega al horizonte con pocos vértices.
        static Mesh RadialGrid(int rings, int segs)
        {
            var v = new Vector3[rings * segs + 1];
            var idx = new int[(rings - 1) * segs * 6 + segs * 3];
            v[0] = Vector3.zero;
            float r = 1.5f;
            for (int i = 0; i < rings; i++)
            {
                for (int s = 0; s < segs; s++)
                {
                    float a = s * 2 * Mathf.PI / segs;
                    v[1 + i * segs + s] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                }
                r *= 1.043f;
            }
            int n = 0;
            for (int s = 0; s < segs; s++) { idx[n++] = 0; idx[n++] = 1 + (s + 1) % segs; idx[n++] = 1 + s; }
            for (int i = 0; i < rings - 1; i++)
                for (int s = 0; s < segs; s++)
                {
                    int a = 1 + i * segs + s, b = 1 + i * segs + (s + 1) % segs, c = a + segs, d = b + segs;
                    idx[n++] = a; idx[n++] = b; idx[n++] = c;
                    idx[n++] = b; idx[n++] = d; idx[n++] = c;
                }
            var m = new Mesh { name = "OceanGrid", hideFlags = HideFlags.DontSave };
            m.vertices = v; m.triangles = idx;
            m.bounds = new Bounds(Vector3.zero, new Vector3(6000, 40, 6000));
            return m;
        }
    }
}
