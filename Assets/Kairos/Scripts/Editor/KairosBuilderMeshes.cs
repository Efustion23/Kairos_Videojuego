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
    }
}
