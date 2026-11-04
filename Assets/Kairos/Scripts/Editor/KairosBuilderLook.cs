using System;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kairos.EditorTools
{
    /// Aspecto del juego: texturas con relieve generadas por código, detalles del buque, lancha y flota lejana.
    public static partial class KairosBuilder
    {
        // ---------- Ruido periódico: las texturas se repiten sin costuras ----------
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float Noise(float u, float v, int px, int py, int seed)
        {
            float x = u * px, y = v * py;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int M(int a, int p) => ((a % p) + p) % p;
            float a00 = Hash(M(xi, px), M(yi, py), seed), a10 = Hash(M(xi + 1, px), M(yi, py), seed);
            float a01 = Hash(M(xi, px), M(yi + 1, py), seed), a11 = Hash(M(xi + 1, px), M(yi + 1, py), seed);
            return Mathf.Lerp(Mathf.Lerp(a00, a10, fx), Mathf.Lerp(a01, a11, fx), fy);
        }

        static float Fbm(float u, float v, int px, int py, int seed, int octaves = 4)
        {
            float sum = 0, amp = .5f, total = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(u, v, px, py, seed + i * 17) * amp;
                total += amp; amp *= .5f; px *= 2; py *= 2;
            }
            return sum / total;
        }

        static float Smooth(float a, float b, float x) { x = Mathf.Clamp01((x - a) / (b - a)); return x * x * (3 - 2 * x); }
        static float SeamDist(float t, int n) { float f = t * n; return Mathf.Abs(f - Mathf.Round(f)) / n; }
        static float Bump(float d2, float r) => Mathf.Exp(-d2 / (r * r));

        /// Color, altura y lisura (en el canal alfa) por punto. Si ya existe no se regenera: tarda varios segundos.
        static void Surface(string name, int size, float normalStrength, Func<float, float, (Color c, float h, float s)> f, out Texture2D albedo, out Texture2D normal)
        {
            string colorPath = $"{GenDir}/T2_{name}.png", normalPath = $"{GenDir}/T2_{name}_N.png";
            if (!System.IO.File.Exists(colorPath) || !System.IO.File.Exists(normalPath))
            {
                var col = new Color[size * size];
                var height = new float[size * size];
                Parallel.For(0, size, y =>
                {
                    for (int x = 0; x < size; x++)
                    {
                        var (c, h, s) = f((x + .5f) / size, (y + .5f) / size);
                        c.a = s;
                        col[y * size + x] = c;
                        height[y * size + x] = h;
                    }
                });
                var nor = new Color[size * size];
                Parallel.For(0, size, y =>
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = height[y * size + (x + 1) % size] - height[y * size + (x + size - 1) % size];
                        float dy = height[(y + 1) % size * size + x] - height[(y + size - 1) % size * size + x];
                        var n = new Vector3(-dx * normalStrength, -dy * normalStrength, 1).normalized;
                        nor[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                    }
                });
                void Save(string path, Color[] data)
                {
                    var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    t.SetPixels(data); t.Apply();
                    System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(t);
                    AssetDatabase.ImportAsset(path);
                }
                Save(colorPath, col);
                Save(normalPath, nor);
                var colorImp = (TextureImporter)AssetImporter.GetAtPath(colorPath);
                colorImp.alphaIsTransparency = false; colorImp.anisoLevel = 4; colorImp.wrapMode = TextureWrapMode.Repeat;
                colorImp.SaveAndReimport();
                var normalImp = (TextureImporter)AssetImporter.GetAtPath(normalPath);
                normalImp.textureType = TextureImporterType.NormalMap; normalImp.anisoLevel = 4; normalImp.wrapMode = TextureWrapMode.Repeat;
                normalImp.SaveAndReimport();
            }
            albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
            normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        }

        /// Chapa pintada de barco: paneles soldados, remaches, pintura saltada y chorretones de óxido hacia abajo.
        static Func<float, float, (Color, float, float)> Plate(Color paintColor, float rustiness, int seed) => (u, v) =>
        {
            float big = Fbm(u, v, 4, 4, seed, 5), fine = Fbm(u, v, 48, 48, seed + 40, 3), streak = Fbm(u, v, 40, 3, seed + 80, 3), chip = Fbm(u, v, 18, 18, seed + 120, 4);
            float dv = SeamDist(u, 2), dh = SeamDist(v, 2), d = Mathf.Min(dv, dh);
            float groove = Bump(d * d, .0035f);
            float along = u - Mathf.Round(u * 24) / 24f, across = v - Mathf.Round(v * 24) / 24f;
            float rivet = Mathf.Max(Bump(along * along + (dh - .014f) * (dh - .014f), .0034f), Bump(across * across + (dv - .014f) * (dv - .014f), .0034f));
            float rust = Smooth(.56f - rustiness * .3f, .72f - rustiness * .3f, (1 - big) * .62f + chip * .22f + (streak - .5f) * .55f + Mathf.Exp(-d / .025f) * .2f);
            var paintCol = paintColor * (.78f + fine * .4f);
            paintCol *= 1f - Smooth(.55f, .8f, streak) * .3f; // suciedad que chorrea
            var rustCol = Color.Lerp(new Color(.17f, .08f, .04f), Pal.Rust * 1.15f, Smooth(.3f, .7f, fine));
            var c = Color.Lerp(paintCol, rustCol, rust) * (1 - groove * .55f);
            float h = .5f - rust * .03f + (fine - .5f) * rust * .35f + (chip - .5f) * .05f - groove * .4f + rivet * .55f;
            return (c, h, Mathf.Lerp(.5f, .1f, rust) * (1 - groove));
        };

        /// Chapa estriada de cubierta, mojada: los charcos brillan y aplanan el relieve.
        static (Color, float, float) DeckPlate(float u, float v)
        {
            const int N = 30;
            float big = Fbm(u, v, 5, 5, 300, 5), fine = Fbm(u, v, 50, 50, 340, 3);
            float cu = u * N, cv = v * N;
            int iu = Mathf.FloorToInt(cu), iv = Mathf.FloorToInt(cv);
            float fu = cu - iu - .5f, fv = cv - iv - .5f;
            float ang = (iu + iv) % 2 == 0 ? .785f : -.785f;
            float rx = fu * Mathf.Cos(ang) + fv * Mathf.Sin(ang), ry = -fu * Mathf.Sin(ang) + fv * Mathf.Cos(ang);
            float tread = Bump(rx * rx * .09f + ry * ry, .13f);
            float puddle = Smooth(.56f, .66f, Fbm(u, v, 6, 6, 380, 3));
            float rust = Smooth(.5f, .72f, (1 - big) * .75f + (fine - .5f) * .5f);
            var c = Color.Lerp(new Color(.25f, .26f, .26f) * (.75f + fine * .5f), new Color(.24f, .12f, .06f), rust);
            c *= Mathf.Lerp(1f, .55f, puddle); // lo mojado se ve más oscuro
            return (c, tread * (1 - puddle * .85f) * .6f + (fine - .5f) * .05f, Mathf.Lerp(.42f, .92f, puddle));
        }

        /// Contenedor corrugado.
        static (Color, float, float) Corrugated(float u, float v)
        {
            float big = Fbm(u, v, 4, 4, 500, 4), fine = Fbm(u, v, 40, 40, 520, 3), streak = Fbm(u, v, 30, 3, 540, 3);
            float rib = Mathf.Sin(u * Mathf.PI * 2 * 12);
            float rust = Smooth(.58f, .74f, (1 - big) * .6f + (streak - .5f) * .6f + (rib > .9f ? .12f : 0));
            var c = Color.Lerp(new Color(.8f, .8f, .8f) * (.75f + fine * .4f), new Color(.3f, .15f, .08f), rust);
            return (c, rib * .35f + (fine - .5f) * rust * .2f, Mathf.Lerp(.42f, .12f, rust));
        }

        static Material super, orange, redPaint, rope, mattress, yellow, pipeBlue, pipeGreen, rubber, navRed, navGreen, navWhite, screenMat, netMat, jacket, vest;
        static Texture2D waterNormal;

        static void BuildMaterials()
        {
            Surface("Rust", 1024, 60, Plate(new Color(.2f, .14f, .13f), .4f, 1), out var rustC, out var rustN);
            Surface("HullPaint", 1024, 45, Plate(Pal.HullPaint * 1.25f, .35f, 60), out var hullC, out var hullN);
            Surface("Super", 1024, 45, Plate(new Color(.4f, .4f, .36f), .1f, 120), out var superC, out var superN);
            Surface("Steel", 1024, 40, Plate(new Color(.42f, .49f, .45f), .2f, 180), out var steelC, out var steelN);
            Surface("Floor", 1024, 30, Plate(new Color(.3f, .36f, .34f), .3f, 240), out var floorC, out var floorN);
            Surface("Deck", 1024, 28, DeckPlate, out var deckC, out var deckN);
            Surface("Crate", 512, 25, Corrugated, out var crateC, out var crateN);
            Surface("Water", 512, 26, (u, v) => (Color.gray, Fbm(u, v, 5, 5, 700, 6), .5f), out _, out waterNormal);

            steel = Lit("M_Steel", Color.white, 1f, .15f, steelC, null, steelN);
            rust = Lit("M_Rust", Color.white, 1f, .1f, rustC, null, rustN);
            super = Lit("M_Super", Color.white, 1f, .1f, superC, null, superN);
            deck = Lit("M_Deck", Color.white, 1f, .15f, deckC, null, deckN);
            paint = Lit("M_HullPaint", Color.white, 1f, .1f, hullC, null, hullN);
            floor = Lit("M_Floor", Color.white, 1f, .1f, floorC, null, floorN);
            crate = Lit("M_Crate", new Color(.36f, .5f, .6f), 1f, .2f, crateC, null, crateN);
            frozen = Lit("M_Frozen", new Color(.72f, .85f, .98f), 1f, .1f, crateC, null, crateN);
            darkMetal = Lit("M_DarkMetal", new Color(.17f, .18f, .19f), .45f, .3f);
            skinA = Lit("M_SkinA", new Color(.55f, .38f, .26f), .3f, 0);
            skinB = Lit("M_SkinB", new Color(.33f, .22f, .15f), .3f, 0);
            skinC = Lit("M_SkinC", new Color(.68f, .5f, .36f), .3f, 0);
            skinD = Lit("M_SkinD", new Color(.45f, .31f, .22f), .3f, 0);
            coat = Lit("M_GuardCoat", new Color(.13f, .15f, .13f), .25f, 0);
            suit = Lit("M_Suit", new Color(.06f, .13f, .16f), .45f, .3f);
            worker = Lit("M_Worker", new Color(.4f, .34f, .24f), .12f, 0);
            jacket = Lit("M_WorkerJacket", new Color(.2f, .27f, .33f), .15f, 0);
            vest = Lit("M_GuardVest", new Color(.75f, .42f, .08f), .3f, 0);
            bulb = Lit("M_Bulb", Color.white, .2f, 0, null, Pal.DeckLight * 6f);
            cyanGlow = Lit("M_Instrument", Color.black, .6f, 0, null, Pal.Sonar * 2.2f);
            windowMat = Lit("M_Window", new Color(.04f, .06f, .07f), .9f, .2f, null, Pal.DeckLight * .8f);
            screenMat = Lit("M_Screen", Color.black, .7f, 0, null, new Color(.2f, .75f, .55f) * 1.4f);
            orange = Lit("M_Orange", new Color(.85f, .3f, .05f), .4f, 0);
            redPaint = Lit("M_RedPaint", new Color(.6f, .06f, .05f), .45f, 0);
            yellow = Lit("M_Yellow", new Color(.75f, .55f, .08f), .35f, 0);
            rope = Lit("M_Rope", new Color(.34f, .3f, .22f), .1f, 0);
            netMat = Lit("M_Net", new Color(.16f, .24f, .22f), .12f, 0);
            mattress = Lit("M_Mattress", new Color(.3f, .29f, .26f), .08f, 0);
            pipeBlue = Lit("M_PipeBlue", new Color(.14f, .26f, .38f), .4f, .3f);
            pipeGreen = Lit("M_PipeGreen", new Color(.16f, .32f, .2f), .4f, .3f);
            rubber = Lit("M_Rubber", new Color(.09f, .1f, .11f), .35f, 0);
            navRed = Lit("M_NavRed", Color.black, .5f, 0, null, new Color(1f, .05f, .03f) * 5f);
            navGreen = Lit("M_NavGreen", Color.black, .5f, 0, null, new Color(.05f, 1f, .2f) * 5f);
            navWhite = Lit("M_NavWhite", Color.black, .5f, 0, null, Color.white * 6f);

            string glowPath = "Assets/Kairos/Resources/KGlow.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(glowPath) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Kairos/Glow")), glowPath);
        }

        static Material GlowAsset(string name, Color c, bool additive, bool round)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Kairos/Glow")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_Color", c);
            m.SetFloat("_Src", (float)BlendMode.SrcAlpha);
            m.SetFloat("_Dst", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZT", (float)CompareFunction.LessEqual);
            m.SetFloat("_ZW", 0);
            m.SetFloat("_Round", round ? 1 : 0);
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// Une las piezas por material en una malla por cada uno, guardadas como assets.
        static void Combine(ShipBuild b, string prefix)
        {
            int idx = 0;
            foreach (var kv in b.acc)
            {
                var go = new GameObject("Geo_" + kv.Key.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(b.root.transform, false);
                go.isStatic = prefix == "HaiFeng";
                var mesh = kv.Value.ToMesh(prefix + "_" + kv.Key.name);
                AssetDatabase.CreateAsset(mesh, $"{GenDir}/{prefix}_Geo{idx++}.asset");
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = kv.Key;
            }
        }

        // ---------- Detalles del buque: casi todos sin colisión, para no cambiar las rutas ----------
        static void ShipDetails(ShipBuild b)
        {
            Exterior(b);
            Interior(b);
            LightCones(b);
            Funnel(b);
            ShipName(b, "HAI FENG 77", new Vector3(0, 4.7f, -45.1f), Quaternion.identity, 1.5f);
            ShipName(b, "HAI FENG 77", new Vector3(6.55f, 4.9f, 27), Quaternion.Euler(0, -92, 0), 1.2f);
            ShipName(b, "HAI FENG 77", new Vector3(-6.55f, 4.9f, 27), Quaternion.Euler(0, 92, 0), 1.2f);
        }

        static void Exterior(ShipBuild b)
        {
            var st = Stations();
            // Pasamanos sobre la borda y verduguillos del casco: dan línea al barco.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < st.Length - 1; i++)
                {
                    Vector3 Rail(int k, float y, float w) => new Vector3(side * Beam(st[k]) * w, y, st[k]);
                    b.Pipe(darkMetal, Rail(i, 7.28f, 1f), Rail(i + 1, 7.28f, 1f), .05f, 6);
                    if (i % 2 == 0) b.Pipe(darkMetal, Rail(i, 7.2f, 1f), Rail(i, 7.28f, 1f), .035f, 6);
                    b.Pipe(darkMetal, Rail(i, 6.62f, 1.012f), Rail(i + 1, 6.62f, 1.012f), .09f, 6);
                    b.Pipe(darkMetal, Rail(i, 3.1f, 1.0f), Rail(i + 1, 3.1f, 1.0f), .06f, 6);
                }
            for (float x = -7; x < 7; x += 1.4f) b.Pipe(darkMetal, new Vector3(x, 7.28f, -45), new Vector3(x + 1.4f, 7.28f, -45), .05f, 6);

            // Bitas de amarre.
            foreach (var z in new[] { -42f, -22f, 5f, 26f })
                foreach (var side in new[] { -1f, 1f })
                {
                    var p = new Vector3(side * (Beam(z) - .55f), 6, z);
                    b.Pipe(darkMetal, p, p + Vector3.up * .55f, .13f, 10);
                    b.Pipe(darkMetal, p + Vector3.forward * .45f, p + Vector3.forward * .45f + Vector3.up * .55f, .13f, 10);
                }

            // Pórtico de popa con su motón.
            foreach (var x in new[] { -5.2f, 5.2f }) b.Pipe(rust, new Vector3(x, 6, -43.4f), new Vector3(x * .8f, 11.6f, -43.4f), .16f, 10);
            b.Pipe(rust, new Vector3(-4.3f, 11.6f, -43.4f), new Vector3(4.3f, 11.6f, -43.4f), .15f, 10);
            b.Box(darkMetal, new Vector3(0, 11.2f, -43.4f), new Vector3(.4f, .55f, .3f), null, false);
            b.Pipe(rope, new Vector3(0, 11, -43.4f), new Vector3(0, 7.7f, -41.6f), .025f, 5);

            // Chigre sobre su base, redes y aparejos.
            b.Pipe(rope, new Vector3(-1.2f, 8, -41), new Vector3(1.2f, 8, -41), .42f, 14);
            foreach (var x in new[] { -1.35f, 1.35f }) b.Box(darkMetal, new Vector3(x, 7.9f, -41), new Vector3(.12f, .7f, 1.1f), null, false);
            b.Ellipsoid(netMat, new Vector3(-5, 6.25f, -33), new Vector3(1.2f, .42f, 1.8f));
            b.Ellipsoid(netMat, new Vector3(-4.6f, 6.45f, -32.2f), new Vector3(.7f, .35f, .9f));
            b.Solid(new Vector3(-5, 6.4f, -33), new Vector3(2.2f, .8f, 3));
            b.Pipe(rope, new Vector3(3.6f, 6.03f, -41.4f), new Vector3(3.6f, 6.2f, -41.4f), .55f, 14);
            b.Pipe(rope, new Vector3(-3.7f, 6.03f, -21.6f), new Vector3(-3.7f, 6.18f, -21.6f), .5f, 14);
            foreach (var (x, z) in new[] { (-3.6f, 12.7f), (-3.1f, 7.6f) }) b.Ellipsoid(orange, new Vector3(x, 7.05f, z), Vector3.one * .3f); // boyas sobre las tapas de bodega

            // Bidones.
            foreach (var (x, z, m) in new[] { (5.6f, -38.6f, redPaint), (5.6f, -39.4f, pipeBlue), (4.9f, -39f, redPaint), (-5.7f, 19.2f, yellow), (5.3f, 22.5f, pipeBlue) })
            {
                var p = new Vector3(x, 6, z);
                b.Pipe(m, p, p + Vector3.up * .9f, .3f, 12);
                b.Pipe(darkMetal, p + Vector3.up * .3f, p + Vector3.up * .33f, .31f, 12);
                b.Pipe(darkMetal, p + Vector3.up * .6f, p + Vector3.up * .63f, .31f, 12);
                b.Solid(p + Vector3.up * .45f, new Vector3(.6f, .9f, .6f));
            }

            // Ventanas y marcos de la habilitación.
            foreach (var z in new[] { -17f, -12.5f, -8f, -3.5f, .5f })
                foreach (var x in new[] { -5.1f, 5.1f })
                {
                    b.Box(darkMetal, new Vector3(x, 7.8f, z), new Vector3(.05f, .8f, 1.25f), null, false);
                    b.Box(Mathf.Abs(z + 8) < .1f ? super : windowMat, new Vector3(x * 1.008f, 7.8f, z), new Vector3(.05f, .62f, 1.05f), null, false);
                }
            foreach (var x in new[] { -3.2f, 3.2f })
            {
                b.Box(darkMetal, new Vector3(x, 7.8f, 2.12f), new Vector3(1.25f, .8f, .05f), null, false);
                b.Box(windowMat, new Vector3(x, 7.8f, 2.15f), new Vector3(1.05f, .62f, .05f), null, false);
            }
            // Ojos de buey en el casco: la mayoría a oscuras, como quien no quiere que se le vea.
            foreach (var z in new[] { -40f, -35f, -27f, -22f, -12f, -2f, 8f, 16f })
                foreach (var side in new[] { -1f, 1f })
                {
                    var p = new Vector3(side * Beam(z) * .992f, 5.15f, z);
                    b.Pipe(darkMetal, p, p + Vector3.right * side * .05f, .24f, 12);
                    b.Pipe(z == -35f || z == -12f ? windowMat : rubber, p + Vector3.right * side * .05f, p + Vector3.right * side * .07f, .18f, 12);
                }

            // Marcos de puerta exteriores.
            DoorFrameX(b, -20.12f, 0, 6);
            DoorFrameX(b, 2.12f, 0, 6);
            DoorFrameX(b, -3.88f, 3, 9.1f);

            // Lanchas de salvamento en sus pescantes.
            foreach (var x in new[] { -3.4f, 3.4f })
            {
                b.Ellipsoid(orange, new Vector3(x, 10.25f, -17), new Vector3(.75f, .55f, 2.4f));
                b.Ellipsoid(super, new Vector3(x, 10.6f, -17), new Vector3(.6f, .35f, 1.7f));
                foreach (var z in new[] { -18.4f, -15.6f })
                {
                    b.Pipe(darkMetal, new Vector3(x, 9.1f, z), new Vector3(x, 11, z), .06f, 8);
                    b.Pipe(darkMetal, new Vector3(x, 11, z), new Vector3(x, 10.7f, z), .05f, 6);
                }
            }

            // Ventiladores y antenas sobre el techo.
            foreach (var (x, z) in new[] { (-4.2f, -6f), (4.2f, -1f), (-4.2f, -1f) })
            {
                var p = new Vector3(x, 9.1f, z);
                b.Pipe(super, p, p + Vector3.up * .9f, .2f, 10);
                b.Ellipsoid(super, p + Vector3.up * 1f, new Vector3(.34f, .2f, .34f));
            }
            b.Pipe(darkMetal, new Vector3(3, 12.1f, -12), new Vector3(3, 15.5f, -12), .025f, 5);
            b.Pipe(darkMetal, new Vector3(-3, 12.1f, -12), new Vector3(-3, 14.6f, -12), .025f, 5);
            b.Box(darkMetal, new Vector3(0, 12.3f, -6f), new Vector3(5.6f, .3f, .08f), null, false); // visera de las ventanas del puente

            // Luces de navegación: apagadas no estarían; un pesquero ilegal apenas las lleva.
            b.Ellipsoid(navRed, new Vector3(-4.25f, 11.6f, -4.3f), Vector3.one * .1f);
            b.Ellipsoid(navGreen, new Vector3(4.25f, 11.6f, -4.3f), Vector3.one * .1f);
            b.Ellipsoid(navWhite, new Vector3(0, 15.65f, -9), Vector3.one * .09f);

            // Radar: gira, le da vida al puente.
            var radar = new GameObject("Radar");
            radar.transform.SetParent(b.root.transform, false);
            radar.transform.localPosition = new Vector3(0, 13.2f, -9);
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
            bar.transform.SetParent(radar.transform, false);
            bar.transform.localScale = new Vector3(1.9f, .1f, .18f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = darkMetal;
            radar.AddComponent<Spin>().degreesPerSecond = 90;
        }

        static void DoorFrameX(ShipBuild b, float z, float x, float y)
        {
            foreach (var s in new[] { -.76f, .76f }) b.Box(darkMetal, new Vector3(x + s, y + 1.12f, z), new Vector3(.13f, 2.3f, .05f), null, false);
            b.Box(darkMetal, new Vector3(x, y + 2.27f, z), new Vector3(1.65f, .13f, .05f), null, false);
        }

        static void DoorFrameZ(ShipBuild b, float x, float z, float y)
        {
            foreach (var s in new[] { -.76f, .76f }) b.Box(darkMetal, new Vector3(x, y + 1.12f, z + s), new Vector3(.26f, 2.3f, .13f), null, false);
            b.Box(darkMetal, new Vector3(x, y + 2.27f, z), new Vector3(.26f, .13f, 1.65f), null, false);
        }

        static void Interior(ShipBuild b)
        {
            // Pasillo inferior: tuberías y bandeja de cables por el techo, con sus abrazaderas.
            void Line(Material m, float x, float y, float r)
            {
                b.Pipe(m, new Vector3(x, y, -29), new Vector3(x, y, 20.5f), r, 8);
                for (float z = -27; z < 20; z += 5f) b.Box(darkMetal, new Vector3(x, y, z), new Vector3(r * 2.6f, r * 2.6f, .05f), null, false);
            }
            Line(pipeBlue, -.95f, 5.72f, .06f);
            Line(rust, -.72f, 5.76f, .045f);
            Line(pipeGreen, -.5f, 5.72f, .05f);
            b.Box(darkMetal, new Vector3(.86f, 5.74f, -4.5f), new Vector3(.32f, .04f, 50), null, false);
            foreach (var x in new[] { .78f, .86f, .95f }) b.Box(rubber, new Vector3(x, 5.78f, -4.5f), new Vector3(.03f, .03f, 49.8f), null, false);

            // Válvulas, cuadros eléctricos y extintores.
            foreach (var z in new[] { -19f, -4f, 11f })
            {
                b.Pipe(redPaint, new Vector3(-1.12f, 5.28f, z), new Vector3(-1.05f, 5.28f, z), .12f, 12);
                b.Pipe(pipeGreen, new Vector3(-1.15f, 5.28f, z), new Vector3(-.5f, 5.28f, z), .025f, 6);
            }
            foreach (var (x, z) in new[] { (1.13f, -25f), (-1.13f, -14f), (1.13f, 8f) })
            {
                b.Box(super, new Vector3(x, 4.7f, z), new Vector3(.1f, .6f, .42f), null, false);
                b.Box(screenMat, new Vector3(x - Mathf.Sign(x) * .055f, 4.86f, z), new Vector3(.01f, .06f, .06f), null, false);
                b.Pipe(rubber, new Vector3(x, 5, z), new Vector3(x, 5.72f, z), .015f, 5);
            }
            foreach (var (x, z) in new[] { (1.1f, -8.5f), (-1.1f, 7f), (-1.1f, -24f) })
            {
                b.Pipe(redPaint, new Vector3(x, 3.5f, z), new Vector3(x, 4.05f, z), .08f, 10);
                b.Ellipsoid(redPaint, new Vector3(x, 4.05f, z), Vector3.one * .08f);
            }
            foreach (var z in new[] { -21f, -6f, 12f }) DoorFrameZ(b, 1.25f, z, 3);
            foreach (var z in new[] { -20f, -10f, 2f, 16f }) DoorFrameZ(b, -1.25f, z, 3);
            foreach (var z in new[] { -16f, -7f }) { DoorFrameZ(b, 1.25f, z, 6); DoorFrameZ(b, -1.25f, z, 6); }

            // Máquinas: tuberías, volantes y manómetros de los compresores.
            foreach (var z in new[] { -24.5f, -17.5f })
            {
                b.Pipe(darkMetal, new Vector3(4.3f, 5.2f, z - .8f), new Vector3(4.3f, 5.2f, z + .8f), .42f, 14);
                b.Pipe(pipeGreen, new Vector3(4.3f, 5.62f, z), new Vector3(4.3f, 5.9f, z), .09f, 8);
                b.Pipe(steel, new Vector3(2.75f, 4.3f, z), new Vector3(2.6f, 4.3f, z), .38f, 16);
                b.Box(cyanGlow, new Vector3(2.79f, 4.85f, z + .9f), new Vector3(.02f, .13f, .13f), null, false);
            }
            b.Pipe(pipeBlue, new Vector3(3, 5.6f, -27.5f), new Vector3(3, 5.6f, -14.5f), .07f, 8);
            b.Pipe(pipeGreen, new Vector3(6.3f, 5.65f, -27.5f), new Vector3(6.3f, 5.65f, -14.5f), .08f, 8);

            // K4: colchonetas gastadas y un bidón de agua. La escena se entiende por las huellas, sin más.
            foreach (var z in new[] { -4f, -1f, 2f, 5f, 8f })
            {
                b.Box(mattress, new Vector3(-5.9f, 3.71f, z), new Vector3(1.45f, .1f, 2f), new Vector3(0, (z * 13) % 4 - 2, 0), false);
                b.Box(mattress, new Vector3(-5.9f, 4.91f, z), new Vector3(1.45f, .1f, 2f), new Vector3(0, (z * 7) % 4 - 2, 0), false);
                b.Pipe(darkMetal, new Vector3(-5.15f, 3, z - 1.05f), new Vector3(-5.15f, 5.3f, z - 1.05f), .035f, 6);
            }
            b.Pipe(pipeBlue, new Vector3(-2.2f, 3.6f, 8.5f), new Vector3(-2.2f, 4.05f, 8.5f), .15f, 10);

            // Habilitación: literas con colchón, banquetas, carteles y lámparas.
            foreach (var z in new[] { -10f, -6.5f, -3f, .5f }) b.Box(mattress, new Vector3(4.2f, 7.84f, z), new Vector3(1.2f, .08f, 2f), null, false);
            foreach (var z in new[] { -6.8f, -5f, -3.2f })
            {
                b.Pipe(darkMetal, new Vector3(-1.9f, 6, z), new Vector3(-1.9f, 6.45f, z), .03f, 6);
                b.Pipe(crate, new Vector3(-1.9f, 6.45f, z), new Vector3(-1.9f, 6.5f, z), .17f, 10);
            }
            b.Box(yellow, new Vector3(1.14f, 7.5f, -11f), new Vector3(.02f, .5f, .36f), null, false);
            b.Box(super, new Vector3(-1.14f, 7.4f, -3.4f), new Vector3(.02f, .6f, .9f), null, false);

            // Puente: pantallas, timón, silla y mesa de cartas.
            b.Box(screenMat, new Vector3(-1.3f, 10.4f, -4.7f), new Vector3(.75f, .45f, .03f), new Vector3(-15, 0, 0), false);
            b.Box(cyanGlow, new Vector3(1.3f, 10.4f, -4.7f), new Vector3(.75f, .45f, .03f), new Vector3(-15, 0, 0), false);
            b.Pipe(darkMetal, new Vector3(0, 9.1f, -6.5f), new Vector3(0, 9.65f, -6.5f), .04f, 8);
            b.Pipe(rubber, new Vector3(0, 9.65f, -6.5f), new Vector3(0, 9.73f, -6.5f), .25f, 12);
            b.Box(rubber, new Vector3(0, 10, -6.72f), new Vector3(.5f, .55f, .07f), null, false);
            b.Box(super, new Vector3(-2.6f, 10.01f, -11.8f), new Vector3(1.4f, .01f, 1f), null, false);
        }

        static void LightCones(ShipBuild b)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{GenDir}/LightCone.asset");
            if (mesh == null)
            {
                const int segs = 24;
                var v = new Vector3[segs * 2]; var c = new Color[segs * 2]; var uv = new Vector2[segs * 2]; var t = new int[segs * 6];
                for (int i = 0; i < segs; i++)
                {
                    float a = i * Mathf.PI * 2 / segs;
                    v[i] = new Vector3(Mathf.Cos(a) * .06f, 0, Mathf.Sin(a) * .06f); c[i] = Color.white;
                    v[segs + i] = new Vector3(Mathf.Cos(a), -1, Mathf.Sin(a)); c[segs + i] = new Color(1, 1, 1, 0);
                    int n = (i + 1) % segs;
                    t[i * 6] = i; t[i * 6 + 1] = n; t[i * 6 + 2] = segs + i;
                    t[i * 6 + 3] = n; t[i * 6 + 4] = segs + n; t[i * 6 + 5] = segs + i;
                }
                mesh = new Mesh { name = "LightCone", vertices = v, colors = c, uv = uv, triangles = t };
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, $"{GenDir}/LightCone.asset");
            }
            var mat = GlowAsset("M_LightCone", new Color(1f, .68f, .3f, .008f), true, false);
            void Cone(Vector3 top, float height, float radius, Vector3 euler)
            {
                foreach (var k in new[] { 1f, .72f, .45f }) // anidados: el borde del haz se difumina
                {
                    var go = new GameObject("Haz", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(b.root.transform, false);
                    go.transform.SetLocalPositionAndRotation(top, Quaternion.Euler(euler));
                    go.transform.localScale = new Vector3(radius * k, height, radius * k);
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.GetComponent<MeshRenderer>();
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
            }
            Cone(new Vector3(0, 9.4f, -32), 3.4f, 5.5f, Vector3.zero);
            Cone(new Vector3(0, 9.8f, 12), 3.8f, 5.5f, Vector3.zero);
            Cone(new Vector3(0, 9.8f, 30), 3.8f, 5f, Vector3.zero);
            Cone(new Vector3(-6.2f, 8.6f, -9), 2.6f, 3f, Vector3.zero);
            Cone(new Vector3(0, 12.5f, -2.5f), 8f, 5.6f, new Vector3(-18, 0, 0)); // el foco del mástil apunta 72° hacia proa
        }

        static void Funnel(ShipBuild b)
        {
            b.Pipe(super, new Vector3(0, 9.1f, -17), new Vector3(0, 11.8f, -17), .68f, 16);
            b.Pipe(redPaint, new Vector3(0, 11.2f, -17), new Vector3(0, 11.65f, -17), .695f, 16);
            b.Pipe(darkMetal, new Vector3(0, 11.65f, -17), new Vector3(0, 12.3f, -17), .7f, 16);

            var go = new GameObject("Humo");
            go.transform.SetParent(b.root.transform, false);
            go.transform.localPosition = new Vector3(0, 12.4f, -17);
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.loop = true; main.startLifetime = 7f; main.startSpeed = 1.5f; main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startColor = new Color(.5f, .52f, .55f, .14f); main.maxParticles = 80; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission; em.rateOverTime = 7;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = .4f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.8f, 2.6f); vel.y = new ParticleSystem.MinMaxCurve(.3f, .6f); vel.z = new ParticleSystem.MinMaxCurve(-.4f, .4f);
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .5f, 1, 2.6f));
            var color = ps.colorOverLifetime; color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .2f), new GradientAlphaKey(0, 1) });
            color.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = GlowAsset("M_Smoke", Color.white, false, true);
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void ShipName(ShipBuild b, string text, Vector3 pos, Quaternion rot, float size)
        {
            var go = new GameObject("Nombre");
            go.transform.SetParent(b.root.transform, false);
            go.transform.SetLocalPositionAndRotation(pos, rot);
            var t = go.AddComponent<TextMeshPro>();
            t.text = text; t.fontSize = size * 10f; t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(.78f, .76f, .68f, .8f);
            t.fontStyle = FontStyles.Bold; t.characterSpacing = 8;
            t.rectTransform.sizeDelta = new Vector2(16, 3);
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // ---------- Lancha: semirrígida con flotadores, consola y arco de radar ----------
        static GameObject boatPrefab;

        static GameObject BoatPrefab()
        {
            if (boatPrefab != null) return boatPrefab;
            var b = new ShipBuild { root = new GameObject("Lancha") };
            b.colliders = new GameObject("c").transform;
            b.colliders.SetParent(b.root.transform, false);
            foreach (var x in new[] { -1.02f, 1.02f })
            {
                b.Ellipsoid(rubber, new Vector3(x, .42f, -.4f), new Vector3(.38f, .38f, 3f), 8, 18);
                b.Ellipsoid(orange, new Vector3(x, .46f, -3.1f), new Vector3(.3f, .3f, .35f)); // cono de popa, visible de noche
                b.Pipe(darkMetal, new Vector3(x * 1.3f, .42f, -2.6f), new Vector3(x * 1.3f, .42f, 2f), .012f, 4);
            }
            b.Ellipsoid(rubber, new Vector3(0, .46f, 2.5f), new Vector3(1.05f, .4f, 1.2f), 8, 18);
            b.Box(darkMetal, new Vector3(0, .34f, -.6f), new Vector3(1.75f, .42f, 5.2f), null, false);
            b.Box(suit, new Vector3(0, .57f, -.6f), new Vector3(1.6f, .05f, 5f), null, false);
            b.Box(darkMetal, new Vector3(0, .98f, .5f), new Vector3(.95f, .85f, .75f), null, false);
            b.Box(windowMat, new Vector3(0, 1.62f, .78f), new Vector3(.9f, .46f, .04f), new Vector3(-22, 0, 0), false);
            b.Box(cyanGlow, new Vector3(0, 1.32f, .35f), new Vector3(.6f, .2f, .02f), new Vector3(30, 0, 0), false);
            b.Pipe(darkMetal, new Vector3(0, .85f, -.4f), new Vector3(0, 1.1f, -.4f), .05f, 8);
            b.Box(rubber, new Vector3(0, 1.15f, -.4f), new Vector3(.75f, .14f, .55f), null, false);
            foreach (var x in new[] { -.75f, .75f })
            {
                b.Pipe(darkMetal, new Vector3(x, .6f, -1.35f), new Vector3(x * .8f, 2.25f, -1.1f), .04f, 8);
                b.Pipe(darkMetal, new Vector3(x, .6f, -.85f), new Vector3(x * .8f, 2.25f, -1.1f), .04f, 8);
            }
            b.Pipe(darkMetal, new Vector3(-.6f, 2.25f, -1.1f), new Vector3(.6f, 2.25f, -1.1f), .04f, 8);
            b.Ellipsoid(super, new Vector3(0, 2.42f, -1.1f), new Vector3(.3f, .16f, .3f));
            b.Pipe(darkMetal, new Vector3(-.45f, 2.25f, -1.1f), new Vector3(-.45f, 3.3f, -1.1f), .012f, 4);
            b.Box(rubber, new Vector3(0, .75f, -3.15f), new Vector3(.5f, .75f, .55f), null, false); // motor
            b.Box(darkMetal, new Vector3(0, .12f, -3.3f), new Vector3(.18f, .6f, .18f), null, false);
            b.Ellipsoid(navRed, new Vector3(-.8f, .95f, 2.1f), Vector3.one * .045f);
            b.Ellipsoid(navGreen, new Vector3(.8f, .95f, 2.1f), Vector3.one * .045f);
            b.Ellipsoid(cyanGlow, new Vector3(-.45f, 3.32f, -1.1f), Vector3.one * .03f);
            Combine(b, "Lancha");
            UnityEngine.Object.DestroyImmediate(b.colliders.gameObject);
            boatPrefab = PrefabUtility.SaveAsPrefabAsset(b.root, $"{Root}/Prefabs/Lancha.prefab");
            UnityEngine.Object.DestroyImmediate(b.root);
            return boatPrefab;
        }

        // ---------- La flota: poteros encendidos en el horizonte, como en la milla 201 ----------
        static Mesh fleetMesh;

        static void Fleet()
        {
            if (fleetMesh == null)
            {
                fleetMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{GenDir}/FleetLights.asset");
                if (fleetMesh == null)
                {
                    var v = new System.Collections.Generic.List<Vector3>(); var c = new System.Collections.Generic.List<Color>();
                    var uv = new System.Collections.Generic.List<Vector2>(); var t = new System.Collections.Generic.List<int>();
                    void Quad(Vector3 center, float w, float h, float alpha)
                    {
                        int i = v.Count;
                        v.Add(center + new Vector3(-w, -h)); v.Add(center + new Vector3(w, -h)); v.Add(center + new Vector3(w, h)); v.Add(center + new Vector3(-w, h));
                        uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
                        for (int k = 0; k < 4; k++) c.Add(new Color(1, 1, 1, alpha));
                        t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
                    }
                    for (float x = -32; x <= 32; x += 5.3f) Quad(new Vector3(x, 15, 0), 3.2f, 3.2f, 1f);
                    Quad(new Vector3(0, 15, 0), 55, 14, .05f); // resplandor
                    fleetMesh = new Mesh { name = "FleetLights" };
                    fleetMesh.SetVertices(v); fleetMesh.SetColors(c); fleetMesh.SetUVs(0, uv); fleetMesh.SetTriangles(t, 0);
                    fleetMesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(fleetMesh, $"{GenDir}/FleetLights.asset");
                }
            }
            var mat = GlowAsset("M_FleetLights", new Color(2.4f, 2.1f, 1.5f, 1f), true, true);
            var root = new GameObject("Flota lejana").transform;
            foreach (var (deg, dist) in new[] { (28f, 1500f), (70f, 1150f), (115f, 1750f), (165f, 1300f), (205f, 1600f), (250f, 1050f), (300f, 1450f), (340f, 1250f) })
            {
                float a = deg * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a) * dist, 0, Mathf.Cos(a) * dist);
                var go = new GameObject("Potero", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos));
                go.GetComponent<MeshFilter>().sharedMesh = fleetMesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }
        }

        // ---------- Tratamiento de imagen ----------
        static void GradePost(VolumeProfile p)
        {
            var smh = p.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(.86f, 1f, 1.1f, -.02f));   // sombras frías, tirando a cian
            smh.highlights.Override(new Vector4(1.08f, 1f, .9f, 0f));   // luces cálidas
            var grain = p.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1); grain.intensity.Override(.16f); grain.response.Override(.8f);
        }
    }
}
