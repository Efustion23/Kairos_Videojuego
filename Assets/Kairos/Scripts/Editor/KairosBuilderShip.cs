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
    public static partial class KairosBuilder
    {
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

            /// Coloca un objeto de Models/Props con su base en 'at'. La mayor de sus medidas pasa a valer 'size'.
            public Transform Prop(string id, Vector3 at, float yaw, float size)
            {
                var t = NewProp(id, at, yaw, out var b);
                t.localScale = Vector3.one * (size / Mathf.Max(b.x, b.y, b.z));
                return t;
            }

            /// Igual, pero ajustado a una caja: para que encaje con su colisión o sostenga una prueba a la altura exacta.
            public Transform PropFit(string id, Vector3 at, float yaw, Vector3 box)
            {
                var t = NewProp(id, at, yaw, out var b);
                t.localScale = new Vector3(box.x / b.x, box.y / b.y, box.z / b.z);
                return t;
            }

            Transform NewProp(string id, Vector3 at, float yaw, out Vector3 size)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(PropPrefab(id), root.transform);
                var bounds = new Bounds(go.transform.position, Vector3.zero);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) bounds.Encapsulate(r.bounds);
                size = bounds.size;
                go.transform.SetLocalPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
                return go.transform;
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

        /// Barcos de la Fase 3. Comparten el esqueleto (pasillos, celdas, habilitación y puente) para que la IA,
        /// las puertas y las pruebas funcionen igual; cambia el aparejo de cubierta según la pesquería.
        /// 0: Hai Feng 77 (potero) · 1: Kestrel Explorer (arrastrero) · 2: Oceanic Crown 9 (palangrero)
        static readonly string[] ShipPrefix = { "HaiFeng", "Kestrel", "Crown" };
        static readonly string[] ShipPrefabName = { "HaiFeng77", "KestrelExplorer", "OceanicCrown9" };

        /// Pintura de cada barco (multiplica la textura): casco sobre la flotación y superestructura.
        /// Hai Feng: óxido rojo y gris sucio · Kestrel: casco azul marino y superestructura blanca · Crown: casco negro y crema.
        static readonly Color[] HullTint = { Color.white, new Color(.8f, 1.6f, 3.4f), new Color(.5f, .5f, .58f) };
        static readonly Color[] SuperTint = { Color.white, new Color(1.95f, 1.95f, 1.9f), new Color(2.05f, 1.9f, 1.5f) };

        /// Copia de un material con otro tinte, guardada junto a los generados (M_Super_Kestrel...).
        static Material Variant(Material src, string suffix, Color tint)
        {
            string path = $"{GenDir}/{src.name}_{suffix}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(src);
            m.SetColor("_BaseColor", src.GetColor("_BaseColor") * tint);
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject BuildShip(int kind = 0)
        {
            string prefix = ShipPrefix[kind];
            // Cada barco con su pintura: durante su construcción 'super' y 'rust' apuntan a sus variantes.
            var (super0, rust0) = (super, rust);
            if (kind > 0) { super = Variant(super0, prefix, SuperTint[kind]); rust = Variant(rust0, prefix, HullTint[kind]); }
            // La raíz se llama siempre HaiFeng77: SectorTheme y los rótulos la buscan por ese nombre.
            var b = new ShipBuild { root = new GameObject("HaiFeng77") };
            b.colliders = new GameObject("Colliders").transform;
            b.colliders.SetParent(b.root.transform, false);
            var amber = Pal.DeckLight;

            // --- Casco y cubierta ---
            var hullGo = new GameObject("Hull", typeof(MeshFilter), typeof(MeshRenderer)) { layer = Layers.HullShell };
            hullGo.transform.SetParent(b.root.transform, false);
            var hm = HullMesh();
            AssetDatabase.CreateAsset(hm, $"{GenDir}/{prefix}_Hull.asset");
            hullGo.GetComponent<MeshFilter>().sharedMesh = hm;
            hullGo.GetComponent<MeshRenderer>().sharedMaterials = new[] { rust, paint };
            hullGo.AddComponent<MeshCollider>().sharedMesh = hm;

            var deckGo = new GameObject("Deck", typeof(MeshFilter), typeof(MeshRenderer)) { layer = Layers.ShipSolid };
            deckGo.transform.SetParent(b.root.transform, false);
            var dm = DeckMesh();
            AssetDatabase.CreateAsset(dm, $"{GenDir}/{prefix}_Deck.asset");
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
            b.Solid(new Vector3(-3.5f, 6.45f, -18.8f), new Vector3(2.4f, .9f, 1f));        // camarote capitán
            b.PropFit("metal_office_desk", new Vector3(-3.5f, 6, -18.8f), 0, new Vector3(2.4f, .9f, 1f));
            b.Box(darkMetal, new Vector3(-4.4f, 6.8f, -14f), new Vector3(.8f, 1.6f, 1f));  // caja fuerte
            b.Prop("old_bed_frame", new Vector3(-3.9f, 6, -12.6f), 90, 2f);                 // cama del capitán, contra el mamparo
            b.Solid(new Vector3(-3.9f, 6.3f, -12.6f), new Vector3(2f, .6f, .9f));
            b.Solid(new Vector3(3.5f, 6.45f, -18.8f), new Vector3(2.4f, .9f, 1f));         // camarote oficial
            b.PropFit("metal_office_desk", new Vector3(3.5f, 6, -18.8f), 0, new Vector3(2.4f, .9f, 1f));
            b.Solid(new Vector3(-3.4f, 6.4f, -5f), new Vector3(1.6f, .8f, 5f));            // comedor: deja paso junto a la puerta
            foreach (var z in new[] { -6.25f, -3.75f }) b.PropFit("dining_table", new Vector3(-3.4f, 6, z), 90, new Vector3(2.5f, .8f, 1.6f));
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

            // Proa: tapas de bodega y cajas (las mismas coberturas en los tres barcos).
            b.Box(rust, new Vector3(-3.6f, 6.45f, 10), new Vector3(3.6f, .9f, 6));
            b.Box(rust, new Vector3(3.6f, 6.45f, 16), new Vector3(3.6f, .9f, 6));
            b.Box(crate, new Vector3(-3.8f, 6.7f, 19), new Vector3(2.2f, 1.4f, 2.4f));
            b.Box(crate, new Vector3(3.8f, 6.7f, 6.5f), new Vector3(2.2f, 1.4f, 2.4f));
            b.Box(crate, new Vector3(3f, 6.6f, 31), new Vector3(2.2f, 1.2f, 2.2f));
            b.Box(crate, new Vector3(-2.5f, 6.6f, 34), new Vector3(2f, 1.2f, 2f));
            if (kind == 0) PoteroGear(b);
            else if (kind == 1) TrawlerGear(b);
            else LonglinerGear(b);
            ShipIdentity(b, kind);

            // Focos de cubierta: zonas de peligro. El color avisa.
            // Usamos Point sin sombras para el relleno + Spot cenital con sombras (1 mapa vs 6 en URP) para evitar downsampling del atlas.
            b.Lamp("Foco Popa", new Vector3(0, 9.4f, -32), amber, 18, 5);
            b.Spot("Foco Popa Haz", new Vector3(0, 9.35f, -32), new Vector3(90, 0, 0), amber, 18, 115, 18);
            b.Lamp("Foco Proa 1", new Vector3(0, 9.8f, 12), amber, 16, 8);
            b.Lamp("Foco Proa 2", new Vector3(0, 9.8f, 30), amber, 15, 5);
            b.Spot("Foco Proa 2 Haz", new Vector3(0, 9.75f, 30), new Vector3(90, 0, 0), amber, 16, 115, 18);
            b.Lamp("Foco Babor", new Vector3(-6.2f, 8.6f, -9), amber, 8, 3.5f);
            b.Lamp("Luz Tejado", new Vector3(5.8f, 11f, -3), amber, 8, 3.5f);
            b.Spot("Foco Mastil", new Vector3(0, 12.5f, -2.5f), new Vector3(72, 0, 0), amber, 40, 70, 12);

            ShipDetails(b, kind == 0 ? redPaint : kind == 1 ? orange : yellow);
            Combine(b, prefix); // una malla por material, como un único asset cada una

            var prefabPath = $"{Root}/Prefabs/{ShipPrefabName[kind]}.prefab";
            PrefabUtility.SaveAsPrefabAsset(b.root, prefabPath);
            Object.DestroyImmediate(b.root);
            (super, rust) = (super0, rust0);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        /// Potero (Sector 1): racks de luces sobre la proa para atraer al calamar.
        static void PoteroGear(ShipBuild b)
        {
            for (float z = -18; z <= 36; z += 9)
            {
                if (z < 3) continue;
                b.Box(darkMetal, new Vector3(0, 8.2f, z), new Vector3(.22f, 4.4f, .22f));
                b.Box(darkMetal, new Vector3(0, 10.3f, z), new Vector3(Mathf.Min(9, Beam(z) * 1.6f), .14f, .14f), null, false);
                for (float x = -3.6f; x <= 3.61f; x += 1.2f)
                    if (Mathf.Abs(x) < Beam(z) * .78f) b.Box(bulb, new Vector3(x, 10.15f, z), new Vector3(.3f, .18f, .3f), null, false);
            }
        }

        /// Arrastrero (Sector 2): pórtico en A a popa con el tambor de red y las puertas de arrastre, cables al agua
        /// y maquinillas junto a la habilitación. Todo por encima o a los lados: la cubierta se recorre igual que en el Hai Feng.
        static void TrawlerGear(ShipBuild b)
        {
            foreach (var x in new[] { -5.6f, 5.6f })
            {
                b.Box(orange, new Vector3(x, 9.8f, -43.2f), new Vector3(.45f, 7.6f, .45f), new Vector3(0, 0, x > 0 ? -6 : 6), false); // patas del pórtico
                b.Box(rust, new Vector3(x * 1.17f, 8.1f, -43.8f), new Vector3(.25f, 2.4f, 3.4f), new Vector3(0, 0, x > 0 ? 12 : -12), false); // puertas de arrastre
                b.Pipe(darkMetal, new Vector3(x * .9f, 13.4f, -43.2f), new Vector3(x * .6f, 1f, -50f), .04f, 4); // cables al agua
            }
            b.Box(orange, new Vector3(0, 13.6f, -43.2f), new Vector3(11.8f, .55f, .55f), null, false); // travesaño
            b.Pipe(netMat, new Vector3(-3.4f, 11.6f, -42.4f), new Vector3(3.4f, 11.6f, -42.4f), 1.1f, 14); // tambor con la red
            b.Pipe(darkMetal, new Vector3(-3.7f, 11.6f, -42.4f), new Vector3(3.7f, 11.6f, -42.4f), .18f, 8);
            foreach (var x in new[] { -2.3f, 2.3f })
            {
                b.Box(darkMetal, new Vector3(x, 6.55f, -22.6f), new Vector3(1.2f, 1.1f, 1.6f)); // maquinillas de arrastre
                b.Pipe(rope, new Vector3(x, 7.2f, -23.3f), new Vector3(x, 7.2f, -21.9f), .42f, 10);
            }
            b.Box(netMat, new Vector3(-5.6f, 6.35f, 14f), new Vector3(1.4f, .7f, 5f), null, false); // red de repuesto junto a la borda
            b.Box(rust, new Vector3(0, 8.6f, 24f), new Vector3(.22f, 5.2f, .22f), null, false);   // mástil de proa
        }

        /// Palangrero (Sector 3): halador de línea a estribor, mesas de carnada, cajas de anzuelos y boyas de palangre.
        static void LonglinerGear(ShipBuild b)
        {
            b.Box(darkMetal, new Vector3(6.1f, 6.9f, 9f), new Vector3(.9f, 1.8f, 1f));                       // halador (fuera de las rondas)
            b.Pipe(steel, new Vector3(6.1f, 7.9f, 8.5f), new Vector3(6.1f, 7.9f, 9.5f), .38f, 12);               // rodillo
            b.Pipe(rope, new Vector3(6.4f, 7.9f, 9f), new Vector3(7.2f, 5f, 9f), .03f, 4);                // línea al agua
            foreach (var z in new[] { 22f, 26.5f })
            {
                b.Box(steel, new Vector3(-5.1f, 7f, z), new Vector3(1.1f, .08f, 3f), null, false);             // mesa de carnada
                b.Prop("fish_knife", new Vector3(-5f, 7.06f, z + .4f), 70, .22f).localRotation = Quaternion.Euler(90, 70, 0); // tumbado sobre la mesa
                b.Box(darkMetal, new Vector3(-5.1f, 6.48f, z), new Vector3(.9f, .95f, 2.6f));
            }
            for (float z = -12; z <= -2; z += 2.5f)
                b.Box(yellow, new Vector3(-6.05f, 6.3f, z), new Vector3(.7f, .6f, 1f), null, false);           // cajas de anzuelos
            for (float z = 8; z <= 30; z += 5.5f)
            {
                b.Ellipsoid(orange, new Vector3(5.9f, 7.2f, z), new Vector3(.32f, .32f, .32f));                // boyas de palangre
                b.Box(darkMetal, new Vector3(5.9f, 8.2f, z), new Vector3(.04f, 1.6f, .04f), null, false);       // banderolas
            }
            b.Box(darkMetal, new Vector3(0, 7.4f, -44.2f), new Vector3(.6f, .25f, 2.6f), new Vector3(-28, 0, 0), false); // tolva de calado
        }

        /// Franja de pintura por fuera del casco, justo bajo la borda, siguiendo su curva.
        static void HullStripe(ShipBuild b, Material m, float y, float h)
        {
            var st = Stations();
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < st.Length - 1; i++)
                {
                    var a = new Vector3(side * (Beam(st[i]) + .03f), y, st[i]); var c = new Vector3(side * (Beam(st[i + 1]) + .03f), y, st[i + 1]);
                    var d = c - a;
                    b.Box(m, (a + c) / 2, new Vector3(.04f, h, d.magnitude + .05f), new Vector3(0, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0), false);
                }
            b.Box(m, new Vector3(0, y, -45.03f), new Vector3(14.06f, h, .04f), null, false);
        }

        /// Lo que distingue a cada barco desde fuera, sin tocar lo que se pisa: todo va sobre la borda, en los
        /// techos o fuera de las rondas. El interior (pasillos, celdas, puente) es el mismo en los tres.
        static void ShipIdentity(ShipBuild b, int kind)
        {
            if (kind == 0)
            {
                // Potero: máquinas poteras a lo largo de la borda, con sus tambores y el brazo hacia el agua.
                for (float z = -14; z <= 36; z += 3.2f)
                    foreach (var side in new[] { -1f, 1f })
                    {
                        if (z > -21 && z < 3) continue; // junto a la habilitación no caben
                        float x = side * (Beam(z) - .05f);
                        b.Box(darkMetal, new Vector3(x, 7.45f, z), new Vector3(.3f, .32f, .5f), null, false);
                        b.Pipe(steel, new Vector3(x, 7.52f, z - .42f), new Vector3(x, 7.52f, z + .42f), .11f, 10);
                        b.Pipe(steel, new Vector3(x, 7.6f, z), new Vector3(side * (Beam(z) + .65f), 7.95f, z), .025f, 5);
                    }
                return;
            }

            if (kind == 1)
            {
                // Arrastrero: franja blanca, puente volante con barandilla y radomo, y flotadores en el pórtico.
                HullStripe(b, super, 5.85f, .3f);
                foreach (var (a, c) in new[] { (new Vector3(-4.1f, 12.9f, -14.1f), new Vector3(4.1f, 12.9f, -14.1f)), (new Vector3(-4.1f, 12.9f, -14.1f), new Vector3(-4.1f, 12.9f, -3.9f)), (new Vector3(4.1f, 12.9f, -14.1f), new Vector3(4.1f, 12.9f, -3.9f)) })
                {
                    b.Pipe(super, a, c, .04f, 6);
                    for (float t = 0; t <= 1.001f; t += .2f) { var p = Vector3.Lerp(a, c, t); b.Pipe(super, p + Vector3.down * .8f, p, .03f, 5); }
                }
                b.Pipe(super, new Vector3(2.6f, 12.1f, -12.8f), new Vector3(2.6f, 12.4f, -12.8f), .18f, 8);
                b.Ellipsoid(super, new Vector3(2.6f, 12.65f, -12.8f), new Vector3(.5f, .4f, .5f));
                for (float x = -4f; x <= 4.01f; x += 1.6f)
                {
                    b.Pipe(rope, new Vector3(x, 13.35f, -43.6f), new Vector3(x, 13.05f, -43.6f), .02f, 4);
                    b.Ellipsoid(orange, new Vector3(x, 12.85f, -43.6f), Vector3.one * .26f);
                }
                return;
            }

            // Palangrero: franja roja, palo trinquete con cofa, domos de satélite y techo sobre el halador.
            HullStripe(b, redPaint, 5.85f, .25f);
            b.Pipe(darkMetal, new Vector3(0, 6, 37.5f), new Vector3(0, 16.5f, 37.5f), .14f, 10);
            b.Solid(new Vector3(0, 7, 37.5f), new Vector3(.35f, 2, .35f));
            b.Box(darkMetal, new Vector3(0, 13.2f, 37.5f), new Vector3(1.1f, .08f, 1.1f), null, false);
            foreach (var (a, c) in new[] { (new Vector3(-.55f, 13.7f, 36.95f), new Vector3(.55f, 13.7f, 36.95f)), (new Vector3(-.55f, 13.7f, 38.05f), new Vector3(.55f, 13.7f, 38.05f)), (new Vector3(-.55f, 13.7f, 36.95f), new Vector3(-.55f, 13.7f, 38.05f)), (new Vector3(.55f, 13.7f, 36.95f), new Vector3(.55f, 13.7f, 38.05f)) })
                b.Pipe(darkMetal, a, c, .03f, 5);
            b.Box(darkMetal, new Vector3(0, 15.2f, 37.5f), new Vector3(3.4f, .12f, .12f), null, false);
            b.Ellipsoid(navWhite, new Vector3(0, 16.6f, 37.5f), Vector3.one * .09f);
            foreach (var (x, r) in new[] { (2.2f, .55f), (-2.2f, .42f) })
            {
                b.Pipe(super, new Vector3(x, 12.1f, -13.4f), new Vector3(x, 12.4f, -13.4f), .16f, 8);
                b.Ellipsoid(super, new Vector3(x, 12.4f + r * .9f, -13.4f), new Vector3(r, r * .95f, r));
            }
            b.Box(super, new Vector3(6.05f, 9.35f, 9), new Vector3(1.8f, .1f, 4.6f), null, false);
            foreach (var z in new[] { 6.9f, 11.1f })
            {
                b.Pipe(darkMetal, new Vector3(6.75f, 6, z), new Vector3(6.75f, 9.3f, z), .06f, 6);
                b.Solid(new Vector3(6.75f, 7.6f, z), new Vector3(.15f, 3.2f, .15f));
            }
        }
    }
}
