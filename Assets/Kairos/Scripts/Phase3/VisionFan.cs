using UnityEngine;

namespace Kairos
{
    /// Cono de visión pintado en el suelo: recorta contra mamparos. Se desvanece desde el guardia y marca
    /// con una línea dónde acaba su vista. Solo se ven los de guardias cercanos y de tu misma cubierta:
    /// antes eran losas cian uniformes (mezcla aditiva) por todo el barco y no se entendía qué eran.
    public class VisionFan : MonoBehaviour
    {
        const int Rays = 24;
        const float RimStart = .9f;                // la franja del borde ocupa el último 10 %
        const float ShowNear = 14f, HideFar = 24f; // distancia al jugador: visible del todo / ya no se ve
        Mesh mesh; Material mat; MeshRenderer rend; int frame;
        Vector3[] verts; Color tint = Color.white;
        public float range = 10, angle = 90;

        public static VisionFan Create(Transform parent, float y, float range, float angle)
        {
            var go = new GameObject("ConoVision", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, y, 0);
            var f = go.AddComponent<VisionFan>();
            f.range = range; f.angle = angle;

            // Vértices: 0 = guardia; luego el anillo interior (inicio del borde) y el exterior (fin de la vista).
            int ring = Rays + 1;
            f.verts = new Vector3[1 + ring * 2];
            var cols = new Color[f.verts.Length];
            cols[0] = new Color(1, 1, 1, .26f);
            for (int i = 0; i < ring; i++) { cols[1 + i] = new Color(1, 1, 1, .05f); cols[1 + ring + i] = new Color(1, 1, 1, .45f); }
            var tris = new int[Rays * 9];
            for (int i = 0; i < Rays; i++)
            {
                int inA = 1 + i, inB = 2 + i, outA = 1 + ring + i, outB = 2 + ring + i, t = i * 9;
                tris[t] = 0; tris[t + 1] = inA; tris[t + 2] = inB;                    // relleno que se desvanece
                tris[t + 3] = inA; tris[t + 4] = outA; tris[t + 5] = outB;            // franja del borde
                tris[t + 6] = inA; tris[t + 7] = outB; tris[t + 8] = inB;
            }
            f.mesh = new Mesh { name = "Fan" };
            f.mesh.vertices = f.verts; f.mesh.colors = cols; f.mesh.triangles = tris;
            go.GetComponent<MeshFilter>().sharedMesh = f.mesh;
            f.mat = Mats.Glow(Color.white, additive: false);
            f.rend = go.GetComponent<MeshRenderer>();
            f.rend.sharedMaterial = f.mat;
            f.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return f;
        }

        public void SetColor(Color c) => tint = c;

        void LateUpdate()
        {
            float fade = Fade();
            rend.enabled = fade > .01f;
            mat.SetColor("_Color", new Color(tint.r, tint.g, tint.b, fade));
            if (!rend.enabled || ++frame % 2 != 0) return;

            var origin = transform.position;
            verts[0] = Vector3.zero;
            int ring = Rays + 1;
            for (int i = 0; i <= Rays; i++)
            {
                float a = -angle / 2 + angle * i / Rays;
                var localDir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var dir = transform.TransformDirection(localDir);
                float d = range;
                if (Physics.Raycast(origin + Vector3.up * .5f, dir, out var hit, range, Layers.SolidMask)) d = hit.distance;
                d = FloorEnds(origin, dir, d);
                verts[1 + i] = localDir * (d * RimStart);
                verts[1 + ring + i] = localDir * d;
            }
            mesh.vertices = verts;
            mesh.RecalculateBounds();
        }

        /// Corta el rayo donde se acaba el suelo: sin esto el cono quedaba flotando sobre el hueco de las rampas.
        static float FloorEnds(Vector3 origin, Vector3 dir, float d)
        {
            for (float s = .6f; s < d; s += .5f)
                if (!Physics.Raycast(origin + dir * s + Vector3.up * .3f, Vector3.down, .7f, Layers.SolidMask))
                    return Mathf.Max(0, s - .25f);
            return d;
        }

        /// Visible cerca del jugador y en su misma cubierta; se apaga con la distancia.
        float Fade()
        {
            var I = InfiltrationController.I;
            if (I == null || I.player == null) return 1f;
            var to = I.player.transform.position - transform.position;
            if (Mathf.Abs(to.y) > 2.2f) return 0f;
            to.y = 0;
            return 1f - Mathf.InverseLerp(ShowNear, HideFar, to.magnitude);
        }

        void OnDestroy()
        {
            if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); mesh = null; }
            if (mat != null) { if (Application.isPlaying) Destroy(mat); else DestroyImmediate(mat); mat = null; }
        }
    }
}
