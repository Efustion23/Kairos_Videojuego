using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kairos.EditorTools
{
    /// Objetos con licencia CC0 (Models/Props): cada carpeta se convierte en un prefab con material URP y base en su origen.
    public static partial class KairosBuilder
    {
        const string PropDir = Root + "/Models/Props", PropPrefabDir = Root + "/Prefabs/Props";
        static readonly Dictionary<string, GameObject> propPrefabs = new Dictionary<string, GameObject>();

        static GameObject PropPrefab(string id)
        {
            if (propPrefabs.TryGetValue(id, out var cached) && cached != null) return cached;
            string prefabPath = $"{PropPrefabDir}/{id}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return propPrefabs[id] = existing;

            if (!AssetDatabase.IsValidFolder(PropPrefabDir)) AssetDatabase.CreateFolder(Root + "/Prefabs", "Props");
            string dir = $"{PropDir}/{id}";
            string fbx = $"{dir}/{id}_1k.fbx";
            var model = (ModelImporter)AssetImporter.GetAtPath(fbx);
            model.importCameras = model.importLights = model.importAnimation = false;
            model.animationType = ModelImporterAnimationType.None;
            model.generateMeshLods = true;
            model.SaveAndReimport();

            var root = new GameObject(id);
            var mesh = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            mesh.transform.SetParent(root.transform, false);

            var bounds = new Bounds();
            bool first = true;
            foreach (var r in mesh.GetComponentsInChildren<MeshRenderer>())
            {
                // Algunos modelos traen curvas (líneas) exportadas como malla: no se dibujan.
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.subMeshCount == 0 || mf.sharedMesh.GetTopology(0) != MeshTopology.Triangles || mf.sharedMesh.GetIndexCount(0) == 0) { r.gameObject.SetActive(false); continue; }
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = PropMaterial(id, dir, slots[i] != null ? slots[i].name : id);
                r.sharedMaterials = slots;
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }
            // La base del objeto queda en y=0 y centrada: colocarlo es dar el punto donde se apoya.
            mesh.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return propPrefabs[id] = prefab;
        }

        /// Textura de un juego (diff, nor_gl, rough, metal, opacity) sea cual sea su formato.
        static string Tex(string dir, string set, string kind)
        {
            foreach (var ext in new[] { ".jpg", ".png", ".exr" })
                if (File.Exists($"{dir}/{set}_{kind}_1k{ext}")) return $"{dir}/{set}_{kind}_1k{ext}";
            return null;
        }

        /// Cada hueco de material usa su propio juego de texturas si el modelo lo trae (p. ej. «_accessories»).
        static Material PropMaterial(string id, string dir, string slot)
        {
            string lower = slot.ToLowerInvariant();
            bool glass = lower.Contains("glass"), screen = lower.EndsWith("_screen");
            if (Tex(dir, slot, "diff") != null) id = slot;
            string path = $"{dir}/M_{id}{(glass ? "_glass" : screen ? "_screen" : "")}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (glass)
            {
                // Tulipa encendida: se ve como luz, sin coste de transparencia.
                m.SetColor("_BaseColor", new Color(.9f, .75f, .5f));
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(1f, .78f, .45f) * 2.2f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                AssetDatabase.CreateAsset(m, path);
                return m;
            }
            if (screen)
            {
                // Pantalla encendida: carta náutica en verde azulado, como el resto de consolas del puente.
                m.SetColor("_BaseColor", new Color(.03f, .18f, .16f));
                m.SetFloat("_Smoothness", .9f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(.15f, .85f, .7f) * 1.4f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                AssetDatabase.CreateAsset(m, path);
                return m;
            }
            string baseTex = Tex(dir, id, "diff"), opacity = Tex(dir, id, "opacity");
            if (opacity != null) baseTex = BakeAlpha(baseTex, opacity, $"{dir}/{id}_base.png");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseTex));
            if (opacity != null)
            {
                m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            string normal = Tex(dir, id, "nor_gl");
            var ni = (TextureImporter)AssetImporter.GetAtPath(normal);
            if (ni.textureType != TextureImporterType.NormalMap) { ni.textureType = TextureImporterType.NormalMap; ni.SaveAndReimport(); }
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", BakeMask(id, dir));
            m.SetFloat("_Smoothness", 1);
            m.SetFloat("_Metallic", 1);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// URP guarda metal en R y lisura en A: se combinan los mapas sueltos del modelo y se retiran los originales.
        static Texture2D BakeMask(string id, string dir)
        {
            string output = $"{dir}/{id}_mask.png";
            if (!File.Exists(output))
            {
                string rough = Tex(dir, id, "rough"), metal = Tex(dir, id, "metal");
                var r = ReadPixels(rough, out int w, out int h);
                var mt = metal != null ? ReadPixels(metal, out _, out _) : null;
                var px = new Color[r.Length];
                for (int i = 0; i < px.Length; i++)
                    px[i] = new Color(mt != null && mt.Length == r.Length ? mt[i].r : 0, 1, 0, 1 - r[i].r);
                WritePng(px, w, h, output);
                AssetDatabase.DeleteAsset(rough);
                if (mt != null) AssetDatabase.DeleteAsset(metal);
                AssetDatabase.ImportAsset(output);
                var ti = (TextureImporter)AssetImporter.GetAtPath(output);
                ti.sRGBTexture = false; ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(output);
        }

        static string BakeAlpha(string diffuse, string opacity, string output)
        {
            var d = ReadPixels(diffuse, out int w, out int h);
            var o = ReadPixels(opacity, out _, out _);
            for (int i = 0; i < d.Length && i < o.Length; i++) d[i].a = o[i].r;
            WritePng(d, w, h, output);
            AssetDatabase.ImportAsset(output);
            AssetDatabase.DeleteAsset(diffuse);
            AssetDatabase.DeleteAsset(opacity);
            return output;
        }

        static Color[] ReadPixels(string path, out int w, out int h)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.isReadable = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.sRGBTexture = false;
            ti.SaveAndReimport();
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            w = t.width; h = t.height;
            return t.GetPixels();
        }

        static void WritePng(Color[] px, int w, int h, string path)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            t.SetPixels(px);
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }
    }
}
