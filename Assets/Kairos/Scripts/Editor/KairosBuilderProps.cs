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
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = PropMaterial(id, dir, slots[i] != null && slots[i].name.ToLowerInvariant().Contains("glass"));
                r.sharedMaterials = slots;
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }
            // La base del objeto queda en y=0 y centrada: colocarlo es dar el punto donde se apoya.
            mesh.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return propPrefabs[id] = prefab;
        }

        static Material PropMaterial(string id, string dir, bool glass)
        {
            string path = $"{dir}/M_{id}{(glass ? "_glass" : "")}.mat";
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
            string baseTex = $"{dir}/{id}_diff_1k.jpg", opacity = $"{dir}/{id}_opacity_1k.png";
            if (File.Exists(opacity)) baseTex = BakeAlpha(baseTex, opacity, $"{dir}/{id}_base.png");
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseTex));
            if (File.Exists(opacity))
            {
                m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", .5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            string normal = $"{dir}/{id}_nor_gl_1k.exr";
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
                string rough = File.Exists($"{dir}/{id}_rough_1k.exr") ? $"{dir}/{id}_rough_1k.exr" : $"{dir}/{id}_rough_1k.jpg";
                string metal = $"{dir}/{id}_metal_1k.exr";
                var r = ReadPixels(rough, out int w, out int h);
                var mt = File.Exists(metal) ? ReadPixels(metal, out _, out _) : null;
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
