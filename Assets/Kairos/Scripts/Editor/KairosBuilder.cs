using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Kairos.EditorTools
{
    /// Genera materiales, el buque Hai Feng 77 y las 5 escenas. Menú: Kairos > Generar juego.
    /// Coordenadas del buque: proa +Z, popa -Z, babor -X. Cubierta principal y=6, cubierta inferior y=3.
    public static partial class KairosBuilder
    {
        const string Root = "Assets/Kairos", ScenesDir = Root + "/Scenes", GenDir = Root + "/Generated";

        // ---------- Mallas: cajas con UV a escala real, combinadas por material ----------
        class MeshAcc
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, float uvScale = .33f)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                for (int k = 0; k < 4; k++) n.Add(normal);
                float w = (b - a).magnitude * uvScale, h = (d - a).magnitude * uvScale;
                var o = new Vector2(a.x + a.y * .37f, a.z + a.y * .61f) * uvScale;
                uv.Add(o); uv.Add(o + new Vector2(w, 0)); uv.Add(o + new Vector2(w, h)); uv.Add(o + new Vector2(0, h));
                t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
            }
            public void Box(Vector3 c, Vector3 s, Quaternion r)
            {
                Vector3 P(float x, float y, float z) => c + r * new Vector3(x * s.x * .5f, y * s.y * .5f, z * s.z * .5f);
                Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), r * Vector3.forward);
                Quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), r * Vector3.back);
                Quad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), r * Vector3.right);
                Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), r * Vector3.left);
                Quad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1), r * Vector3.up);
                Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), r * Vector3.down);
            }
            /// Tubo entre dos puntos, con tapas y normales suaves.
            public void Cylinder(Vector3 a, Vector3 b, float r, int segs)
            {
                var dir = (b - a).normalized;
                var x = Vector3.Cross(dir, Mathf.Abs(dir.y) > .9f ? Vector3.right : Vector3.up).normalized;
                var y = Vector3.Cross(dir, x);
                float len = (b - a).magnitude;
                int start = v.Count;
                for (int i = 0; i <= segs; i++)
                {
                    float ang = i * Mathf.PI * 2 / segs;
                    var radial = x * Mathf.Cos(ang) + y * Mathf.Sin(ang);
                    float u = i * Mathf.PI * 2 * r * .33f / segs;
                    v.Add(a + radial * r); n.Add(radial); uv.Add(new Vector2(u, 0));
                    v.Add(b + radial * r); n.Add(radial); uv.Add(new Vector2(u, len * .33f));
                }
                for (int i = 0; i < segs; i++)
                {
                    int s = start + i * 2;
                    t.Add(s); t.Add(s + 2); t.Add(s + 1);
                    t.Add(s + 1); t.Add(s + 2); t.Add(s + 3);
                }
                Cap(a, -dir, x, y, r, segs);
                Cap(b, dir, x, y, r, segs);
            }

            void Cap(Vector3 c, Vector3 normal, Vector3 x, Vector3 y, float r, int segs)
            {
                int center = v.Count;
                v.Add(c); n.Add(normal); uv.Add(Vector2.zero);
                for (int i = 0; i <= segs; i++)
                {
                    float ang = i * Mathf.PI * 2 / segs;
                    v.Add(c + (x * Mathf.Cos(ang) + y * Mathf.Sin(ang)) * r); n.Add(normal); uv.Add(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r * .33f);
                }
                bool flip = Vector3.Dot(Vector3.Cross(x, y), normal) > 0;
                for (int i = 0; i < segs; i++)
                {
                    t.Add(center);
                    t.Add(center + (flip ? i + 1 : i + 2));
                    t.Add(center + (flip ? i + 2 : i + 1));
                }
            }

            public void Ellipsoid(Vector3 c, Vector3 radii, int rows, int cols)
            {
                int start = v.Count;
                for (int j = 0; j <= rows; j++)
                {
                    float la = Mathf.PI * j / rows - Mathf.PI / 2;
                    for (int i = 0; i <= cols; i++)
                    {
                        float lo = Mathf.PI * 2 * i / cols;
                        var d = new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo));
                        v.Add(c + Vector3.Scale(d, radii));
                        n.Add(new Vector3(d.x / radii.x, d.y / radii.y, d.z / radii.z).normalized);
                        uv.Add(new Vector2(lo * radii.x * .33f, la * radii.y * .33f));
                    }
                }
                for (int j = 0; j < rows; j++)
                    for (int i = 0; i < cols; i++)
                    {
                        int a = start + j * (cols + 1) + i, b = a + 1, d = a + cols + 1, e = d + 1;
                        t.Add(a); t.Add(d); t.Add(b);
                        t.Add(b); t.Add(d); t.Add(e);
                    }
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
                m.RecalculateTangents(); // el relieve de las texturas los necesita
                return m;
            }
        }

        // ---------- Materiales ----------
        static Material steel, rust, deck, paint, floor, crate, frozen, skinA, skinB, skinC, skinD, coat, suit, worker, bulb, cyanGlow, darkMetal, windowMat;

        /// Con normal, el canal alfa de la textura lleva la lisura: lo oxidado es mate y lo mojado brilla.
        static Material Lit(string name, Color c, float smooth = .3f, float metal = .5f, Texture2D tex = null, Color? emission = null, Texture2D normal = null)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (emission != null)
            {
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetKeyword(new LocalKeyword(m.shader, "_EMISSION"), true);
                m.SetColor("_EmissionColor", emission.Value);
            }
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetKeyword(new LocalKeyword(m.shader, "_NORMALMAP"), true);
                m.SetFloat("_SmoothnessTextureChannel", 1);
                m.SetKeyword(new LocalKeyword(m.shader, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A"), true);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- Casco ----------
        static float Beam(float z) => z < 15f ? 7f : 7f * Mathf.Sqrt(Mathf.Max(0, 1f - Mathf.Pow((z - 15f) / 30f, 2)));

        static float[] Stations()
        {
            var s = new List<float>();
            for (float z = -45; z <= 45.01f; z += 3f) s.Add(z);
            return s.ToArray();
        }

        static Mesh HullMesh()
        {
            float[] levels = { 6.6f, 3f, 1.1f, -1f, -3.5f, -5f };
            float[] width = { 1f, .99f, .95f, .86f, .65f, .3f };
            var st = Stations();
            var verts = new List<Vector3>(); var uv = new List<Vector2>(); var subPaint = new List<int>(); var subRust = new List<int>();
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < st.Length - 1; i++)
                    for (int l = 0; l < levels.Length - 1; l++)
                    {
                        Vector3 P(int si, int lv) => new Vector3(side * Beam(st[si]) * width[lv], levels[lv], st[si]);
                        int b = verts.Count;
                        verts.Add(P(i, l)); verts.Add(P(i + 1, l)); verts.Add(P(i + 1, l + 1)); verts.Add(P(i, l + 1));
                        float u0 = st[i] * .2f, u1 = st[i + 1] * .2f, v0 = levels[l] * .2f, v1 = levels[l + 1] * .2f;
                        uv.Add(new Vector2(u0, v0)); uv.Add(new Vector2(u1, v0)); uv.Add(new Vector2(u1, v1)); uv.Add(new Vector2(u0, v1));
                        var tgt = l >= 2 ? subPaint : subRust; // bajo la línea de flotación, pintura de casco
                        if (side > 0) tgt.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                        else tgt.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                    }
            // Espejo de popa
            for (int l = 0; l < levels.Length - 1; l++)
            {
                int b = verts.Count;
                float w0 = 7 * width[l], w1 = 7 * width[l + 1];
                verts.Add(new Vector3(-w0, levels[l], -45)); verts.Add(new Vector3(w0, levels[l], -45));
                verts.Add(new Vector3(w1, levels[l + 1], -45)); verts.Add(new Vector3(-w1, levels[l + 1], -45));
                uv.Add(new Vector2(-w0 * .2f, levels[l] * .2f)); uv.Add(new Vector2(w0 * .2f, levels[l] * .2f));
                uv.Add(new Vector2(w1 * .2f, levels[l + 1] * .2f)); uv.Add(new Vector2(-w1 * .2f, levels[l + 1] * .2f));
                (l >= 2 ? subPaint : subRust).AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var m = new Mesh { name = "HaiFeng_Hull", indexFormat = IndexFormat.UInt32, subMeshCount = 2 };
            m.SetVertices(verts); m.SetUVs(0, uv);
            m.SetTriangles(subRust, 0); m.SetTriangles(subPaint, 1);
            m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
            return m;
        }

        static Mesh DeckMesh()
        {
            var acc = new MeshAcc();
            var st = Stations();
            for (int i = 0; i < st.Length - 1; i++)
            {
                float z0 = st[i], z1 = st[i + 1], b0 = Beam(z0), b1 = Beam(z1);
                bool hole = (z0 >= -36 && z1 <= -30) || (z0 >= 21 && z1 <= 27);
                float c0 = Mathf.Min(1.25f, b0), c1 = Mathf.Min(1.25f, b1);
                void Strip(float xa0, float xb0, float xa1, float xb1) =>
                    acc.Quad(new Vector3(xa0, 6, z0), new Vector3(xb0, 6, z0), new Vector3(xb1, 6, z1), new Vector3(xa1, 6, z1), Vector3.up);
                Strip(-b0, -c0, -b1, -c1);
                if (!hole) Strip(-c0, c0, -c1, c1);
                Strip(c0, b0, c1, b1);
            }
            return acc.ToMesh("HaiFeng_Deck");
        }

        // ---------- Constructor del buque ----------
        class ShipBuild
        {
            public GameObject root;
            public Transform colliders;
            public readonly Dictionary<Material, MeshAcc> acc = new Dictionary<Material, MeshAcc>();

            public void Box(Material m, Vector3 c, Vector3 s, Vector3? euler = null, bool collide = true)
            {
                var r = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
                if (!acc.TryGetValue(m, out var a)) acc[m] = a = new MeshAcc();
                a.Box(c, s, r);
                if (!collide) return;
                var go = new GameObject("c") { layer = Layers.ShipSolid };
                go.transform.SetParent(colliders, false);
                go.transform.SetLocalPositionAndRotation(c, r);
                go.AddComponent<BoxCollider>().size = s;
            }

            MeshAcc Of(Material m)
            {
                if (!acc.TryGetValue(m, out var a)) acc[m] = a = new MeshAcc();
                return a;
            }
            public void Pipe(Material m, Vector3 a, Vector3 b, float r, int segs = 8) => Of(m).Cylinder(a, b, r, segs);
            public void Ellipsoid(Material m, Vector3 c, Vector3 radii, int rows = 8, int cols = 12) => Of(m).Ellipsoid(c, radii, rows, cols);

            public void Solid(Vector3 c, Vector3 s, Vector3? euler = null)
            {
                var go = new GameObject("c") { layer = Layers.ShipSolid };
                go.transform.SetParent(colliders, false);
                go.transform.SetLocalPositionAndRotation(c, euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity);
                go.AddComponent<BoxCollider>().size = s;
            }

            /// Mamparo a lo largo de Z, a una x fija, con huecos de puerta (centro z, ancho 1.4, alto 2.2).
            public void WallZ(Material m, float x, float z0, float z1, float y0, float y1, params float[] doors)
            {
                float cur = z0;
                System.Array.Sort(doors);
                foreach (var d in doors)
                {
                    Box(m, new Vector3(x, (y0 + y1) / 2, (cur + d - .7f) / 2), new Vector3(.2f, y1 - y0, d - .7f - cur));
                    Box(m, new Vector3(x, (y0 + 2.2f + y1) / 2, d), new Vector3(.2f, y1 - y0 - 2.2f, 1.4f));
                    cur = d + .7f;
                }
                Box(m, new Vector3(x, (y0 + y1) / 2, (cur + z1) / 2), new Vector3(.2f, y1 - y0, z1 - cur));
            }

            public void WallX(Material m, float z, float x0, float x1, float y0, float y1, params float[] doors)
            {
                float cur = x0;
                System.Array.Sort(doors);
                foreach (var d in doors)
                {
                    Box(m, new Vector3((cur + d - .7f) / 2, (y0 + y1) / 2, z), new Vector3(d - .7f - cur, y1 - y0, .2f));
                    Box(m, new Vector3(d, (y0 + 2.2f + y1) / 2, z), new Vector3(1.4f, y1 - y0 - 2.2f, .2f));
                    cur = d + .7f;
                }
                Box(m, new Vector3((cur + x1) / 2, (y0 + y1) / 2, z), new Vector3(x1 - cur, y1 - y0, .2f));
            }

            public void Lamp(string name, Vector3 p, Color c, float range, float intensity, bool shadows = false)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = p;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity * 2.5f;
                l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
                go.AddComponent<UniversalAdditionalLightData>();
                Box(bulb, p + Vector3.up * .05f, new Vector3(.35f, .1f, .35f), null, false);
                Box(darkMetal, p + Vector3.up * .15f, new Vector3(.5f, .1f, .5f), null, false); // pantalla de la lámpara
            }

            public void Spot(string name, Vector3 p, Vector3 euler, Color c, float range, float angle, float intensity)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                go.transform.SetLocalPositionAndRotation(p, Quaternion.Euler(euler));
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot; l.color = c; l.range = range; l.spotAngle = angle; l.intensity = intensity;
                l.shadows = LightShadows.Soft;
                go.AddComponent<UniversalAdditionalLightData>();
            }
        }

        static GameObject BuildShip()
        {
            var b = new ShipBuild { root = new GameObject("HaiFeng77") };
            b.colliders = new GameObject("Colliders").transform;
            b.colliders.SetParent(b.root.transform, false);
            var amber = Pal.DeckLight;

            // --- Casco y cubierta ---
            var hullGo = new GameObject("Hull", typeof(MeshFilter), typeof(MeshRenderer)) { layer = Layers.HullShell };
            hullGo.transform.SetParent(b.root.transform, false);
            var hm = HullMesh();
            AssetDatabase.CreateAsset(hm, $"{GenDir}/HaiFeng_Hull.asset");
            hullGo.GetComponent<MeshFilter>().sharedMesh = hm;
            hullGo.GetComponent<MeshRenderer>().sharedMaterials = new[] { rust, paint };
            hullGo.AddComponent<MeshCollider>().sharedMesh = hm;

            var deckGo = new GameObject("Deck", typeof(MeshFilter), typeof(MeshRenderer)) { layer = Layers.ShipSolid };
            deckGo.transform.SetParent(b.root.transform, false);
            var dm = DeckMesh();
            AssetDatabase.CreateAsset(dm, $"{GenDir}/HaiFeng_Deck.asset");
            deckGo.GetComponent<MeshFilter>().sharedMesh = dm;
            deckGo.GetComponent<MeshRenderer>().sharedMaterial = deck;
            deckGo.AddComponent<MeshCollider>().sharedMesh = dm;

            // Borda: tramos siguiendo la curva del casco.
            var st = Stations();
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < st.Length - 1; i++)
                {
                    var a = new Vector3(side * Beam(st[i]), 6, st[i]); var c = new Vector3(side * Beam(st[i + 1]), 6, st[i + 1]);
                    var d = c - a;
                    var yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    b.Box(rust, (a + c) / 2 + Vector3.up * .6f, new Vector3(.18f, 1.2f, d.magnitude + .1f), new Vector3(0, yaw, 0));
                }
            b.Box(rust, new Vector3(0, 6.6f, -45), new Vector3(14, 1.2f, .18f));

            // --- Cubierta inferior ---
            float y0 = 3f, y1 = 5.95f;
            b.Box(floor, new Vector3(0, 2.9f, -4.5f), new Vector3(13.8f, .2f, 51f));
            b.Box(darkMetal, new Vector3(0, 5.97f, -4.5f), new Vector3(13.8f, .06f, 51f));

            // Pasillo central y mamparos
            b.WallZ(steel, 1.25f, -30, 21, y0, y1, -21, -6, 12);
            b.WallZ(steel, -1.25f, -30, 21, y0, y1, -20, -10, 2, 16);
            b.WallZ(steel, 6.8f, -30, 21, y0, y1);
            b.WallZ(steel, -6.8f, -30, 21, y0, y1);
            foreach (var z in new[] { -28f, -14f, 4f, 21f }) b.WallX(steel, z, 1.25f, 6.8f, y0, y1);
            foreach (var z in new[] { -28f, -14f, 10f, 21f }) b.WallX(steel, z, -6.8f, -1.25f, y0, y1);
            b.WallX(steel, -6f, -6.8f, -1.25f, y0, y1, -4f); // puerta de servicio K4 <-> pañol
            b.WallX(steel, -30f, 1.25f, 6.8f, y0, y1);
            b.WallX(steel, -30f, -6.8f, -1.25f, y0, y1);

            // Rampas de bajada con sus paredes laterales
            float ramp = Mathf.Atan2(3f, 6f) * Mathf.Rad2Deg, rampLen = Mathf.Sqrt(45f);
            b.Box(floor, new Vector3(0, 4.5f, -33), new Vector3(2.5f, .18f, rampLen), new Vector3(ramp, 0, 0));
            b.Box(floor, new Vector3(0, 4.5f, 24), new Vector3(2.5f, .18f, rampLen), new Vector3(-ramp, 0, 0));
            foreach (var zc in new[] { -33f, 24f })
                foreach (var x in new[] { -1.3f, 1.3f })
                    b.Box(steel, new Vector3(x, 5.0f, zc), new Vector3(.15f, 3.9f, 6f));

            // Máquinas: K1, compresores, zona segura para la cortadora
            b.Box(darkMetal, new Vector3(4.3f, 4.1f, -24.5f), new Vector3(3f, 2.2f, 3.5f));
            b.Box(darkMetal, new Vector3(4.3f, 4.1f, -17.5f), new Vector3(3f, 2.2f, 3.5f));
            b.Box(steel, new Vector3(5.8f, 5.2f, -21f), new Vector3(.3f, .3f, 12f), null, false);
            b.Box(cyanGlow, new Vector3(3.1f, 4.2f, -22.65f), new Vector3(.4f, .2f, .05f), null, false);
            b.Lamp("Luz Maquinas", new Vector3(3.2f, 5.7f, -21), new Color(.8f, .9f, .75f), 12, 3.5f);

            // Bodegas frías: K2, K3, K5
            void Hold(float x, float zMin, float zMax, float doorSide)
            {
                for (float z = zMin + 1.5f; z < zMax - 1f; z += 2.4f)
                    for (int row = 0; row < 2; row++)
                    {
                        float cx = x + doorSide * (row == 0 ? 1.7f : .0f);
                        float h = 1.2f + ((int)(z * 7 + row * 3) % 3) * .6f;
                        if (row == 1 && Mathf.Abs(z - (zMin + zMax) / 2) < 2f) continue;
                        b.Box(frozen, new Vector3(cx, y0 + h / 2, z), new Vector3(1.7f, h, 2f));
                    }
            }
            Hold(4.6f, -13, 3, 1f);
            Hold(4.6f, 5, 20, 1f);
            Hold(-4.6f, 11, 20, -1f);
            b.Lamp("Luz Bodega A", new Vector3(3f, 5.7f, -5), new Color(.55f, .75f, 1f), 11, 2.5f);
            b.Lamp("Luz Bodega B", new Vector3(3f, 5.7f, 12), new Color(.55f, .75f, 1f), 11, 2.5f);
            b.Lamp("Luz Bodega C", new Vector3(-3f, 5.7f, 16), new Color(.55f, .75f, 1f), 9, 2.5f);

            // K4: compartimento oculto. Se entiende por las huellas: literas soldadas, cubos, poca luz.
            foreach (var z in new[] { -4f, -1f, 2f, 5f, 8f })
            {
                b.Box(darkMetal, new Vector3(-5.9f, 3.6f, z), new Vector3(1.6f, .12f, 2.2f));
                b.Box(darkMetal, new Vector3(-5.9f, 4.8f, z), new Vector3(1.6f, .12f, 2.2f));
            }
            b.Box(crate, new Vector3(-2.2f, 3.3f, 8.5f), new Vector3(.5f, .6f, .5f));
            b.Lamp("Luz K4", new Vector3(-3.8f, 5.7f, 2), new Color(1f, .75f, .45f), 9, 1.6f);

            // Pañol y rancho inferior
            b.Box(crate, new Vector3(-5.4f, 3.5f, -11f), new Vector3(2f, 1f, 3.5f));
            b.Box(crate, new Vector3(-5.4f, 3.7f, -21f), new Vector3(2f, 1.4f, 5f));
            b.Lamp("Luz Rancho", new Vector3(-3.5f, 5.7f, -21), new Color(.9f, .85f, .7f), 9, 2f);
            b.Lamp("Luz Pasillo 1", new Vector3(0, 5.7f, -18), new Color(.95f, .85f, .65f), 9, 2f);
            b.Lamp("Luz Pasillo 2", new Vector3(0, 5.7f, 4), new Color(.95f, .85f, .65f), 9, 2f);
            b.Lamp("Luz Pasillo 3", new Vector3(0, 5.7f, 17), new Color(.95f, .85f, .65f), 8, 1.6f);

            // --- Habilitación (y 6-9) ---
            float hy0 = 6f, hy1 = 9f;
            b.WallX(super, -20f, -5, 5, hy0, hy1, 0);
            b.WallX(super, 2f, -5, 5, hy0, hy1, 0);
            b.WallZ(super, 5f, -20, 2, hy0, hy1);
            b.WallZ(super, -5f, -20, 2, hy0, hy1);
            b.WallZ(steel, 1.25f, -20, 2, hy0, hy1, -16, -7);
            b.WallZ(steel, -1.25f, -20, 2, hy0, hy1, -16, -7);
            b.WallX(steel, -12f, 1.25f, 5, hy0, hy1);
            b.WallX(steel, -12f, -5, -1.25f, hy0, hy1);
            b.Box(super, new Vector3(0, 9.05f, -9), new Vector3(10.2f, .12f, 22.2f));  // techo
            b.Box(super, new Vector3(6.05f, 9.05f, -3f), new Vector3(2.1f, .12f, 10.2f)); // rellano al final de la rampa
            b.Box(darkMetal, new Vector3(0, 8.96f, -9), new Vector3(10, .04f, 22), null, false);

            // Mobiliario de habilitación
            b.Box(crate, new Vector3(-3.5f, 6.45f, -18.8f), new Vector3(2.4f, .9f, 1f));   // camarote capitán
            b.Box(darkMetal, new Vector3(-4.4f, 6.8f, -14f), new Vector3(.8f, 1.6f, 1f));  // caja fuerte
            b.Box(crate, new Vector3(3.5f, 6.45f, -18.8f), new Vector3(2.4f, .9f, 1f));    // camarote oficial
            b.Box(crate, new Vector3(-3.4f, 6.4f, -5f), new Vector3(1.6f, .8f, 5f));       // comedor: deja paso junto a la puerta
            foreach (var z in new[] { -10f, -6.5f, -3f, .5f }) b.Box(darkMetal, new Vector3(4.2f, 6.9f, z), new Vector3(1.3f, 1.8f, 2.2f)); // literas
            b.Lamp("Luz Hab 1", new Vector3(0, 8.8f, -15), new Color(.95f, .85f, .65f), 9, 2.2f);
            b.Lamp("Luz Hab 2", new Vector3(0, 8.8f, -4), new Color(.95f, .85f, .65f), 9, 2.2f);
            b.Lamp("Luz Capitan", new Vector3(-3, 8.8f, -16), new Color(1f, .8f, .55f), 7, 1.6f);
            b.Lamp("Luz Oficial", new Vector3(3, 8.8f, -16), new Color(1f, .8f, .55f), 7, 1.6f);
            b.Lamp("Luz Comedor", new Vector3(-3, 8.8f, -5), new Color(1f, .8f, .55f), 8, 1.8f);
            b.Lamp("Luz Literas", new Vector3(3, 8.8f, -5), new Color(1f, .8f, .55f), 8, 1.4f);

            // Rampa exterior al puente, por estribor
            float bridgeRamp = Mathf.Atan2(3f, 6f) * Mathf.Rad2Deg;
            b.Box(floor, new Vector3(6f, 7.5f, -11f), new Vector3(1.9f, .18f, rampLen), new Vector3(-bridgeRamp, 0, 0));
            b.Solid(new Vector3(6f, 7f, -7.5f), new Vector3(2f, 2f, 1f));
            b.Box(super, new Vector3(1, 9.6f, 2.05f), new Vector3(12.2f, 1f, .15f));
            b.Box(super, new Vector3(0, 9.6f, -20.05f), new Vector3(10.2f, 1f, .15f));
            b.Box(super, new Vector3(-5.05f, 9.6f, -9), new Vector3(.15f, 1f, 22.2f));
            b.Box(super, new Vector3(7.05f, 9.6f, -3f), new Vector3(.15f, 1f, 10f));
            b.Box(super, new Vector3(5.05f, 9.6f, -14f), new Vector3(.15f, 1f, 12f));   // separa la rampa del techo
            b.Box(super, new Vector3(7.05f, 8.3f, -11f), new Vector3(.15f, 3.8f, 6.2f)); // barandilla de la rampa

            // --- Puente (y 9-12) ---
            float by0 = 9.1f, by1 = 12f;
            b.WallZ(super, 4f, -14, -4, by0, by1);
            b.WallZ(super, -4f, -14, -4, by0, by1);
            b.WallX(super, -14f, -4, 4, by0, by1);
            b.WallX(super, -4f, -4, 4, by0, by1, 3f); // puerta a proa, desde el rellano
            b.Box(windowMat, new Vector3(-1f, 11f, -3.85f), new Vector3(5.5f, 1.1f, .1f), null, false);
            b.Box(windowMat, new Vector3(-3.85f, 11f, -9f), new Vector3(.1f, 1.1f, 8f), null, false);
            b.Box(super, new Vector3(0, 12.05f, -9), new Vector3(8.4f, .12f, 10.4f));
            b.Box(darkMetal, new Vector3(0, 9.6f, -5f), new Vector3(5f, 1f, .9f));            // consola
            b.Box(cyanGlow, new Vector3(0, 10.12f, -5.1f), new Vector3(4.6f, .04f, .6f), null, false);
            b.Box(crate, new Vector3(-2.6f, 9.55f, -11.8f), new Vector3(1.6f, .9f, 1.2f));    // mesa de cartas
            b.Lamp("Luz Puente", new Vector3(0, 11.8f, -9), new Color(.5f, .9f, .85f), 9, 1.6f);
            b.Box(darkMetal, new Vector3(0, 13.8f, -9), new Vector3(.3f, 3.5f, .3f), null, false);   // mástil
            b.Box(darkMetal, new Vector3(0, 15f, -9), new Vector3(2.6f, .15f, .15f), null, false);

            // --- Cubierta exterior ---
            // Cubierta de popa: cajas para esconderse, chigre y redes.
            b.Box(crate, new Vector3(-4.6f, 6.6f, -39), new Vector3(2.6f, 1.2f, 3));
            b.Box(crate, new Vector3(4.6f, 6.8f, -34), new Vector3(2.4f, 1.6f, 2.4f));
            b.Box(crate, new Vector3(-4.2f, 6.5f, -27), new Vector3(2, 1f, 2.4f));
            b.Box(darkMetal, new Vector3(0, 6.8f, -41), new Vector3(3, 1.6f, 1.6f));
            b.Box(crate, new Vector3(4.8f, 6.5f, -25), new Vector3(2f, 1f, 3f));

            // Proa: tapas de bodega y racks de luces de potero.
            b.Box(rust, new Vector3(-3.6f, 6.45f, 10), new Vector3(3.6f, .9f, 6));
            b.Box(rust, new Vector3(3.6f, 6.45f, 16), new Vector3(3.6f, .9f, 6));
            b.Box(crate, new Vector3(-3.8f, 6.7f, 19), new Vector3(2.2f, 1.4f, 2.4f));
            b.Box(crate, new Vector3(3.8f, 6.7f, 6.5f), new Vector3(2.2f, 1.4f, 2.4f));
            b.Box(crate, new Vector3(3f, 6.6f, 31), new Vector3(2.2f, 1.2f, 2.2f));
            b.Box(crate, new Vector3(-2.5f, 6.6f, 34), new Vector3(2f, 1.2f, 2f));
            for (float z = -18; z <= 36; z += 9)
            {
                if (z < 3) continue;
                b.Box(darkMetal, new Vector3(0, 8.2f, z), new Vector3(.22f, 4.4f, .22f), null, false);
                b.Box(darkMetal, new Vector3(0, 10.3f, z), new Vector3(Mathf.Min(9, Beam(z) * 1.6f), .14f, .14f), null, false);
                for (float x = -3.6f; x <= 3.61f; x += 1.2f)
                    if (Mathf.Abs(x) < Beam(z) * .78f) b.Box(bulb, new Vector3(x, 10.15f, z), new Vector3(.3f, .18f, .3f), null, false);
            }

            // Focos de cubierta: zonas de peligro. El color avisa.
            b.Lamp("Foco Popa", new Vector3(0, 9.4f, -32), amber, 18, 9, true);
            b.Lamp("Foco Proa 1", new Vector3(0, 9.8f, 12), amber, 16, 8);
            b.Lamp("Foco Proa 2", new Vector3(0, 9.8f, 30), amber, 15, 8, true);
            b.Lamp("Foco Babor", new Vector3(-6.2f, 8.6f, -9), amber, 8, 3.5f);
            b.Lamp("Luz Tejado", new Vector3(5.8f, 11f, -3), amber, 8, 3.5f);
            b.Spot("Foco Mastil", new Vector3(0, 12.5f, -2.5f), new Vector3(72, 0, 0), amber, 40, 70, 12);

            ShipDetails(b);
            Combine(b, "HaiFeng"); // una malla por material, como un único asset cada una

            var prefabPath = $"{Root}/Prefabs/HaiFeng77.prefab";
            PrefabUtility.SaveAsPrefabAsset(b.root, prefabPath);
            Object.DestroyImmediate(b.root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        // ---------- Personajes ----------
        static Transform Part(Transform parent, PrimitiveType t, Material m, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(t);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.layer = Layers.Actor;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go.transform;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            var p = new GameObject(name).transform;
            p.SetParent(parent, false); p.localPosition = pos;
            return p;
        }

        static GameObject Humanoid(string name, Material body, Material skin, Material legsMat = null, Material hat = null)
        {
            var root = new GameObject(name) { layer = Layers.Actor };
            legsMat = legsMat ? legsMat : darkMetal;
            var bodyRoot = Pivot(root.transform, "Body", Vector3.zero);
            bool guard = hat != null;
            // Torso con hombros y caderas, cuello y cabeza: una silueta de persona, no una cápsula.
            Part(bodyRoot, PrimitiveType.Capsule, body, new Vector3(0, 1.2f, 0), new Vector3(.42f, .36f, .25f));
            Part(bodyRoot, PrimitiveType.Sphere, body, new Vector3(0, 1.43f, 0), new Vector3(.5f, .18f, .26f));
            Part(bodyRoot, PrimitiveType.Sphere, legsMat, new Vector3(0, .92f, 0), new Vector3(.36f, .2f, .24f));
            Part(bodyRoot, PrimitiveType.Cylinder, skin, new Vector3(0, 1.54f, 0), new Vector3(.09f, .07f, .09f));
            var head = Part(bodyRoot, PrimitiveType.Sphere, skin, new Vector3(0, 1.69f, .01f), new Vector3(.2f, .25f, .22f));
            head.name = "Cabeza";
            if (guard)
            {
                Part(bodyRoot, PrimitiveType.Cube, vest, new Vector3(0, 1.24f, 0), new Vector3(.4f, .05f, .26f));
                Part(bodyRoot, PrimitiveType.Cube, vest, new Vector3(0, 1.08f, 0), new Vector3(.4f, .05f, .24f));
                Part(bodyRoot, PrimitiveType.Sphere, hat, new Vector3(0, 1.77f, 0), new Vector3(.22f, .13f, .24f)); // gorra
                Part(bodyRoot, PrimitiveType.Cube, hat, new Vector3(0, 1.75f, .13f), new Vector3(.16f, .02f, .12f));
            }
            else
            {
                Part(bodyRoot, PrimitiveType.Sphere, rope, new Vector3(0, 1.75f, -.01f), new Vector3(.21f, .17f, .23f)); // gorro de lana
                Part(bodyRoot, PrimitiveType.Cube, jacket, new Vector3(0, 1.2f, .115f), new Vector3(.3f, .42f, .03f));
            }
            Transform Arm(string n, float x)
            {
                var a = Pivot(bodyRoot, n, new Vector3(x, 1.46f, 0));
                Part(a, PrimitiveType.Capsule, body, new Vector3(0, -.15f, 0), new Vector3(.11f, .17f, .11f));
                Part(a, PrimitiveType.Capsule, body, new Vector3(0, -.43f, .02f), new Vector3(.09f, .16f, .09f));
                Part(a, PrimitiveType.Sphere, skin, new Vector3(0, -.62f, .03f), new Vector3(.09f, .1f, .09f));
                return a;
            }
            Transform Leg(string n, float x)
            {
                var l = Pivot(bodyRoot, n, new Vector3(x, .88f, 0));
                Part(l, PrimitiveType.Capsule, legsMat, new Vector3(0, -.2f, 0), new Vector3(.16f, .24f, .16f));
                Part(l, PrimitiveType.Capsule, legsMat, new Vector3(0, -.56f, 0), new Vector3(.12f, .22f, .12f));
                Part(l, PrimitiveType.Cube, rubber, new Vector3(0, -.84f, .05f), new Vector3(.12f, .08f, .26f)); // bota
                return l;
            }
            var armL = Arm("ArmL", -.27f); var armR = Arm("ArmR", .27f);
            var legL = Leg("LegL", -.1f); var legR = Leg("LegR", .1f);
            var w = root.AddComponent<Walker>();
            w.legL = legL; w.legR = legR; w.armL = armL; w.armR = armR; w.body = bodyRoot;
            return root;
        }

        /// El protagonista: modelo de Models/Protagonista, a 1,80 m y con los pies en el origen.
        static GameObject Inspector(out HumanoidMotion motion)
        {
            const string dir = Root + "/Models/Protagonista/";
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(dir + "Textures_v2/Protagonista_v2_Normal.png");
            if (normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            var mat = Lit("M_Protagonista", Color.white, .35f, .1f, AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Textures_v2/Protagonista_v2_Color.jpg"));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Textures_v2/Protagonista_v2_Normal.png"));
            mat.SetKeyword(new LocalKeyword(mat.shader, "_NORMALMAP"), true);

            var root = new GameObject("Inspector") { layer = Layers.Actor };
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir + "Protagonista_v2.fbx"));
            model.transform.SetParent(root.transform, false);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Actor;

            var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
            skin.sharedMaterial = mat;
            skin.updateWhenOffscreen = true; // el modelo trae unos límites diminutos y desaparecería al acercar la cámara
            skin.forceMeshLod = 4; // 56 mil triángulos en vez de 787 mil (Mesh LOD activado en el importador del FBX)
            model.transform.localScale = Vector3.one * (1.8f / skin.bounds.size.y); // los clips ya apoyan los pies en el origen

            var animator = model.GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KairosAnimBuilder.ControllerPath);
            motion = model.AddComponent<HumanoidMotion>();
            motion.animator = animator;
            return root;
        }

        static NavMeshAgent Agent(GameObject go)
        {
            var a = go.AddComponent<NavMeshAgent>();
            a.radius = .35f; a.height = 1.8f; a.angularSpeed = 300; a.acceleration = 10; a.stoppingDistance = .2f;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            a.enabled = false;
            return a;
        }

        // ---------- Escenas ----------
        static Scene NewScene(string name)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            s.name = name;
            return s;
        }

        static Camera MainCamera(float far = 4000)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var c = go.AddComponent<Camera>();
            c.farClipPlane = far; c.nearClipPlane = .15f; c.fieldOfView = 58;
            c.allowHDR = true;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            go.AddComponent<AudioListener>();
            return c;
        }

        static VolumeProfile nightProfile;
        static void PostVolume()
        {
            var go = new GameObject("Global Volume");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true; v.sharedProfile = nightProfile;
        }

        static void BuildProfile()
        {
            string path = $"{GenDir}/NightProfile.asset";
            AssetDatabase.DeleteAsset(path);
            nightProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(nightProfile, path);
            var bloom = nightProfile.Add<Bloom>(true);
            bloom.intensity.Override(.9f); bloom.threshold.Override(.95f); bloom.scatter.Override(.72f);
            var tone = nightProfile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var vig = nightProfile.Add<Vignette>(true);
            vig.intensity.Override(.3f); vig.smoothness.Override(.5f);
            var ca = nightProfile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(.6f); ca.contrast.Override(10f); ca.saturation.Override(-4f);
            GradePost(nightProfile);
            foreach (var c in nightProfile.components) AssetDatabase.AddObjectToAsset(c, nightProfile);
            EditorUtility.SetDirty(nightProfile);
        }

        static Material skyMat, oceanMat;

        static void NightWorld(float clouds, float douglas, Color? ambient = null)
        {
            var moon = new GameObject("Luna");
            var l = moon.AddComponent<Light>();
            l.type = LightType.Directional; l.color = new Color(.66f, .78f, .95f); l.intensity = 1.25f;
            l.shadows = LightShadows.Soft; l.shadowStrength = .75f;
            moon.AddComponent<UniversalAdditionalLightData>();
            var moonDir = new Vector3(.45f, .55f, .7f).normalized;
            moon.transform.rotation = Quaternion.LookRotation(-moonDir);

            if (skyMat == null)
            {
                string sp = $"{GenDir}/M_NightSky.mat";
                skyMat = AssetDatabase.LoadAssetAtPath<Material>(sp);
                if (skyMat == null) { skyMat = new Material(Shader.Find("Kairos/NightSky")); AssetDatabase.CreateAsset(skyMat, sp); }
                skyMat.SetVector("_MoonDir", moonDir);
                skyMat.SetColor("_Horizon", Pal.Storm);
                skyMat.SetFloat("_Clouds", clouds);
                EditorUtility.SetDirty(skyMat);
                string op = $"{GenDir}/M_Ocean.mat";
                oceanMat = AssetDatabase.LoadAssetAtPath<Material>(op);
                if (oceanMat == null) { oceanMat = new Material(Shader.Find("Kairos/Ocean")); AssetDatabase.CreateAsset(oceanMat, op); }
                oceanMat.SetColor("_Deep", Pal.Ocean);
                oceanMat.SetColor("_SkyTint", Pal.Storm);
                oceanMat.SetTexture("_WaterNormal", waterNormal);
                EditorUtility.SetDirty(oceanMat);
            }
            RenderSettings.skybox = skyMat;
            RenderSettings.sun = l;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient ?? new Color(.22f, .28f, .34f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(Pal.Storm, Pal.Ocean, .35f);
            RenderSettings.fogDensity = .0022f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var o = new GameObject("Ocean", typeof(MeshFilter), typeof(MeshRenderer));
            o.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;
            o.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var ocean = o.AddComponent<Ocean>();
            ocean.douglasAtStart = douglas;
            ocean.ambient = RenderSettings.ambientLight;
            Fleet();
            PostVolume();
        }

        static void SaveScene(Scene s, string name, List<EditorBuildSettingsScene> list)
        {
            string path = $"{ScenesDir}/{name}.unity";
            EditorSceneManager.SaveScene(s, path);
            list.Add(new EditorBuildSettingsScene(path, true));
        }

        static GameObject Boat() => (GameObject)PrefabUtility.InstantiatePrefab(BoatPrefab());

        [MenuItem("Kairos/Generar juego")]
        public static void BuildAll()
        {
            EnsureLayers();
            foreach (var dir in new[] { Root + "/Generated", Root + "/Prefabs", Root + "/Scenes", Root + "/Resources" })
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Root, System.IO.Path.GetFileName(dir));

            BuildMaterials();
            BuildProfile();
            skyMat = null;
            boatPrefab = null;
            var shipPrefab = BuildShip();

            var scenes = new List<EditorBuildSettingsScene>();
            BuildMenuScene(shipPrefab, scenes);
            BuildRastreoScene(scenes);
            BuildSonarScene(shipPrefab, scenes);
            BuildInfiltrationScene(shipPrefab, scenes);
            BuildJuicioScene(scenes);
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.productName = "KAIROS Sombras en Altamar";
            PlayerSettings.companyName = "Unidad KAIROS";
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(scenes[0].path);
            Debug.Log("KAIROS: juego generado (" + scenes.Count + " escenas).");
        }

        static void BuildMenuScene(GameObject ship, List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("00_Menu");
            var cam = MainCamera();
            NightWorld(.55f, 3.5f);
            PrefabUtility.InstantiatePrefab(ship);
            var go = new GameObject("Menu");
            go.AddComponent<MenuController>().cam = cam;
            SaveScene(s, "00_Menu", list);
        }

        static void BuildRastreoScene(List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("01_Rastreo");
            var cam = MainCamera(50);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.01f, .02f, .035f);
            cam.nearClipPlane = .005f; cam.fieldOfView = 40;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            PostVolume();
            var go = new GameObject("Rastreo");
            go.AddComponent<RastreoController>().cam = cam;
            SaveScene(s, "01_Rastreo", list);
        }

        static void BuildSonarScene(GameObject ship, List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("02_Sonar");
            var cam = MainCamera();
            NightWorld(.55f, 4f);
            var sh = (GameObject)PrefabUtility.InstantiatePrefab(ship);
            var boat = Boat();
            var go = new GameObject("Sonar");
            var c = go.AddComponent<SonarController>();
            c.cam = cam; c.ship = sh.transform; c.boat = boat.transform;
            SaveScene(s, "02_Sonar", list);
        }

        static Transform Waypoint(Transform parent, string name, Vector3 p)
        {
            var w = new GameObject(name).transform;
            w.SetParent(parent, false); w.position = p;
            return w;
        }

        static GuardAI Guard(Transform parent, string name, Vector3 start, Material hat, Vector3[] route)
        {
            var go = Humanoid(name, coat, skinD, darkMetal, hat);
            go.transform.SetParent(parent, false);
            go.transform.position = start;
            Agent(go);
            var g = go.AddComponent<GuardAI>();
            g.displayName = name;
            var wps = new List<Transform>();
            for (int i = 0; i < route.Length; i++) wps.Add(Waypoint(parent, $"{name}_wp{i}", route[i]));
            g.route = wps.ToArray();
            return g;
        }

        static PersonNpc Person(Transform parent, string id, Vector3 pos, float yaw, Material skin)
        {
            var go = Humanoid("Persona " + id, worker, skin);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Agent(go);
            var n = go.AddComponent<PersonNpc>();
            n.personId = id;
            Trigger(go.transform, Vector3.up, new Vector3(1.2f, 2f, 1.2f));
            return n;
        }

        static void Trigger(Transform parent, Vector3 local, Vector3 size)
        {
            var t = new GameObject("Trigger") { layer = Layers.Interact };
            t.transform.SetParent(parent, false);
            t.transform.localPosition = local;
            var c = t.AddComponent<BoxCollider>(); c.size = size; c.isTrigger = true;
        }

        static EvidenceProp Evidence(Transform parent, string id, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Prueba " + id;
            go.transform.SetParent(parent, false);
            go.transform.position = pos; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.layer = Layers.Interact;
            var e = go.AddComponent<EvidenceProp>();
            e.evidenceId = id;
            var trig = new GameObject("Trigger") { layer = Layers.Interact };
            trig.transform.SetParent(go.transform, false);
            trig.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            var c = trig.AddComponent<BoxCollider>(); c.size = Vector3.one * 1.3f; c.isTrigger = true;

            // Marca verde discreta: se documenta con la cámara.
            var mark = new GameObject("Luz prueba");
            mark.transform.SetParent(go.transform, false);
            mark.transform.position = pos + Vector3.up * .4f;
            var l = mark.AddComponent<Light>();
            l.type = LightType.Point; l.color = Pal.Sealed; l.range = 2.2f; l.intensity = .9f; l.shadows = LightShadows.None;
            return e;
        }

        static LockedDoor Door(Transform parent, string id, string doorName, Vector3 pos, Vector3 size, float seconds, bool cuttable, bool compressors, params string[] frees)
        {
            var solid = new GameObject("Hoja " + id) { layer = Layers.ShipSolid };
            solid.transform.SetParent(parent, false);
            solid.transform.position = pos;
            var bc = solid.AddComponent<BoxCollider>(); bc.size = size;
            var vis = Part(solid.transform, PrimitiveType.Cube, rust, Vector3.zero, size);
            vis.gameObject.layer = Layers.ShipSolid;
            var lockVis = Part(solid.transform, PrimitiveType.Cube, bulb, new Vector3(size.x > size.z ? 0 : Mathf.Sign(-pos.x) * .15f, .1f, size.x > size.z ? -.15f : 0), new Vector3(.18f, .24f, .18f));
            lockVis.gameObject.layer = Layers.ShipSolid;

            var go = new GameObject("Puerta " + id) { layer = Layers.Interact };
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var c = go.AddComponent<BoxCollider>(); c.size = size + Vector3.one * 1.2f; c.isTrigger = true;
            var d = go.AddComponent<LockedDoor>();
            d.doorId = id; d.doorName = doorName; d.cutSeconds = seconds; d.cuttable = cuttable;
            d.nearCompressors = compressors; d.solid = solid; d.freesPersons = frees;
            return d;
        }

        static void BuildInfiltrationScene(GameObject shipPrefab, List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("03_Infiltracion");
            var cam = MainCamera();
            NightWorld(.75f, 4.5f, new Color(.24f, .29f, .34f));
            var ship = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
            var gameplay = new GameObject("Gameplay").transform;

            var surfaceGo = new GameObject("NavMesh");
            var surface = surfaceGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << Layers.ShipSolid;
            surface.overrideVoxelSize = true; surface.voxelSize = .1f;

            // Jugador
            var player = Inspector(out var motion);
            player.transform.position = new Vector3(2.5f, 6.1f, -38.5f); // lejos de la salida: no se ofrece nada más subir
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = .3f; cc.center = new Vector3(0, .95f, 0); cc.stepOffset = .35f; cc.slopeLimit = 50;
            var agent = player.AddComponent<PlayerAgent>();
            agent.cam = cam; agent.motion = motion;

            // Guardias: rondas en bucle con huecos para colarse.
            var guards = new List<GuardAI>();
            guards.Add(Guard(gameplay, "Contramaestre", new Vector3(-6, 6.05f, -24), bulb, new[] {
                new Vector3(-6, 6, -23), new Vector3(-6, 6, 1), new Vector3(0, 6, 8), new Vector3(0, 6, 34), new Vector3(5.8f, 6, 12), new Vector3(0, 6, 4), new Vector3(-6, 6, -6), new Vector3(0, 6, -24), new Vector3(0, 6, -37), new Vector3(3, 6, -29) }));
            guards.Add(Guard(gameplay, "Guardia de bodega", new Vector3(0, 3.05f, -26), darkMetal, new[] {
                new Vector3(0, 3, -26), new Vector3(3.6f, 3, -6), new Vector3(0, 3, 0), new Vector3(0, 3, 19), new Vector3(3.2f, 3, 12), new Vector3(0, 3, 8), new Vector3(-3.5f, 3, -20), new Vector3(0, 3, -12) }));
            guards.Add(Guard(gameplay, "Guardia de habilitación", new Vector3(0, 6.05f, -18), darkMetal, new[] {
                new Vector3(0, 6, -18), new Vector3(0, 6, 0), new Vector3(3.2f, 6, -8), new Vector3(0, 6, 4), new Vector3(6, 6, 3), new Vector3(0, 6, -22), new Vector3(0, 6, -9) }));
            var captain = Guard(gameplay, "Capitán", new Vector3(0, 9.15f, -7), coat, new[] {
                new Vector3(0, 9.1f, -7), new Vector3(-2, 9.1f, -11), new Vector3(2.5f, 9.1f, -9) });
            guards.Add(captain);
            captain.viewRange = 9;

            // Cámara del barco: vigila la cubierta de popa; el pasillo inferior queda libre.
            var cam1 = new GameObject("Camara popa").AddComponent<SecurityCam>();
            cam1.transform.SetParent(gameplay, false);
            cam1.transform.SetPositionAndRotation(new Vector3(0, 8.6f, -20.4f), Quaternion.Euler(0, 180, 0));
            cam1.floorY = 6f; cam1.range = 12f; cam1.alarmName = "La cámara de popa";
            Part(cam1.transform, PrimitiveType.Cube, darkMetal, Vector3.zero, new Vector3(.18f, .18f, .4f));

            // Puertas cerradas (encajan en los huecos de los mamparos)
            var doorSize = new Vector3(.3f, 2.2f, 1.4f);
            var doors = new List<LockedDoor>
            {
                Door(gameplay, "babor", "escotilla de babor", new Vector3(-1.25f, 4.1f, 2), doorSize, 6f, true, false, "ari", "moussa", "thant"),
                Door(gameplay, "servicio", "puerta de servicio", new Vector3(-4f, 4.1f, -6), new Vector3(1.4f, 2.2f, .3f), 6f, false, false),
                Door(gameplay, "capitan", "puerta del capitán", new Vector3(-1.25f, 7.1f, -16), doorSize, 6f, true, false),
                Door(gameplay, "oficial", "puerta del oficial", new Vector3(1.25f, 7.1f, -16), doorSize, 4f, true, false),
                Door(gameplay, "maquinas", "reja de máquinas", new Vector3(1.25f, 4.1f, -21), doorSize, 5f, true, true),
            };

            // Personas
            var people = new List<PersonNpc>
            {
                Person(gameplay, "rodel", new Vector3(-3f, 6.05f, -9.5f), 40, skinA),
                Person(gameplay, "ari", new Vector3(-3.3f, 3.05f, 5f), 110, skinC),
                Person(gameplay, "moussa", new Vector3(-3.8f, 3.05f, -2.5f), 70, skinB),
                Person(gameplay, "thant", new Vector3(-3.2f, 3.05f, 1f), 100, skinD),
            };

            // Pruebas
            var paper = Lit("M_Paper", new Color(.8f, .78f, .68f), .2f, 0);
            var ev = new List<EvidenceProp>
            {
                Evidence(gameplay, "e_celda", new Vector3(-6.3f, 4.2f, 3.5f), new Vector3(.08f, .8f, 1.2f), rust),
                Evidence(gameplay, "e_pasaportes", new Vector3(-3.95f, 7.35f, -14f), new Vector3(.3f, .25f, .5f), paper),
                Evidence(gameplay, "e_deudas", new Vector3(2.9f, 6.95f, -18.8f), new Vector3(.5f, .08f, .7f), paper),
                Evidence(gameplay, "e_contratos", new Vector3(-3.2f, 6.85f, -5f), new Vector3(.6f, .06f, .9f), paper),
                // En el camarote del oficial: el camino principal no obliga a pasar por el capitán.
                Evidence(gameplay, "e_manifiesto", new Vector3(4.1f, 6.95f, -18.8f), new Vector3(.7f, .05f, .6f), paper),
                Evidence(gameplay, "e_registro", new Vector3(-3.5f, 6.95f, -18.8f), new Vector3(.7f, .1f, .5f), paper),
                Evidence(gameplay, "e_turnos", new Vector3(4.86f, 7.5f, -7f), new Vector3(.06f, .9f, .7f), paper),
                Evidence(gameplay, "e_candados", new Vector3(-1.05f, 4.3f, 1.05f), new Vector3(.15f, .35f, .3f), darkMetal),
                Evidence(gameplay, "e_facturas", new Vector3(6.66f, 4.3f, -21f), new Vector3(.06f, .7f, .5f), paper),
            };

            // Consola del puente y salida
            var console = new GameObject("Consola") { layer = Layers.Interact };
            console.transform.SetParent(gameplay, false);
            console.transform.position = new Vector3(0, 10f, -5.8f);
            var cc2 = console.AddComponent<BoxCollider>(); cc2.size = new Vector3(5, 2, 1.6f); cc2.isTrigger = true;
            console.AddComponent<BridgeConsole>();

            var exit = new GameObject("Salida popa") { layer = Layers.Interact };
            exit.transform.SetParent(gameplay, false);
            exit.transform.position = new Vector3(0, 7f, -44f);
            var ec = exit.AddComponent<BoxCollider>(); ec.size = new Vector3(6, 2.5f, 2.2f); ec.isTrigger = true;
            exit.AddComponent<ExitZone>();

            var boat = Boat();
            boat.transform.position = new Vector3(0, 0, -52);

            var compressor = new GameObject("Compresores").transform;
            compressor.position = new Vector3(3.9f, 4.2f, -21f);

            // Controlador
            var ctrl = new GameObject("Infiltracion").AddComponent<InfiltrationController>();
            ctrl.cam = cam; ctrl.player = agent; ctrl.surface = surface; ctrl.guards = guards.ToArray(); ctrl.captain = captain;
            ctrl.people = people.ToArray(); ctrl.evidenceProps = ev.ToArray(); ctrl.doors = doors.ToArray();
            ctrl.boat = boat.transform; ctrl.compressorRoom = compressor;
            var lights = new List<Light>();
            foreach (var l in ship.GetComponentsInChildren<Light>())
                if (l.name.StartsWith("Foco") || l.name == "Luz Tejado") lights.Add(l);
            ctrl.dangerLights = lights.ToArray();

            SaveScene(s, "03_Infiltracion", list);
        }

        static void BuildJuicioScene(List<EditorBuildSettingsScene> list)
        {
            var s = NewScene("04_Juicio");
            var cam = MainCamera(50);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.01f, .03f, .05f);
            RenderSettings.fog = false;
            new GameObject("Juicio").AddComponent<JuicioController>();
            SaveScene(s, "04_Juicio", list);
        }

        static void EnsureLayers()
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            void Set(int i, string n) { layers.GetArrayElementAtIndex(i).stringValue = n; }
            Set(Layers.ShipSolid, "ShipSolid"); Set(Layers.Actor, "Actor"); Set(Layers.Thermal, "Thermal");
            Set(Layers.Interact, "Interact"); Set(Layers.HullShell, "HullShell");
            tm.ApplyModifiedProperties();
        }

        /// Comprobación mínima de las reglas del GDD. Menú: Kairos > Verificar reglas.
        [MenuItem("Kairos/Verificar reglas")]
        public static string CheckRules()
        {
            void Must(bool ok, string what) { if (!ok) throw new System.Exception("Regla rota: " + what); }
            int all = (1 << 5) - 1;
            Must(CaseData.ComputeIR(CaseData.Target, all, 0) == 72, "Hai Feng 77 con todas las capas da IR 72");
            Must(CaseData.ComputeIR(CaseData.Target, all, 24) == 67, "el IR cae 5 por día");
            Must(CaseData.ComputeIR(CaseData.Target, 1, 0) < CaseData.IrToScan, "AIS solo no basta");
            foreach (var s in CaseData.Ships) if (!s.target) Must(CaseData.ComputeIR(s, all, 0) < CaseData.IrToScan, s.name + " no debe pasar de 60");

            int k4 = CaseData.PersonsCompartment;
            Must(CaseData.Compartments[k4].kind == 1, "el compartimento marcado es el de las personas");
            Must(Mathf.Approximately(CaseData.ComputeConfidence(k4, new[] { 7, 7, 7, 7, 7 }), 100f), "con las 3 señales da 100 %");
            Must(CaseData.ComputeConfidence(k4, new[] { 0, 0, 0, 1, 0 }) >= CaseData.ConfidenceToBoard, "basta con el sonar y marcar bien");
            Must(CaseData.ComputeConfidence(k4, new[] { 0, 0, 0, 6, 0 }) < CaseData.ConfidenceToBoard, "las ayudas no sustituyen al sonar");
            Must(CaseData.ComputeConfidence(0, new[] { 7, 7, 7, 7, 7 }) == 0, "marcar donde no hay personas no da confianza");
            Must(CaseData.ComputeConfidence(-1, new[] { 7, 7, 7, 7, 7 }) == 0, "sin marcar no hay confianza");

            Must(CaseData.VerdictFor(3) == 0 && CaseData.VerdictFor(4) == 1 && CaseData.VerdictFor(7) == 2 && CaseData.VerdictFor(11) == 3, "umbrales OIT 4/7/11");
            Must(!CaseData.ChainComplete(new List<string> { "0-3", "3-4" }), "cadena incompleta");
            Must(CaseData.ChainComplete(new List<string> { "0-3", "3-4", "4-5" }), "cadena vía fachada");
            Must(CaseData.ChainComplete(new List<string> { "0-1", "1-5" }), "cadena vía frigorífico");

            var everything = new List<string>();
            foreach (var e in CaseData.EvidenceDefs) everything.Add(e.id);
            Must(CaseData.CountIndicators(everything) == 11, "las 11 pruebas de OIT son alcanzables");
            Must(everything.Count <= CaseData.MaxEvidence, "todas las pruebas caben en el maletín");

            // El camino que marca el objetivo debe bastar para una sentencia.
            var path = new List<string> { "e_facturas", "e_celda", "t_ari", "e_manifiesto" };
            var pathLinks = new List<string>();
            foreach (var id in path) pathLinks.AddRange(CaseData.Evidence(id).edges);
            Must(CaseData.ChainComplete(pathLinks), "el camino guiado completa la cadena");
            Must(CaseData.VerdictFor(CaseData.CountIndicators(path)) >= 1, "el camino guiado da al menos una Orden de Retención");

            // Lo que se puede perder nunca rompe ese camino.
            foreach (var id in CaseData.BurnOrder) Must(!path.Contains(id), id + " no debe destruirse: es del camino guiado");
            foreach (var id in CaseData.DumpedWhenDetected) Must(!path.Contains(id), id + " no debe tirarse al mar: es del camino guiado");
            foreach (var e in CaseData.EvidenceDefs)
                foreach (var k in e.edges) Must(k == "0-1" || k == "0-2" || k == "0-3" || k == "3-4" || k == "4-5" || k == "1-5", "enlace desconocido " + k);
            Debug.Log("KAIROS: reglas OK");
            return "OK";
        }
    }
}
