using UnityEngine;
using UnityEngine.AI;

namespace Kairos
{
    public enum GuardState { Tranquilo, Sospecha, Busqueda, Alerta, Bloqueo }

    public static class Layers
    {
        public const int ShipSolid = 8, Actor = 9, Thermal = 10, Interact = 11, HullShell = 12;
        public const int SolidMask = 1 << ShipSolid;
    }

    /// Cono de visión pintado en el suelo: recorta contra mamparos.
    public class VisionFan : MonoBehaviour
    {
        const int Rays = 22;
        Mesh mesh; Material mat; Vector3[] verts = new Vector3[Rays + 2]; int frame;
        public float range = 10, angle = 90;

        public static VisionFan Create(Transform parent, float y, float range, float angle)
        {
            var go = new GameObject("ConoVision", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, y, 0);
            var f = go.AddComponent<VisionFan>();
            f.range = range; f.angle = angle;
            f.mesh = new Mesh { name = "Fan" };
            var tris = new int[Rays * 3];
            for (int i = 0; i < Rays; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }
            f.mesh.vertices = f.verts; f.mesh.triangles = tris;
            go.GetComponent<MeshFilter>().sharedMesh = f.mesh;
            f.mat = Mats.Glow(new Color(1, 1, 1, .15f));
            go.GetComponent<MeshRenderer>().sharedMaterial = f.mat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return f;
        }

        public void SetColor(Color c) { c.a = .16f; mat.SetColor("_Color", c); }

        void LateUpdate()
        {
            if (++frame % 2 != 0) return;
            var origin = transform.position;
            verts[0] = Vector3.zero;
            for (int i = 0; i <= Rays; i++)
            {
                float a = -angle / 2 + angle * i / Rays;
                var localDir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var dir = transform.TransformDirection(localDir);
                float d = range;
                if (Physics.Raycast(origin + Vector3.up * .5f, dir, out var hit, range, Layers.SolidMask)) d = hit.distance;
                verts[i + 1] = localDir * d;
            }
            mesh.vertices = verts;
            mesh.RecalculateBounds();
        }
    }
}
